using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnitConverter.Auth.Core.Domain.Entities;
using UnitConverter.Auth.Core.Domain.ValueObjects;

namespace UnitConverter.Auth.Common.Interfaces;

/// <summary>
/// Repository interface specifically for User entities.
/// Extends generic repository with User-specific query operations.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">The email value object to search for.</param>
    /// <returns>The user with the specified email; otherwise null.</returns>
    Task<User?> GetByEmailAsync(Email email);

    /// <summary>
    /// Checks whether an email address already exists in the system.
    /// </summary>
    /// <param name="email">The email value object to check.</param>
    /// <returns>True if the email exists; otherwise false.</returns>
    Task<bool> EmailExistsAsync(Email email);

    /// <summary>
    /// Retrieves a user with their assigned roles eagerly loaded.
    /// </summary>
    /// <param name="userId">The user''s unique identifier.</param>
    /// <returns>The user with roles loaded; otherwise null.</returns>
    Task<User?> GetWithRolesAsync(UserId userId);
}
