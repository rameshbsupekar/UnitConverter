namespace UnitConverter.Common.Contracts.Responses;

/// <summary>
/// Standardized error response (RFC 7807-style) shared across all APIs.
/// </summary>
public sealed record ErrorResponse(
    string Type,
    string Title,
    int Status,
    string Detail,
    string TraceId,
    string CorrelationId);
