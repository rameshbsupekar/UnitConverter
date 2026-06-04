namespace UnitConverter.UnitsDefinitions.Catalog;

/// <summary>
/// User-facing unit catalog error and validation messages.
/// </summary>
public static class UnitCatalogMessages
{
    public const string DisplayNameRequired = "Display name is required.";
    public const string MultiplierMustBePositive = "MultiplierToBase must be greater than zero.";
    public const string UnitCreatedButNotLoaded = "Unit was created but could not be loaded.";

    public const string DuplicateUnitFormat = "Unit '{0}' already exists in category {1}.";
    public const string UnitNotFoundByIdFormat = "Unit with id {0} was not found.";
    public const string CannotApproveFormat = "Unit with id {0} cannot be approved from status {1}.";
    public const string CannotRejectFormat = "Unit with id {0} cannot be rejected from status {1}.";
}
