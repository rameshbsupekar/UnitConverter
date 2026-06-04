using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitConverter.UserManagement.Application.Services;

namespace UnitConverter.UserManagement.Api.Tests.Unit.Application;

/// <summary>
/// BDD-organized tests for password hashing and verification using bcrypt.
/// Groups tests by scenario: happy path, randomness, verification (positive/negative), and edge cases.
/// No test duplication - uses DataTestMethod for parameterized scenarios.
/// </summary>
[TestClass]
public class PasswordValidationTests
{
    private PasswordService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new PasswordService();
    }

    // ===== SCENARIO 1: Happy Path - Hash & Verify =====

    [TestMethod]
    public void HashPassword_ValidPassword_ReturnsHash()
    {
        // Arrange
        var password = "ValidPassword123!";

        // Act
        var hash = _service.HashPassword(password);

        // Assert
        Assert.IsFalse(string.IsNullOrWhiteSpace(hash));
        Assert.IsTrue(hash.Length > 0);
    }

    [TestMethod]
    public void HashPassword_HashDiffersFromPlaintext_Success()
    {
        // Arrange
        var password = "SecureP@ss456";

        // Act
        var hash = _service.HashPassword(password);

        // Assert - hash should be completely different from plaintext
        Assert.AreNotEqual(password, hash);
        Assert.IsFalse(hash.Contains(password));
    }

    [TestMethod]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "CorrectPassword789!";
        var hash = _service.HashPassword(password);

        // Act
        var isValid = _service.VerifyPassword(password, hash);

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void VerifyPassword_MatchesHashedPassword_Success()
    {
        // Arrange
        var password = "MyP@ssw0rd123";
        var hash = _service.HashPassword(password);

        // Act
        var result = _service.VerifyPassword(password, hash);

        // Assert
        Assert.IsTrue(result);
    }

    // ===== SCENARIO 2: Randomness (bcrypt adds salt) =====

    [DataTestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void HashPassword_SamePasswordTwice_ProducesDifferentHashes(int iteration)
    {
        // Arrange
        var password = "SamePassword123!";
        var hashes = new HashSet<string>();

        // Act - hash the same password 3 times
        for (int i = 0; i < 3; i++)
        {
            var hash = _service.HashPassword(password);
            hashes.Add(hash);
        }

        // Assert - all hashes should be unique (due to random salt)
        Assert.AreEqual(3, hashes.Count, 
            "Same password should produce different hashes due to random salt in bcrypt");
        
        // Verify that each hash still matches the original password
        foreach (var hash in hashes)
        {
            Assert.IsTrue(_service.VerifyPassword(password, hash));
        }
    }

    // ===== SCENARIO 3: Password Verification - Positive =====

    [DataTestMethod]
    [DataRow("ValidPassword123!")]
    [DataRow("SecureP@ss456")]
    [DataRow("MyP@ssw0rd789")]
    public void VerifyPassword_ValidPasswords_SuccessfullyVerify(string password)
    {
        // Arrange
        var hash = _service.HashPassword(password);

        // Act
        var result = _service.VerifyPassword(password, hash);

        // Assert
        Assert.IsTrue(result);
    }

    // ===== SCENARIO 4: Password Verification - Negative =====

    [DataTestMethod]
    [DataRow("correct@Pass123", "incorrect@Pass123")]
    [DataRow("Password123!", "Password124!")]
    [DataRow("MySecure@2024", "MySecure@2025")]
    public void VerifyPassword_WrongPasswords_ReturnFalse(string correctPassword, string wrongPassword)
    {
        // Arrange
        var hash = _service.HashPassword(correctPassword);

        // Act
        var result = _service.VerifyPassword(wrongPassword, hash);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyPassword_EmptyPassword_ReturnsFalse()
    {
        // Arrange
        var validPassword = "ValidPassword123!";
        var hash = _service.HashPassword(validPassword);

        // Act
        var result = _service.VerifyPassword("", hash);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyPassword_NullPassword_ReturnsFalse()
    {
        // Arrange
        var validPassword = "ValidPassword123!";
        var hash = _service.HashPassword(validPassword);

        // Act
        var result = _service.VerifyPassword(null!, hash);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyPassword_NullHash_ReturnsFalse()
    {
        // Arrange
        var password = "ValidPassword123!";

        // Act
        var result = _service.VerifyPassword(password, null!);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyPassword_EmptyHash_ReturnsFalse()
    {
        // Arrange
        var password = "ValidPassword123!";

        // Act
        var result = _service.VerifyPassword(password, "");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyPassword_InvalidHashFormat_ReturnsFalse()
    {
        // Arrange
        var password = "ValidPassword123!";
        var invalidHash = "this-is-not-a-valid-bcrypt-hash-format";

        // Act
        var result = _service.VerifyPassword(password, invalidHash);

        // Assert - should gracefully return false instead of throwing
        Assert.IsFalse(result);
    }

    // ===== SCENARIO 5: Edge Cases =====

    [TestMethod]
    public void VerifyPassword_VeryLongPassword_SucceedsOrFails()
    {
        // Arrange - bcrypt has a max length of 72 bytes, but this tests the boundary
        var veryLongPassword = new string('a', 100);
        var hash = _service.HashPassword(veryLongPassword);

        // Act - verify with the same long password
        var result1 = _service.VerifyPassword(veryLongPassword, hash);
        
        // Act - verify with a different long password
        var differentLongPassword = new string('b', 100);
        var result2 = _service.VerifyPassword(differentLongPassword, hash);

        // Assert
        Assert.IsTrue(result1, "Same very long password should verify correctly");
        Assert.IsFalse(result2, "Different very long password should fail verification");
    }

    [TestMethod]
    public void VerifyPassword_SpecialCharacters_Succeeds()
    {
        // Arrange - password with special characters
        var specialPassword = "P@ss!#$%^&*()";
        var hash = _service.HashPassword(specialPassword);

        // Act
        var result = _service.VerifyPassword(specialPassword, hash);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void VerifyPassword_UnicodeCharacters_Succeeds()
    {
        // Arrange - password with unicode characters
        var unicodePassword = "P@ss🔐™字";
        var hash = _service.HashPassword(unicodePassword);

        // Act
        var result = _service.VerifyPassword(unicodePassword, hash);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void HashPassword_Performance_CompletesInReasonableTime()
    {
        // Arrange
        var password = "PerformanceTest123!";
        var stopwatch = Stopwatch.StartNew();

        // Act
        var hash = _service.HashPassword(password);
        stopwatch.Stop();

        // Assert - bcrypt with work factor 12 should take ~100-300ms
        // We allow up to 1000ms to account for slower systems (CI/CD environments can be slower)
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000,
            $"Password hashing took {stopwatch.ElapsedMilliseconds}ms, expected < 1000ms");
        Assert.IsFalse(string.IsNullOrWhiteSpace(hash));
    }

    // ===== SCENARIO 6: Constructor Validation & Input Validation =====

    [TestMethod]
    public void HashPassword_NullPassword_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() => _service.HashPassword(null!));
    }

    [TestMethod]
    public void HashPassword_EmptyPassword_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() => _service.HashPassword(""));
    }

    [TestMethod]
    public void HashPassword_WhitespaceOnlyPassword_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() => _service.HashPassword("   "));
    }

    [TestMethod]
    public void VerifyPassword_BothInputsEmpty_ReturnsFalse()
    {
        // Act
        var result = _service.VerifyPassword("", "");

        // Assert
        Assert.IsFalse(result);
    }

    // ===== SCENARIO 7: Case Sensitivity =====

    [TestMethod]
    public void VerifyPassword_CaseSensitive_FailsOnCaseMismatch()
    {
        // Arrange
        var password = "MyPassword123!";
        var hash = _service.HashPassword(password);

        // Act - try with different case
        var wrongCase = "mypassword123!";
        var result = _service.VerifyPassword(wrongCase, hash);

        // Assert - passwords should be case-sensitive
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void VerifyPassword_CorrectCaseMatches_Success()
    {
        // Arrange
        var password = "MyPassword123!";
        var hash = _service.HashPassword(password);

        // Act - use exact same case
        var result = _service.VerifyPassword(password, hash);

        // Assert
        Assert.IsTrue(result);
    }
}
