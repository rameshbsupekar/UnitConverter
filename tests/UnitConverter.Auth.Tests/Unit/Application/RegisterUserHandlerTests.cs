using System;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using UnitConverter.UserManagement.Contracts.Requests;
using UnitConverter.UserManagement.Api.Tests.TestHelpers;
using UnitConverter.UserManagement.Application.Handlers;
using UnitConverter.UserManagement.Common.Interfaces;
using UnitConverter.UserManagement.Core.Domain.Entities;
using UnitConverter.UserManagement.Core.Domain.Exceptions;
using UnitConverter.UserManagement.Core.Domain.ValueObjects;
using UnitConverter.UserManagement.Api.Tests.Fixtures;

namespace UnitConverter.UserManagement.Api.Tests.Unit.Application;

/// <summary>
/// BDD-organized tests for RegisterUserCommandHandler.
/// Groups tests by scenario: happy path, email validation, password validation, 
/// name validation, organization validation, dependencies, and edge cases.
/// 
/// Uses Moq for mocking IUserRepository, IPasswordHasher, and IValidator.
/// No test duplication - test one behavior per method.
/// </summary>
[TestClass]
public class RegisterUserHandlerTests
{
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IPasswordHasher> _mockPasswordHasher = null!;
    private Mock<IValidator<RegisterUserRequest>> _mockValidator = null!;
    private Mock<Microsoft.Extensions.Logging.ILogger<RegisterUserCommandHandler>> _mockLogger = null!;
    private RegisterUserCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockValidator = new Mock<IValidator<RegisterUserRequest>>();
        _mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<RegisterUserCommandHandler>>();

        // Setup default mock behavior
        _mockUserRepository
            .Setup(r => r.EmailExistsAsync(It.IsAny<Email>()))
            .ReturnsAsync(false);

