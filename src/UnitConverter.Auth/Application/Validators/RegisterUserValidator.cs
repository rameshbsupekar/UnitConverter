using System;
using FluentValidation;
using UnitConverter.Auth.Application.Commands;
using UnitConverter.Auth.Common.Constants;
using UnitConverter.Auth.Common.Interfaces;

namespace UnitConverter.Auth.Application.Validators;

/// <summary>
/// Fluent validator for RegisterUserCommand.
/// Enforces all business rules for user registration including:
/// - Email format and uniqueness
/// - Password strength and length
/// - Name length and character restrictions
/// - Organization name requirements
/// - Idempotency key presence
/// </summary>
public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    private readonly IUserRepository _userRepository;

    public RegisterUserValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

        // Email validation
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        // Password validation
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(SecurityConstants.PasswordMinLength)
                .WithMessage("Password must be at least " + SecurityConstants.PasswordMinLength + " characters")
            .MaximumLength(SecurityConstants.PasswordMaxLength)
                .WithMessage("Password cannot exceed " + SecurityConstants.PasswordMaxLength + " characters")
            .Custom((password, context) =>
            {
                if (!SecurityConstants.IsValidPassword(password))
                {
                    context.AddFailure("password", SecurityConstants.PasswordRequirementsMessage);
                }
            });

        // First name validation
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .Length(1, 100).WithMessage("First name must be 1-100 characters")
            .Matches(@"^[a-zA-Z\s\-']*$").WithMessage("First name contains invalid characters");

        // Last name validation
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .Length(1, 100).WithMessage("Last name must be 1-100 characters")
            .Matches(@"^[a-zA-Z\s\-']*$").WithMessage("Last name contains invalid characters");

        // Organization name validation
        RuleFor(x => x.OrganizationName)
            .NotEmpty().WithMessage("Organization name is required")
            .Length(1, 200).WithMessage("Organization name must be 1-200 characters");

        // Idempotency key validation
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("Idempotency key is required");
    }
}