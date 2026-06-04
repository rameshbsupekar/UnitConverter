using FluentValidation;
using FluentValidation.Results;

namespace UnitConverter.Common.Http;

/// <summary>
/// Formats FluentValidation failures for RFC 7807 detail strings.
/// </summary>
public static class ValidationErrorFormatter
{
    public static string Format(ValidationException exception) =>
        Format(exception.Errors);

    public static string Format(IEnumerable<ValidationFailure> failures) =>
        string.Join("; ", failures.Select(static f => $"{f.PropertyName}: {f.ErrorMessage}"));
}