        _mockPasswordHasher
            .Setup(h => h.HashPassword(It.IsAny<string>()))
            .Returns("hashed_password_bcrypt_12345678901");

        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<RegisterUserRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _handler = new RegisterUserCommandHandler(
            _mockUserRepository.Object,
            _mockPasswordHasher.Object,
            _mockValidator.Object,
            _mockLogger.Object);
    }

    // ===== SCENARIO 1: Happy Path - Valid Registration =====

    [TestMethod]
    public async Task HandleAsync_ValidCommand_CreatesUserSuccessfully()
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "john@example.com",
            password: "SecurePass123!@#",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME Corp");

        _mockUserRepository.Setup(r => r.EmailExistsAsync(It.IsAny<Email>()))
            .ReturnsAsync(false);

        // Act
        var response = await _handler.HandleAsync(command);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(command.Email, response.Email);
        Assert.AreEqual(command.FirstName, response.FirstName);
        Assert.AreEqual(command.LastName, response.LastName);
    }

    [TestMethod]
    public async Task HandleAsync_ValidCommand_ReturnsUserResponseWithAllFields()
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "jane@example.com",
            password: "ValidPass123!@#",
            firstName: "Jane",
            lastName: "Smith",
            organizationName: "TechCorp");

        // Act
        var response = await _handler.HandleAsync(command);

        // Assert
        Assert.IsTrue(response.Id > 0, "UserId should be populated");
        Assert.AreEqual(command.Email, response.Email);
        Assert.AreEqual(command.FirstName, response.FirstName);
        Assert.AreEqual(command.LastName, response.LastName);
        Assert.AreEqual(command.OrganizationName, response.OrganizationName);
        Assert.IsTrue(response.CreatedAt != default(DateTime), "CreatedAt should be set");
    }

    [TestMethod]
    public async Task HandleAsync_ValidCommand_CallsAddAsync_Once()
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "test@example.com",
            password: "StrongPass123!@#",
            firstName: "Test",
            lastName: "User",
            organizationName: "TestOrg");

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [TestMethod]
    public async Task HandleAsync_ValidCommand_CallsSaveAsync_Once()
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "persist@example.com",
            password: "ValidPass123!@#",
            firstName: "Persist",
            lastName: "Test",
            organizationName: "PersistOrg");

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _mockUserRepository.Verify(r => r.SaveAsync(), Times.Once);
    }

    // ===== SCENARIO 2: Email Validation =====

    [TestMethod]
    public async Task HandleAsync_DuplicateEmail_ThrowsUserAlreadyExistsException()
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "existing@example.com",
            password: "SecurePass123!@#",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME");

        _mockUserRepository.Setup(r => r.EmailExistsAsync(It.IsAny<Email>()))
            .ReturnsAsync(true);

        await Assert.ThrowsExceptionAsync<UserAlreadyExistsException>(
            () => _handler.HandleAsync(command));
    }

    [TestMethod]
    public async Task HandleAsync_DuplicateEmail_DoesNotSaveUser()
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "duplicate@example.com",
            password: "SecurePass123!@#",
            firstName: "Duplicate",
            lastName: "User",
            organizationName: "OrgName");

        _mockUserRepository.Setup(r => r.EmailExistsAsync(It.IsAny<Email>()))
            .ReturnsAsync(true);

        // Act
        try
        {
            await _handler.HandleAsync(command);
        }
        catch (UserAlreadyExistsException)
        {
            // Expected
        }

        // Assert
        _mockUserRepository.Verify(r => r.SaveAsync(), Times.Never);
    }

    [TestMethod]
    public async Task HandleAsync_InvalidEmailFormat_ThrowsValidationException()
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "invalid.email",
            password: "SecurePass123!@#",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME");

        var validationFailure = new FluentValidation.Results.ValidationFailure("Email", "Invalid email format");
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterUserRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[] { validationFailure }));

        await Assert.ThrowsExceptionAsync<ValidationException>(() => _handler.HandleAsync(command));
    }

    // ===== SCENARIO 3: Password Validation =====

    [TestMethod]
    public async Task HandleAsync_WeakPassword_ThrowsValidationException()
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "test@example.com",
            password: "weak",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME");

        var validationFailure = new FluentValidation.Results.ValidationFailure("Password", "Password must be at least 12 characters");
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterUserRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[] { validationFailure }));

        await Assert.ThrowsExceptionAsync<ValidationException>(() => _handler.HandleAsync(command));
    }

    [TestMethod]
    public async Task HandleAsync_EmptyPassword_ThrowsValidationException()
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "test@example.com",
            password: "",
            firstName: "John",
            lastName: "Doe",
            organizationName: "ACME");

        var validationFailure = new FluentValidation.Results.ValidationFailure("Password", "Password is required");
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterUserRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[] { validationFailure }));

        await Assert.ThrowsExceptionAsync<ValidationException>(() => _handler.HandleAsync(command));
    }

    // ===== SCENARIO 4: Name Validation =====

    [TestMethod]
    public async Task HandleAsync_EmptyFirstName_ThrowsValidationException()
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "test@example.com",
            password: "SecurePass123!@#",
            firstName: "",
            lastName: "Doe",
            organizationName: "ACME");

        var validationFailure = new FluentValidation.Results.ValidationFailure("FirstName", "First name is required");
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterUserRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[] { validationFailure }));

        await Assert.ThrowsExceptionAsync<ValidationException>(() => _handler.HandleAsync(command));
    }

    [TestMethod]
    public async Task HandleAsync_EmptyLastName_ThrowsValidationException()
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "test@example.com",
            password: "SecurePass123!@#",
            firstName: "John",
            lastName: "",
            organizationName: "ACME");

        var validationFailure = new FluentValidation.Results.ValidationFailure("LastName", "Last name is required");
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterUserRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[] { validationFailure }));

        await Assert.ThrowsExceptionAsync<ValidationException>(() => _handler.HandleAsync(command));
    }

    [TestMethod]
    public async Task HandleAsync_VeryLongNames_Succeeds()
    {
        // Arrange
        var longName = new string('A', 100);
        var command = RegisterUserRequestFactory.Create(
            email: "longname@example.com",
            password: "SecurePass123!@#",
            firstName: longName,
            lastName: longName,
            organizationName: "ACME");

        // Act
        var response = await _handler.HandleAsync(command);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(longName, response.FirstName);
        Assert.AreEqual(longName, response.LastName);
    }

    // ===== SCENARIO 5: Organization Validation =====

    [TestMethod]
    public async Task HandleAsync_EmptyOrganization_ThrowsValidationException()
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "test@example.com",
            password: "SecurePass123!@#",
            firstName: "John",
            lastName: "Doe",
            organizationName: "");

        var validationFailure = new FluentValidation.Results.ValidationFailure("OrganizationName", "Organization name is required");
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterUserRequest>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[] { validationFailure }));

        await Assert.ThrowsExceptionAsync<ValidationException>(() => _handler.HandleAsync(command));
    }

    [TestMethod]
    public async Task HandleAsync_VeryLongOrganization_Succeeds()
    {
        // Arrange
        var longOrgName = new string('O', 200);
        var command = RegisterUserRequestFactory.Create(
            email: "longorg@example.com",
            password: "SecurePass123!@#",
            firstName: "John",
            lastName: "Doe",
            organizationName: longOrgName);

        // Act
        var response = await _handler.HandleAsync(command);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(longOrgName, response.OrganizationName);
    }

    // ===== SCENARIO 6: Dependencies (Null Checks) =====

    [TestMethod]
    public void HandleAsync_NullUserRepository_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            new RegisterUserCommandHandler(null!, _mockPasswordHasher.Object, _mockValidator.Object, _mockLogger.Object));
    }

    [TestMethod]
    public void HandleAsync_NullPasswordHasher_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            new RegisterUserCommandHandler(_mockUserRepository.Object, null!, _mockValidator.Object, _mockLogger.Object));
    }

    [TestMethod]
    public void HandleAsync_NullValidator_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            new RegisterUserCommandHandler(_mockUserRepository.Object, _mockPasswordHasher.Object, null!, _mockLogger.Object));
    }

    // ===== EDGE CASES =====

    [TestMethod]
    public async Task HandleAsync_UnicodeCharactersInNames_Succeeds()
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "unicode@example.com",
            password: "SecurePass123!@#",
            firstName: "Jo�o",
            lastName: "Garc�a",
            organizationName: "Soci�t� G�n�rale");

        // Act
        var response = await _handler.HandleAsync(command);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual("Jo�o", response.FirstName);
        Assert.AreEqual("Garc�a", response.LastName);
    }

    [TestMethod]
    public async Task HandleAsync_HyphenatedNames_Succeeds()
    {
        // Arrange
        var command = RegisterUserRequestFactory.Create(
            email: "hyphenated@example.com",
            password: "SecurePass123!@#",
            firstName: "Jean-Pierre",
            lastName: "O'Brien",
            organizationName: "ACME");

        // Act
        var response = await _handler.HandleAsync(command);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual("Jean-Pierre", response.FirstName);
        Assert.AreEqual("O'Brien", response.LastName);
    }

    [TestMethod]
    public async Task HandleAsync_RepositorySaveThrows_PropagatesException()
    {
        var command = RegisterUserRequestFactory.Create(
            email: "error@example.com",
            password: "SecurePass123!@#",
            firstName: "Error",
            lastName: "Test",
            organizationName: "ACME");

        _mockUserRepository.Setup(r => r.SaveAsync())
            .ThrowsAsync(new InvalidOperationException("Database error"));

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => _handler.HandleAsync(command));
    }
}
