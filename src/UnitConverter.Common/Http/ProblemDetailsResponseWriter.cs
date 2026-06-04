using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using UnitConverter.Common.Constants;
using UnitConverter.Common.Contracts.Responses;

namespace UnitConverter.Common.Http;

/// <summary>
/// Writes RFC 7807 <see cref="ErrorResponse"/> JSON payloads.
/// </summary>
public static class ProblemDetailsResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static Task WriteAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail) =>
        WriteAsync(context, statusCode, title, detail, context.TraceIdentifier, GetCorrelationId(context));

    public static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string traceId,
        string correlationId)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        var error = new ErrorResponse(
            Type: ApiErrorTypes.ForStatusCode(statusCode),
            Title: title,
            Status: statusCode,
            Detail: detail,
            TraceId: traceId,
            CorrelationId: correlationId);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = MediaTypes.ProblemJson;
        await context.Response.WriteAsync(JsonSerializer.Serialize(error, JsonOptions));
    }

    /// <summary>
    /// Production fallback when an exception escapes the pipeline (see <c>UseExceptionHandler</c>).
    /// </summary>
    public static async Task WriteExceptionHandlerResponseAsync(HttpContext context)
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        if (exception is ValidationException validation)
        {
            await WriteAsync(
                context,
                StatusCodes.Status400BadRequest,
                ProblemTitles.BadRequest,
                ValidationErrorFormatter.Format(validation));
            return;
        }

        await WriteAsync(
            context,
            StatusCodes.Status500InternalServerError,
            ProblemTitles.InternalServerError,
            ProblemTitles.UnexpectedErrorDetail);
    }

    private static string GetCorrelationId(HttpContext context) =>
        context.Request.Headers.TryGetValue(HttpHeaderNames.CorrelationId, out var correlationId)
            ? correlationId.ToString()
            : context.TraceIdentifier;
}
