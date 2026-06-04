using FluentValidation;
using UnitConverter.UnitsDefinitions.Catalog;
using UnitConverter.UnitsDefinitions.Contracts.Requests;
using UnitConverter.UnitsDefinitions.Contracts.Units;

namespace UnitConverter.UnitsDefinitions.Api.Validators;

public sealed class CreateUnitRequestValidator : AbstractValidator<CreateUnitRequest>
{
    public const int SymbolMaxLength = 20;
    public const int DisplayNameMaxLength = 100;

    public CreateUnitRequestValidator()
    {
        RuleFor(x => x.Symbol)
            .NotEmpty().WithMessage(UnitCatalogValidationMessages.SymbolRequired)
            .MaximumLength(SymbolMaxLength)
            .WithMessage(UnitCatalogValidationMessages.SymbolMaxLength(SymbolMaxLength));

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage(UnitCatalogValidationMessages.DisplayNameRequired)
            .MaximumLength(DisplayNameMaxLength)
            .WithMessage(UnitCatalogValidationMessages.DisplayNameMaxLength(DisplayNameMaxLength));

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage(UnitCatalogValidationMessages.CategoryRequired);

        RuleFor(x => x.MultiplierToBase)
            .GreaterThan(0).WithMessage(UnitCatalogValidationMessages.MultiplierMustBePositive);
    }
}
