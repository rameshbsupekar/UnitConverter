using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.Logging;
using UnitConverter.UserManagement.Common.Interfaces;
using UnitConverter.UserManagement.Core.Domain.Entities;
using UnitConverter.UserManagement.Core.Domain.Exceptions;
using UnitConverter.UserManagement.Application.Constants;
using UnitConverter.UserManagement.Core.Domain.ValueObjects;

namespace UnitConverter.UserManagement.Application.Handlers;

/// <summary>
/// Handler for RegisterUserRequest.
/// Orchestrates user registration:
/// 1. Validates command using Fluent Validation
/// 2. Checks for duplicate email
/// 3. Creates domain aggregate
/// 4. Persists to repository
/// 5. Returns response DTO
///
/// Uses constructor injection for dependencies and proper null checks.
/// All operations are async-aware and logged.
/// </summary>
public sealed class RegisterUserCommandHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<RegisterUserRequest> _validator;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IValidator<RegisterUserRequest> validator,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles user registration command.
    /// 
    /// Process:
    /// 1. Validate command
    /// 2. Create Email value object (domain validation)
    /// 3. Check if email already exists
    /// 4. Hash password securely
    /// 5. Create User aggregate
    /// 6. Save to repository
    /// 7. Return user response
    /// </summary>
    public async Task<UserResponse> HandleAsync(RegisterUserRequest command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        // 1. Validate command
        var validationResult = await _validator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Registration validation failed: {Errors}", string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Create Email value object (domain validation)
        var email = Email.Create(command.Email);

        // 3. Check if email already exists
        if (await _userRepository.EmailExistsAsync(email))
        {
            _logger.LogWarning("Registration failed: email {Email} already exists", email.Value);
            throw new UserAlreadyExistsException(
                string.Format(AuthSecurityMessages.UserAlreadyExistsFormat, email.Value));
        }

        // 4. Hash password securely
        var passwordHash = _passwordHasher.HashPassword(command.Password);

        // 5. Create User aggregate
        var userId = GenerateNewUserId();
        var user = User.Create(
            userId,
            command.Email,
            command.FirstName,
            command.LastName,
            command.OrganizationName,
            passwordHash
        );

        // 6. Save to repository
        await _userRepository.AddAsync(user);
        await _userRepository.SaveAsync();

        _logger.LogInformation("User registered successfully: UserId={UserId}, Email={Email}", userId, email.Value);

        // 7. Return response
        return new UserResponse(
            Id: user.Id.Value,
            Email: user.Email.Value,
            FirstName: user.FirstName,
            LastName: user.LastName,
            OrganizationName: user.OrganizationName,
            CreatedAt: user.CreatedAt,
            Roles: user.Roles.Select(r => r.Name).ToArray());
    }

    /// <summary>
    /// Generates a new user ID.
    /// Uses Unix timestamp milliseconds for simple but distributed-friendly ID generation.
    /// In production, consider: sequential IDs (database), Guids, or snowflake IDs.
    /// </summary>
    private static long GenerateNewUserId()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
