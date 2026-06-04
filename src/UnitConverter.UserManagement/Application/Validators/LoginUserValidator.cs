using FluentValidation;
using UnitConverter.UserManagement.Application.Constants;

namespace UnitConverter.UserManagement.Application.Validators;

public sealed class LoginUserValidator : AbstractValidator<LoginRequest>
{
    public LoginUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(AuthValidationMessages.EmailRequired)
            .EmailAddress().WithMessage(AuthValidationMessages.InvalidEmailFormat);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(AuthValidationMessages.PasswordRequired);
    }
}
