using FluentValidation;
using UnitConverter.UnitsDefinitions.Catalog;
using UnitConverter.UnitsDefinitions.Contracts.Requests;

namespace UnitConverter.UnitsDefinitions.Api.Validators;

public sealed class UpdateUnitRequestValidator : AbstractValidator<UpdateUnitRequest>
{
    public const int DisplayNameMaxLength = 100;

    public UpdateUnitRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage(UnitCatalogValidationMessages.DisplayNameRequired)
            .MaximumLength(DisplayNameMaxLength)
            .WithMessage(UnitCatalogValidationMessages.DisplayNameMaxLength(DisplayNameMaxLength));

        RuleFor(x => x.MultiplierToBase)
            .GreaterThan(0).WithMessage(UnitCatalogValidationMessages.MultiplierMustBePositive);
    }
}
