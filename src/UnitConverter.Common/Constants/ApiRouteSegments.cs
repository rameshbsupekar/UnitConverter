namespace UnitConverter.Common.Constants;

/// <summary>
/// Route path segments for versioned APIs (<c>api/v{version}/...</c>).
/// Use nouns that describe the resource or use case, not roles (e.g. avoid <c>admin</c> in URLs).
/// </summary>
public static class ApiRouteSegments
{
    // --- User Management API (identity & access) ---

    /// <summary>POST — create a user account (registration).</summary>
    public const string Users = "users";

    /// <summary>POST — create a session (sign-in, returns tokens).</summary>
    public const string Sessions = "sessions";

    /// <summary>POST — under <see cref="Sessions"/>; renew access token using refresh token.</summary>
    public const string SessionRefresh = "refresh";

    // --- Units API: public catalog (read-only, approved data) ---

    /// <summary>Public unit catalog root.</summary>
    public const string Catalog = "/catalog";

    /// <summary>GET — list measurement categories (length, mass, temperature).</summary>
    public const string Categories = "categories";

    /// <summary>GET — list approved units in a category (filter via query).</summary>
    public const string CatalogUnits = "units";

    // --- Units API: conversion ---

    /// <summary>POST — convert a value between two units.</summary>
    public const string UnitConversions = "/unit-conversions";

    // --- Units API: unit master data (authenticated CRUD & approval workflow) ---

    /// <summary>Unit definitions master data (create, read, update, delete, approve, reject).</summary>
    public const string UnitDefinitions = "/unit-definitions";

    /// <summary>PUT — transition unit to approved (Admin).</summary>
    public const string Approve = "approve";

    /// <summary>PUT — transition unit to rejected (Admin).</summary>
    public const string Reject = "reject";

    /// <summary>PUT — Admin correction of definition regardless of submitter (Admin).</summary>
    public const string AdminCorrection = "admin-correction";

    /// <summary>Relative path (after version) for catalog category list; used in validation error hints.</summary>
    public const string CatalogCategoriesPath = Catalog + "/" + Categories;
}
