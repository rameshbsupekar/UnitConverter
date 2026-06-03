using System;

namespace UnitConverter.Auth.Core.Domain.Exceptions;

/// <summary>
/// Thrown when email validation fails.
/// This includes: empty values, invalid format, exceeding max length, etc.
/// </summary>
public class InvalidEmailException : DomainException
{
    public InvalidEmailException(string message) : base(message)
    {
    }
}
