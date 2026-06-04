using FluentValidation;
using UnitConverter.UserManagement.Application.Constants;
using UnitConverter.UserManagement.Contracts.Requests;
using UnitConverter.Common.Constants.Validation;
using UnitConverter.Common.Security;
using UnitConverter.Common.Validation;

namespace UnitConverter.UserManagement.Application.Validators;

/// <summary>
/// Fluent validator for <see cref="RegisterUserRequest"/>.
/// </summary>
public sealed class RegisterUserValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(AuthValidationMessages.EmailRequired)
            .EmailAddress().WithMessage(AuthValidationMessages.InvalidEmailFormat);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(AuthValidationMessages.PasswordRequired)
            .MinimumLength(FieldLengthLimits.PasswordMinLength)
                .WithMessage(AuthValidationMessages.PasswordMinLength(FieldLengthLimits.PasswordMinLength))
            .MaximumLength(FieldLengthLimits.PasswordMaxLength)
                .WithMessage(AuthValidationMessages.PasswordMaxLength(FieldLengthLimits.PasswordMaxLength))
            .Custom((password, context) =>
            {
                if (!PasswordPolicy.IsValid(password))
                {
                    context.AddFailure("password", PasswordPolicy.RequirementsMessage);
                }
            });

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage(AuthValidationMessages.FirstNameRequired)
            .Length(1, FieldLengthLimits.FirstNameMaxLength)
                .WithMessage(AuthValidationMessages.NameLengthRange("First name", FieldLengthLimits.FirstNameMaxLength))
            .Matches(NamePattern.PersonName).WithMessage(AuthValidationMessages.InvalidFirstNameCharacters);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage(AuthValidationMessages.LastNameRequired)
            .Length(1, FieldLengthLimits.LastNameMaxLength)
                .WithMessage(AuthValidationMessages.NameLengthRange("Last name", FieldLengthLimits.LastNameMaxLength))
            .Matches(NamePattern.PersonName).WithMessage(AuthValidationMessages.InvalidLastNameCharacters);

        RuleFor(x => x.OrganizationName)
            .NotEmpty().WithMessage(AuthValidationMessages.OrganizationNameRequired)
            .Length(1, FieldLengthLimits.OrganizationNameMaxLength)
                .WithMessage(AuthValidationMessages.OrganizationNameLengthRange(FieldLengthLimits.OrganizationNameMaxLength));

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage(AuthValidationMessages.IdempotencyKeyRequired);
    }
}
