namespace UnitConverter.Contracts.Auth.Responses;

/// <summary>
/// Standardized error response for API failures
/// </summary>
public record ErrorResponse(
    string Type,
    string Title,
    int Status,
    string Detail,
    string TraceId,
    string CorrelationId
);
