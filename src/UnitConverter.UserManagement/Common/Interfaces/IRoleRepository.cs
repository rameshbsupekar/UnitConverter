using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnitConverter.UserManagement.Core.Domain.Entities;

namespace UnitConverter.UserManagement.Common.Interfaces;

/// <summary>
/// Repository interface specifically for Role entities.
/// Extends generic repository with Role-specific query operations.
/// </summary>
public interface IRoleRepository : IRepository<Role>
{
    /// <summary>
    /// Retrieves a role by its name.
    /// </summary>
    /// <param name="roleName">The name of the role to retrieve.</param>
    /// <returns>The role with the specified name; otherwise null.</returns>
    Task<Role?> GetByNameAsync(string roleName);

    /// <summary>
    /// Retrieves all default roles that should be assigned to new users.
    /// </summary>
    /// <returns>A collection of default roles.</returns>
    Task<IEnumerable<Role>> GetDefaultRolesAsync();
}
