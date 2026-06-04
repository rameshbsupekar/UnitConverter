namespace UnitConverter.UnitsDefinitions.Catalog;

/// <summary>
/// FluentValidation messages for unit definition requests.
/// </summary>
public static class UnitCatalogValidationMessages
{
    public const string SymbolRequired = "Symbol is required.";
    public const string DisplayNameRequired = "Display name is required.";
    public const string MultiplierMustBePositive = "MultiplierToBase must be greater than zero.";
    public const string CategoryRequired = "Category is required.";

    public static string SymbolMaxLength(int max) => $"Symbol cannot exceed {max} characters.";
    public static string DisplayNameMaxLength(int max) => $"Display name cannot exceed {max} characters.";
    public static string RejectionReasonMaxLength(int max) => $"Rejection reason cannot exceed {max} characters.";
}
