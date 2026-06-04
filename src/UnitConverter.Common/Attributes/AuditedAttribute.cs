namespace UnitConverter.Common.Attributes;

/// <summary>
/// Marks a controller or action for security audit logging in middleware.
/// Apply at controller level to audit all actions; use per-action to opt in selectively.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AuditedAttribute : Attribute
{
    /// <summary>Optional fixed audit action name; otherwise inferred from the action method name.</summary>
    public string? Action { get; init; }
}
