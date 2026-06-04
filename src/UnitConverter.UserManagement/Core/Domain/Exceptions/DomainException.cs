using System;

namespace UnitConverter.UserManagement.Core.Domain.Exceptions;

/// <summary>
/// Base exception for all domain validation failures.
/// Use domain-specific exceptions that inherit from this for specific validation rules.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
