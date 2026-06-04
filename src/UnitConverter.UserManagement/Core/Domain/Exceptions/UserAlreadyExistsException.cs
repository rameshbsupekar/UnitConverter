using System;
using UnitConverter.UserManagement.Core.Domain.Exceptions;

namespace UnitConverter.UserManagement.Core.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to register a user with an email that already exists.
/// </summary>
public sealed class UserAlreadyExistsException : DomainException
{
    public UserAlreadyExistsException(string message) : base(message)
    {
    }

    public UserAlreadyExistsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
