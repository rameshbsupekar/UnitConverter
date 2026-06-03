using System;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using UnitConverter.Auth.Application.Commands;
using UnitConverter.Auth.Application.DTOs;
using UnitConverter.Auth.Application.Handlers;
using UnitConverter.Auth.Common.Interfaces;
using UnitConverter.Auth.Core.Domain.Entities;
using UnitConverter.Auth.Core.Domain.Exceptions;
using UnitConverter.Auth.Core.Domain.ValueObjects;
using UnitConverter.Auth.Tests.Fixtures;

namespace UnitConverter.Auth.Tests.Unit.Application;

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
    private Mock<IValidator<RegisterUserCommand>> _mockValidator = null!;
    private Mock<Microsoft.Extensions.Logging.ILogger<RegisterUserCommandHandler>> _mockLogger = null!;
    private RegisterUserCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockValidator = new Mock<IValidator<RegisterUserCommand>>();
        _mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<RegisterUserCommandHandler>>();

        // Setup default mock behavior
        _mockUserRepository
            .Setup(r => r.EmailExistsAsync(It.IsAny<Email>()))
            .ReturnsAsync(false);

        _mockPasswordHasher
            .Setup(h => h.HashPassword(It.IsAny<string>()))
            .Returns("hashed_password_bcrypt_12345678901");

        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<RegisterUserCommand>(), default))
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
        var command = new RegisterUserCommand
        {
            Email = "john@example.com",
            Password = "SecurePass123!@#",
            FirstName = "John",
            LastName = "Doe",
            OrganizationName = "ACME Corp",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

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
        var command = new RegisterUserCommand
        {
            Email = "jane@example.com",
            Password = "ValidPass123!@#",
            FirstName = "Jane",
            LastName = "Smith",
            OrganizationName = "TechCorp",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        // Act
        var response = await _handler.HandleAsync(command);

        // Assert
        Assert.IsTrue(response.UserId > 0, "UserId should be populated");
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
        var command = new RegisterUserCommand
        {
            Email = "test@example.com",
            Password = "StrongPass123!@#",
            FirstName = "Test",
            LastName = "User",
            OrganizationName = "TestOrg",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [TestMethod]
    public async Task HandleAsync_ValidCommand_CallsSaveAsync_Once()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "persist@example.com",
            Password = "ValidPass123!@#",
            FirstName = "Persist",
            LastName = "Test",
            OrganizationName = "PersistOrg",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _mockUserRepository.Verify(r => r.SaveAsync(), Times.Once);
    }

    // ===== SCENARIO 2: Email Validation =====

    [TestMethod]
    [ExpectedException(typeof(UserAlreadyExistsException))]
    public async Task HandleAsync_DuplicateEmail_ThrowsUserAlreadyExistsException()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "existing@example.com",
            Password = "SecurePass123!@#",
            FirstName = "John",
            LastName = "Doe",
            OrganizationName = "ACME",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        _mockUserRepository.Setup(r => r.EmailExistsAsync(It.IsAny<Email>()))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(command);

        // Assert - should throw exception
    }

    [TestMethod]
    public async Task HandleAsync_DuplicateEmail_DoesNotSaveUser()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "duplicate@example.com",
            Password = "SecurePass123!@#",
            FirstName = "Duplicate",
            LastName = "User",
            OrganizationName = "OrgName",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

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
    [ExpectedException(typeof(ValidationException))]
    public async Task HandleAsync_InvalidEmailFormat_ThrowsValidationException()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "invalid.email",
            Password = "SecurePass123!@#",
            FirstName = "John",
            LastName = "Doe",
            OrganizationName = "ACME",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        var validationFailure = new FluentValidation.Results.ValidationFailure("Email", "Invalid email format");
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterUserCommand>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[] { validationFailure }));

        // Act
        await _handler.HandleAsync(command);

        // Assert - should throw ValidationException
    }

    // ===== SCENARIO 3: Password Validation =====

    [TestMethod]
    [ExpectedException(typeof(ValidationException))]
    public async Task HandleAsync_WeakPassword_ThrowsValidationException()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "test@example.com",
            Password = "weak",
            FirstName = "John",
            LastName = "Doe",
            OrganizationName = "ACME",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        var validationFailure = new FluentValidation.Results.ValidationFailure("Password", "Password must be at least 12 characters");
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterUserCommand>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[] { validationFailure }));

        // Act
        await _handler.HandleAsync(command);

        // Assert - should throw ValidationException
    }

    [TestMethod]
    [ExpectedException(typeof(ValidationException))]
    public async Task HandleAsync_EmptyPassword_ThrowsValidationException()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "test@example.com",
            Password = "",
            FirstName = "John",
            LastName = "Doe",
            OrganizationName = "ACME",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        var validationFailure = new FluentValidation.Results.ValidationFailure("Password", "Password is required");
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterUserCommand>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[] { validationFailure }));

        // Act
        await _handler.HandleAsync(command);

        // Assert - should throw ValidationException
    }

    // ===== SCENARIO 4: Name Validation =====

    [TestMethod]
    [ExpectedException(typeof(ValidationException))]
    public async Task HandleAsync_EmptyFirstName_ThrowsValidationException()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "test@example.com",
            Password = "SecurePass123!@#",
            FirstName = "",
            LastName = "Doe",
            OrganizationName = "ACME",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        var validationFailure = new FluentValidation.Results.ValidationFailure("FirstName", "First name is required");
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterUserCommand>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[] { validationFailure }));

        // Act
        await _handler.HandleAsync(command);

        // Assert - should throw ValidationException
    }

    [TestMethod]
    [ExpectedException(typeof(ValidationException))]
    public async Task HandleAsync_EmptyLastName_ThrowsValidationException()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "test@example.com",
            Password = "SecurePass123!@#",
            FirstName = "John",
            LastName = "",
            OrganizationName = "ACME",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        var validationFailure = new FluentValidation.Results.ValidationFailure("LastName", "Last name is required");
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterUserCommand>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[] { validationFailure }));

        // Act
        await _handler.HandleAsync(command);

        // Assert - should throw ValidationException
    }

    [TestMethod]
    public async Task HandleAsync_VeryLongNames_Succeeds()
    {
        // Arrange
        var longName = new string('A', 100);
        var command = new RegisterUserCommand
        {
            Email = "longname@example.com",
            Password = "SecurePass123!@#",
            FirstName = longName,
            LastName = longName,
            OrganizationName = "ACME",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        // Act
        var response = await _handler.HandleAsync(command);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(longName, response.FirstName);
        Assert.AreEqual(longName, response.LastName);
    }

    // ===== SCENARIO 5: Organization Validation =====

    [TestMethod]
    [ExpectedException(typeof(ValidationException))]
    public async Task HandleAsync_EmptyOrganization_ThrowsValidationException()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "test@example.com",
            Password = "SecurePass123!@#",
            FirstName = "John",
            LastName = "Doe",
            OrganizationName = "",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        var validationFailure = new FluentValidation.Results.ValidationFailure("OrganizationName", "Organization name is required");
        _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterUserCommand>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[] { validationFailure }));

        // Act
        await _handler.HandleAsync(command);

        // Assert - should throw ValidationException
    }

    [TestMethod]
    public async Task HandleAsync_VeryLongOrganization_Succeeds()
    {
        // Arrange
        var longOrgName = new string('O', 200);
        var command = new RegisterUserCommand
        {
            Email = "longorg@example.com",
            Password = "SecurePass123!@#",
            FirstName = "John",
            LastName = "Doe",
            OrganizationName = longOrgName,
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        // Act
        var response = await _handler.HandleAsync(command);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual(longOrgName, response.OrganizationName);
    }

    // ===== SCENARIO 6: Dependencies (Null Checks) =====

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void HandleAsync_NullUserRepository_ThrowsArgumentNullException()
    {
        // Act
        new RegisterUserCommandHandler(null!, _mockPasswordHasher.Object, _mockValidator.Object, _mockLogger.Object);

        // Assert - should throw ArgumentNullException
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void HandleAsync_NullPasswordHasher_ThrowsArgumentNullException()
    {
        // Act
        new RegisterUserCommandHandler(_mockUserRepository.Object, null!, _mockValidator.Object, _mockLogger.Object);

        // Assert - should throw ArgumentNullException
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void HandleAsync_NullValidator_ThrowsArgumentNullException()
    {
        // Act
        new RegisterUserCommandHandler(_mockUserRepository.Object, _mockPasswordHasher.Object, null!, _mockLogger.Object);

        // Assert - should throw ArgumentNullException
    }

    // ===== EDGE CASES =====

    [TestMethod]
    public async Task HandleAsync_UnicodeCharactersInNames_Succeeds()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "unicode@example.com",
            Password = "SecurePass123!@#",
            FirstName = "João",
            LastName = "García",
            OrganizationName = "Société Générale",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        // Act
        var response = await _handler.HandleAsync(command);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual("João", response.FirstName);
        Assert.AreEqual("García", response.LastName);
    }

    [TestMethod]
    public async Task HandleAsync_HyphenatedNames_Succeeds()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "hyphenated@example.com",
            Password = "SecurePass123!@#",
            FirstName = "Jean-Pierre",
            LastName = "O'Brien",
            OrganizationName = "ACME",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        // Act
        var response = await _handler.HandleAsync(command);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreEqual("Jean-Pierre", response.FirstName);
        Assert.AreEqual("O'Brien", response.LastName);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public async Task HandleAsync_RepositorySaveThrows_PropagatesException()
    {
        // Arrange
        var command = new RegisterUserCommand
        {
            Email = "error@example.com",
            Password = "SecurePass123!@#",
            FirstName = "Error",
            LastName = "Test",
            OrganizationName = "ACME",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        _mockUserRepository.Setup(r => r.SaveAsync())
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        await _handler.HandleAsync(command);

        // Assert - should propagate exception
    }
}
