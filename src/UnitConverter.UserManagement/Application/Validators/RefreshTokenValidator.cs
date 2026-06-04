using FluentValidation;
using UnitConverter.UserManagement.Application.Constants;

namespace UnitConverter.UserManagement.Application.Validators;

public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenValidator() =>
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage(AuthValidationMessages.RefreshTokenRequired);
}
