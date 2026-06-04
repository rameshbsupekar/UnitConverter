using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitConverter.UserManagement.Contracts.Requests;
using UnitConverter.UserManagement.Api.Tests.TestHelpers;
using UnitConverter.UserManagement.Application.Validators;
using UnitConverter.Common.Constants.Validation;
namespace UnitConverter.UserManagement.Api.Tests.Unit.Application;

/// <summary>
/// BDD-organized, data-driven tests for RegisterUserRequest validation.
/// Uses [DataTestMethod] and [DataRow] to eliminate test duplication.
/// Groups tests by scenario: happy path, email validation (positive/negative),
/// password validation (positive/negative), name validation, organization validation,
/// required fields, idempotency key, and edge cases.
/// 
/// No repetitive tests - uses data-driven approach throughout.
/// </summary>
[TestClass]
public class RegistrationValidationTests
{
    private RegisterUserValidator _validator = null!;

    [TestInitialize]
    public void Setup()
    {
        _validator = new RegisterUserValidator();
    }

    // ===== SCENARIO 1: Happy Path - Valid Inputs =====

    [DataTestMethod]
    [DataRow("john@example.com", "ValidPass123!@#", "John", "Doe", "ACME Corp", true)]
    [DataRow("j@test.com", "SecurePass789!@", "Mary", "Smith", "Corp", true)]
    [DataRow("user@example.com", "MyP@ssw0rd456", "Jose", "Garcia", "International Corp", true)]
    public void Validate_ValidInputs_SucceedsWithNoErrors(
        string email, string password, string firstName, string lastName, string org, bool expectedValid)
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: email,
            password: password,
            firstName: firstName,
            lastName: lastName,
            organizationName: org);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.AreEqual(expectedValid, result.IsValid, $"Expected valid={expectedValid}. Errors: {string.Join(", ", result.Errors.Select(e => e.ErrorMessage))}");
        if (expectedValid)
        {
            Assert.AreEqual(0, result.Errors.Count);
        }
    }

    [TestMethod]
    public void Validate_MinimumValidInput_Succeeds()
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "a@b.com",
            password: "ValidPass123!@#",
            firstName: "A",
            lastName: "B",
            organizationName: "O");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.IsTrue(result.IsValid, $"Errors: {string.Join(", ", result.Errors.Select(e => e.ErrorMessage))}");
    }

    // ===== SCENARIO 2: Email Validation - Positive =====

    [DataTestMethod]
    [DataRow("user@example.com")]
    [DataRow("firstname.lastname@domain.co.uk")]
    [DataRow("user+tag@example.org")]
    [DataRow("test123@test.io")]
    public void Validate_ValidEmailFormats_Pass(string email)
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: email,
            password: "SecurePass123!@#",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME");

        // Act
        var result = _validator.Validate(command);

        // Assert
        var emailErrors = result.Errors.Where(e => e.PropertyName == "Email").ToList();
        Assert.AreEqual(0, emailErrors.Count, $"Email should be valid");
    }

    [TestMethod]
    public void Validate_EmailCaseSensitivity_Accepts()
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "User@EXAMPLE.COM",
            password: "SecurePass123!@#",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.IsTrue(result.IsValid);
    }

    // ===== SCENARIO 3: Email Validation - Negative =====

    [DataTestMethod]
    [DataRow("invalid.email")]
    [DataRow("@example.com")]
    [DataRow("user@")]
    public void Validate_InvalidEmailFormats_Fail(string email) =>
        AssertEmailValidationFails(email, expectFormatError: true);

    [DataTestMethod]
    [DataRow("")]
    [DataRow(null)]
    [DataRow("   ")]
    public void Validate_EmptyEmail_FailsWithRequiredError(string email) =>
        AssertEmailValidationFails(email, expectFormatError: false);

    // ===== SCENARIO 4: Password Validation - Positive =====

    [DataTestMethod]
    [DataRow("SecurePass123!@#")]
    [DataRow("MyP@ssw0rd456ABC")]
    [DataRow("ValidPass789!@#$")]
    [DataRow("Test#Pass001ABC")]
    public void Validate_StrongPasswords_Pass(string password)
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "test@example.com",
            password: password,
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME");

        // Act
        var result = _validator.Validate(command);

        // Assert
        var passwordErrors = result.Errors.Where(e => e.PropertyName == "Password").ToList();
        Assert.AreEqual(0, passwordErrors.Count, $"Password should be valid");
    }

    // ===== SCENARIO 5: Password Validation - Negative =====

    [DataTestMethod]
    [DataRow("password123")]      
    [DataRow("Pass@123")]          
    [DataRow("NoDigit@abc")]      
    [DataRow("abc123!@#")]   
       
    public void Validate_WeakPasswords_Fail(string password) =>
        AssertPasswordValidationFails(password, expectPolicyError: true);

    [DataTestMethod]
    [DataRow("Short@Pass1")]
    [DataRow("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*_+-=[]{}|:,.;<>?/ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz")]
    public void Validate_PasswordLength_Enforced(string password) =>
        AssertPasswordValidationFails(password, expectPolicyError: false);

    [DataTestMethod]
    [DataRow("")]
    [DataRow(null)]
    [DataRow("   ")]
    public void Validate_EmptyPassword_FailsWithRequiredError(string password) =>
        AssertPasswordValidationFails(password, expectRequiredError: true);

    // ===== SCENARIO 6: Name Validation - Positive =====

    [DataTestMethod]
    [DataRow("John", "Smith")]
    [DataRow("Jean-Pierre", "van den Berg")]
    [DataRow("Jose", "Garcia-Lopez")]
    [DataRow("Mary", "O'Connor")]
    public void Validate_ValidNames_Pass(string firstName, string lastName)
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "test@example.com",
            password: "SecurePass123!@#",
            firstName: firstName,
            lastName: lastName,
            organizationName: "ACME");

        // Act
        var result = _validator.Validate(command);

        // Assert
        var nameErrors = result.Errors
            .Where(e => e.PropertyName == "FirstName" || e.PropertyName == "LastName")
            .ToList();
        Assert.AreEqual(0, nameErrors.Count);
    }

    // ===== SCENARIO 7: Name Validation - Negative =====

    [DataTestMethod]
    [DataRow("John123")]      
    [DataRow("John@Doe")]     
    [DataRow("Name#Tag")]     
    public void Validate_InvalidNameCharacters_Fail(string invalidName)
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "test@example.com",
            password: "SecurePass123!@#",
            firstName: invalidName,
            lastName: "Smith",
            organizationName: "ACME");

        // Act
        var result = _validator.Validate(command);

        // Assert
        var nameErrors = result.Errors.Where(e => e.PropertyName == "FirstName").ToList();
        Assert.IsTrue(nameErrors.Count > 0);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void Validate_NameLength_Enforced(string name)
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "test@example.com",
            password: "SecurePass123!@#",
            firstName: name,
            lastName: "Smith",
            organizationName: "ACME");

        // Act
        var result = _validator.Validate(command);

        // Assert
        var nameErrors = result.Errors.Where(e => e.PropertyName == "FirstName").ToList();
        Assert.IsTrue(nameErrors.Count > 0);
    }

    // ===== SCENARIO 8: Organization Validation =====

    [DataTestMethod]
    [DataRow("ACME")]
    [DataRow("Company Inc.")]
    [DataRow("Test Org 123")]
    [DataRow("A")]
    [DataRow("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX")]
    public void Validate_ValidOrganizations_Pass(string org)
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "test@example.com",
            password: "SecurePass123!@#",
            firstName: "John",
            lastName: "Doe",
            organizationName: org);

        // Act
        var result = _validator.Validate(command);

        // Assert
        var orgErrors = result.Errors.Where(e => e.PropertyName == "OrganizationName").ToList();
        Assert.AreEqual(0, orgErrors.Count);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void Validate_OrganizationLength_Enforced(string org)
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "test@example.com",
            password: "SecurePass123!@#",
            firstName: "John",
            lastName: "Doe",
            organizationName: org);

        // Act
        var result = _validator.Validate(command);

        // Assert
        var orgErrors = result.Errors.Where(e => e.PropertyName == "OrganizationName").ToList();
        Assert.IsTrue(orgErrors.Count > 0);
    }

    // ===== SCENARIO 9: Required Fields =====

    [DataTestMethod]
    [DataRow("", "SecurePass123!@#", "John", "Doe", "ACME")]
    [DataRow("test@example.com", "", "John", "Doe", "ACME")]
    [DataRow("test@example.com", "SecurePass123!@#", "", "Doe", "ACME")]
    [DataRow("test@example.com", "SecurePass123!@#", "John", "", "ACME")]
    [DataRow("test@example.com", "SecurePass123!@#", "John", "Doe", "")]
    public void Validate_MissingRequiredFields_Fail(
        string email, string password, string firstName, string lastName, string org)
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: email,
            password: password,
            firstName: firstName,
            lastName: lastName,
            organizationName: org);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Count > 0);
    }

    // ===== SCENARIO 10: Idempotency Key =====

    [DataTestMethod]
    [DataRow("")]
    [DataRow(null)]
    [DataRow("   ")]
    public void Validate_IdempotencyKey_Required(string idempotencyKey)
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "test@example.com",
            password: "SecurePass123!@#",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME",
            idempotencyKey: idempotencyKey,
            generateIdempotencyKeyIfMissing: false);

        // Act
        var result = _validator.Validate(command);

        // Assert
        var idempotencyErrors = result.Errors.Where(e => e.PropertyName == "IdempotencyKey").ToList();
        Assert.IsTrue(idempotencyErrors.Count > 0);
    }

    // ===== EDGE CASES: Maximum Length Boundaries =====

    [TestMethod]
    public void Validate_MaxLengthBoundaries_Pass()
    {
        // Create valid password at max length
        var maxPassword = "A" + new string('a', 114) + "123!@#";
        var maxFirstName = new string('A', 100);
        var maxLastName = new string('B', 100);
        var maxOrg = new string('C', 200);

        var command = RegisterUserRequestFactory.Create(
            email: "boundary@example.com",
            password: maxPassword,
            firstName: maxFirstName,
            lastName: maxLastName,
            organizationName: maxOrg);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void Validate_AllFieldsMaxLength_Succeeds()
    {
        // Arrange
        var maxFirstName = new string('A', 100);
        var maxLastName = new string('B', 100);
        var maxOrg = new string('C', 200);
        var maxPassword = "A" + new string('a', 114) + "123!@#";

        var command = RegisterUserRequestFactory.Create(
            email: "maxlength@example.com",
            password: maxPassword,
            firstName: maxFirstName,
            lastName: maxLastName,
            organizationName: maxOrg);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.IsTrue(result.IsValid);
    }

    private void AssertEmailValidationFails(string email, bool expectFormatError)
    {
        var command = RegisterUserRequestFactory.Create(
            email: email,
            password: "SecurePass123!@#",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME");

        var result = _validator.Validate(command);
        var emailErrors = result.Errors.Where(e => e.PropertyName == "Email").ToList();
        Assert.IsTrue(emailErrors.Count > 0);

        if (expectFormatError)
        {
            Assert.IsTrue(
                emailErrors.Exists(e => e.ErrorMessage.Contains("format", StringComparison.OrdinalIgnoreCase)),
                "Expected an email format validation error.");
        }
    }

    private void AssertPasswordValidationFails(
        string password,
        bool expectPolicyError = false,
        bool expectRequiredError = false)
    {
        var command = RegisterUserRequestFactory.Create(
            email: "test@example.com",
            password: password,
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME");

        var result = _validator.Validate(command);
        var passwordErrors = result.Errors.Where(e => e.PropertyName == "Password").ToList();
        Assert.IsTrue(passwordErrors.Count > 0);

        if (expectRequiredError)
        {
            Assert.IsTrue(
                passwordErrors.Exists(e => e.ErrorMessage.Contains("required", StringComparison.OrdinalIgnoreCase)),
                "Expected a required-password validation error.");
        }
        else if (expectPolicyError)
        {
            Assert.IsFalse(
                passwordErrors.TrueForAll(e =>
                    e.ErrorMessage.Contains("length", StringComparison.OrdinalIgnoreCase)),
                "Expected a password policy validation error.");
        }
        else
        {
            Assert.IsTrue(
                passwordErrors.Exists(e =>
                    e.ErrorMessage.Contains("length", StringComparison.OrdinalIgnoreCase)
                    || e.ErrorMessage.Contains("at least", StringComparison.OrdinalIgnoreCase)
                    || e.ErrorMessage.Contains("exceed", StringComparison.OrdinalIgnoreCase)),
                "Expected a password length validation error.");
        }
    }
}