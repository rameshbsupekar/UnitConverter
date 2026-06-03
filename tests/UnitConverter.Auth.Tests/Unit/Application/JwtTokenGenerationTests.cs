using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitConverter.Auth.Application.Services;
using UnitConverter.Auth.Common.Models;
using UnitConverter.Auth.Tests.Fixtures;

namespace UnitConverter.Auth.Tests.Unit.Application;

/// <summary>
/// BDD-organized tests for JWT token generation and validation.
/// Groups tests by scenario: happy path, randomness, validation, and edge cases.
/// No test duplication - uses DataTestMethod for parameterized scenarios.
/// </summary>
[TestClass]
public class JwtTokenGenerationTests
{
    private JwtTokenService _service = null!;
    private JwtSettings _settings = null!;

    [TestInitialize]
    public void Setup()
    {
        _settings = new JwtSettings
        {
            Secret = "this-is-a-very-secure-secret-key-min-32-chars",
            Issuer = "http://localhost:5000",
            Audience = "api.unitconverter",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        };
        _service = new JwtTokenService(_settings);
    }

    // ===== SCENARIO 1: Happy Path - Valid Token Generation =====

    [TestMethod]
    public void GenerateAccessToken_ValidUser_ReturnsToken()
    {
        // Arrange
        var user = TestUserFactory.CreateValidUser();
        var roles = new[] { "Employee" };

        // Act
        var token = _service.GenerateAccessToken(user, roles);

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(token));
        Assert.IsTrue(token.Length > 0);
    }

    [TestMethod]
    public void GenerateAccessToken_TokenHasValidJwtFormat_Success()
    {
        // Arrange
        var user = TestUserFactory.CreateValidUser();

        // Act
        var token = _service.GenerateAccessToken(user, new[] { "User" });

        // Assert - JWT format is header.payload.signature (3 parts)
        Assert.IsTrue(JwtTestHelper.HasValidJwtFormat(token));
        var parts = token.Split('.');
        Assert.AreEqual(3, parts.Length);
    }

    [TestMethod]
    public void GenerateAccessToken_TokenExpiresInFuture_Success()
    {
        // Arrange
        var user = TestUserFactory.CreateValidUser();
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _service.GenerateAccessToken(user, new[] { "User" });
        var afterGeneration = DateTime.UtcNow;

        // Assert - expiration should be in future
        var expirationTime = JwtTestHelper.GetExpirationTime(token);
        Assert.IsTrue(expirationTime > afterGeneration);
        // Expiration should be within 1 minute from now (allowing for execution time)
        Assert.IsTrue(expirationTime < DateTime.UtcNow.AddMinutes(16));
    }

    [TestMethod]
    public void GenerateAccessToken_ReturnsNewTokenEachTime_Success()
    {
        // Arrange
        var user = TestUserFactory.CreateValidUser();

        // Act - generate multiple tokens
        var token1 = _service.GenerateAccessToken(user, new[] { "User" });
        var token2 = _service.GenerateAccessToken(user, new[] { "User" });

        // Assert - tokens should be different due to different JTI
        Assert.AreNotEqual(token1, token2);
    }

    // ===== SCENARIO 2: Token Validation - Positive =====

    [TestMethod]
    public void ValidateToken_ValidToken_ReturnsIsValidTrue()
    {
        // Arrange
        var user = TestUserFactory.CreateValidUser();
        var token = _service.GenerateAccessToken(user, new[] { "User" });

        // Act
        var result = _service.ValidateToken(token);

        // Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateToken_ExtractsUserIdCorrectly_Success()
    {
        // Arrange
        var userId = 123;
        var user = TestUserFactory.CreateValidUser(userId: userId);
        var token = _service.GenerateAccessToken(user, new[] { "User" });

        // Act
        var result = _service.ValidateToken(token);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(userId, result.UserId);
    }

    [TestMethod]
    public void ValidateToken_ExtractsEmailCorrectly_Success()
    {
        // Arrange
        var email = "alice@example.com";
        var user = TestUserFactory.CreateValidUser(email: email);
        var token = _service.GenerateAccessToken(user, new[] { "User" });

        // Act
        var result = _service.ValidateToken(token);

        // Assert
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(email, result.Email);
    }

    [TestMethod]
    public void ValidateToken_PopulatesResultWithValidData_Success()
    {
        // Arrange
        var user = TestUserFactory.CreateValidUser();
        var token = _service.GenerateAccessToken(user, new[] { "User" });

        // Act
        var result = _service.ValidateToken(token);

        // Assert - result should be populated
        Assert.IsTrue(result.IsValid);
        Assert.IsTrue(result.UserId > 0);
        Assert.IsFalse(string.IsNullOrEmpty(result.Email));
    }

    // ===== SCENARIO 3: Refresh Token Randomness =====

    [DataTestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    public void GenerateRefreshToken_MultipleCalls_ProduceDifferentTokens(int callNumber)
    {
        // Arrange
        var tokens = new HashSet<string>();

        // Act - generate 5 different tokens
        for (int i = 0; i < 5; i++)
        {
            var token = _service.GenerateRefreshToken();
            tokens.Add(token);
        }

        // Assert - all tokens should be unique
        Assert.AreEqual(5, tokens.Count, "Refresh tokens should be unique on every call");
    }

    [TestMethod]
    public void GenerateRefreshToken_ReturnsBase64String_Success()
    {
        // Arrange & Act
        var token = _service.GenerateRefreshToken();

        // Assert - should be valid base64 and have reasonable length (32 bytes = 44 chars base64)
        Assert.IsFalse(string.IsNullOrWhiteSpace(token));
        try
        {
            Convert.FromBase64String(token);
            // If we got here, it's valid base64
            Assert.IsTrue(token.Length >= 40); // 32 bytes encoded in base64 is ~44 chars
        }
        catch (FormatException)
        {
            Assert.Fail("Refresh token is not valid base64");
        }
    }

    [TestMethod]
    public void GenerateRefreshToken_NonEmpty_Success()
    {
        // Arrange & Act
        var token = _service.GenerateRefreshToken();

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(token));
        Assert.IsTrue(token.Length > 0);
    }

    // ===== SCENARIO 4: Token Validation - Negative =====

    [DataTestMethod]
    [DataRow("invalid.token")]
    [DataRow("missing.parts")]
    [DataRow("way.too.many.parts.here.for.jwt")]
    public void ValidateToken_InvalidTokenFormats_ReturnsFalse(string invalidToken)
    {
        // Act
        var result = _service.ValidateToken(invalidToken);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [TestMethod]
    public void ValidateToken_NullToken_ReturnsFalse()
    {
        // Act
        var result = _service.ValidateToken(null!);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [TestMethod]
    public void ValidateToken_EmptyToken_ReturnsFalse()
    {
        // Act
        var result = _service.ValidateToken("");

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [TestMethod]
    public void ValidateToken_TamperedSignature_ReturnsFalse()
    {
        // Arrange - create a valid token, then tamper with signature
        var user = TestUserFactory.CreateValidUser();
        var validToken = _service.GenerateAccessToken(user, new[] { "User" });
        
        // Tamper with the signature part (last part after last dot)
        var parts = validToken.Split('.');
        parts[2] = "invalidsignature123456789abcdef";
        var tamperedToken = string.Join(".", parts);

        // Act
        var result = _service.ValidateToken(tamperedToken);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [TestMethod]
    public void ValidateToken_WrongIssuer_ReturnsFalse()
    {
        // Arrange - create token with different issuer settings
        var wrongIssuerSettings = new JwtSettings
        {
            Secret = "this-is-a-very-secure-secret-key-min-32-chars",
            Issuer = "http://wrong-issuer.com", // Different issuer
            Audience = "api.unitconverter",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        };
        var wrongIssuerService = new JwtTokenService(wrongIssuerSettings);
        var user = TestUserFactory.CreateValidUser();
        var token = wrongIssuerService.GenerateAccessToken(user, new[] { "User" });

        // Act - validate with original service (different issuer)
        var result = _service.ValidateToken(token);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [TestMethod]
    public void ValidateToken_WrongAudience_ReturnsFalse()
    {
        // Arrange - create token with different audience settings
        var wrongAudienceSettings = new JwtSettings
        {
            Secret = "this-is-a-very-secure-secret-key-min-32-chars",
            Issuer = "http://localhost:5000",
            Audience = "wrong-audience", // Different audience
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        };
        var wrongAudienceService = new JwtTokenService(wrongAudienceSettings);
        var user = TestUserFactory.CreateValidUser();
        var token = wrongAudienceService.GenerateAccessToken(user, new[] { "User" });

        // Act - validate with original service (different audience)
        var result = _service.ValidateToken(token);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [TestMethod]
    public void ValidateToken_SetErrorMessageOnFailure_Success()
    {
        // Act
        var result = _service.ValidateToken("invalid.token.format");

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsTrue(result.ErrorMessage.Length > 0);
    }

    // ===== SCENARIO 5: Edge Cases =====

    [TestMethod]
    public void GenerateAccessToken_VeryLongUserEmail_Succeeds()
    {
        // Arrange
        var user = TestUserFactory.CreateUserWithLongEmail();

        // Act
        var token = _service.GenerateAccessToken(user, new[] { "User" });

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(token));
        var result = _service.ValidateToken(token);
        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(user.Email.Value, result.Email);
    }

    [TestMethod]
    public void GenerateAccessToken_UnicodeInOrganizationName_Succeeds()
    {
        // Arrange
        var user = TestUserFactory.CreateUserWithUnicodeOrganization();

        // Act
        var token = _service.GenerateAccessToken(user, new[] { "User" });

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(token));
        var result = _service.ValidateToken(token);
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void GenerateAccessToken_NoRoles_Succeeds()
    {
        // Arrange
        var user = TestUserFactory.CreateValidUser();
        var roles = Array.Empty<string>();

        // Act
        var token = _service.GenerateAccessToken(user, roles);

        // Assert - should still generate valid token even with no roles
        Assert.IsFalse(string.IsNullOrWhiteSpace(token));
        var result = _service.ValidateToken(token);
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void GenerateAccessToken_SingleRole_Succeeds()
    {
        // Arrange
        var user = TestUserFactory.CreateValidUser();
        var roles = new[] { "Editor" };

        // Act
        var token = _service.GenerateAccessToken(user, roles);

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(token));
        var result = _service.ValidateToken(token);
        Assert.IsTrue(result.IsValid);
    }

    [DataTestMethod]
    [DataRow("Admin", "Editor", "Reviewer")]
    [DataRow("User", "Operator", "Auditor")]
    [DataRow("Root", "Admin", "Support")]
    public void GenerateAccessToken_MultipleRoles_Succeeds(string role1, string role2, string role3)
    {
        // Arrange
        var user = TestUserFactory.CreateValidUser();
        var roles = new[] { role1, role2, role3 };

        // Act
        var token = _service.GenerateAccessToken(user, roles);

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(token));
        var result = _service.ValidateToken(token);
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void GenerateAccessToken_ManyRoles_Succeeds()
    {
        // Arrange - generate 15 roles
        var manyRoles = Enumerable.Range(1, 15)
            .Select(i => $"Role{i}")
            .ToArray();
        var user = TestUserFactory.CreateValidUser();

        // Act
        var token = _service.GenerateAccessToken(user, manyRoles);

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(token));
        var result = _service.ValidateToken(token);
        Assert.IsTrue(result.IsValid);
    }

    // ===== SCENARIO 6: Constructor Validation =====

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Constructor_NullSettings_ThrowsArgumentNullException()
    {
        // Act
        new JwtTokenService(null!);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Constructor_SecretTooShort_ThrowsArgumentException()
    {
        // Arrange
        var shortSecretSettings = new JwtSettings
        {
            Secret = "short", // Less than 32 characters
            Issuer = "http://localhost",
            Audience = "api",
            AccessTokenExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        };

        // Act
        new JwtTokenService(shortSecretSettings);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void GenerateAccessToken_NullUser_ThrowsArgumentNullException()
    {
        // Act
        _service.GenerateAccessToken(null!, new[] { "User" });
    }
}
