using FluentValidation;
using UnitConverter.UnitsDefinitions.Catalog;
using UnitConverter.UnitsDefinitions.Contracts.Requests;

namespace UnitConverter.UnitsDefinitions.Api.Validators;

public sealed class RejectUnitRequestValidator : AbstractValidator<RejectUnitRequest>
{
    public const int RejectionReasonMaxLength = 500;

    public RejectUnitRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(RejectionReasonMaxLength)
            .WithMessage(UnitCatalogValidationMessages.RejectionReasonMaxLength(RejectionReasonMaxLength))
            .When(x => x.Reason is not null);
    }
}
