using System;

namespace UnitConverter.UserManagement.Core.Domain.Exceptions;

/// <summary>
/// Thrown when password validation fails.
/// This includes: insufficient length, missing complexity requirements (uppercase, lowercase, digit, special char).
/// </summary>
public class WeakPasswordException : DomainException
{
    public WeakPasswordException(string message) : base(message)
    {
    }
}
