using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UnitConverter.Common.Security;
using UnitConverter.UserManagement.Common.Interfaces;
using UnitConverter.UserManagement.Core.Domain.Entities;
using UnitConverter.UserManagement.DataAccess.Data;

namespace UnitConverter.UserManagement.DataAccess.Repositories;

/// <inheritdoc />
public class RoleRepository : IRoleRepository
{
    private readonly AuthDbContext _context;

    public RoleRepository(AuthDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<Role?> GetByIdAsync(object id)
    {
        if (id is not int roleId || roleId <= 0)
            return null;

        return await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
    }

    /// <inheritdoc />
    public async Task<Role?> FirstOrDefaultAsync(Expression<Func<Role, bool>> predicate)
    {
        return await _context.Roles.FirstOrDefaultAsync(predicate);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Role>> GetAllAsync()
    {
        return await _context.Roles.ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Role>> FindAsync(Expression<Func<Role, bool>> predicate)
    {
        return await _context.Roles.Where(predicate).ToListAsync();
    }

    /// <inheritdoc />
    public async Task AddAsync(Role entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        await _context.Roles.AddAsync(entity);
    }

    /// <inheritdoc />
    public void Update(Role entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        _context.Roles.Update(entity);
    }

    /// <inheritdoc />
    public void Remove(Role entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        _context.Roles.Remove(entity);
    }

    /// <inheritdoc />
    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<Role?> GetByNameAsync(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            throw new ArgumentException("Role name cannot be empty.", nameof(roleName));

        return await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Role>> GetDefaultRolesAsync()
    {
        var defaultRoleNames = RoleNames.All;
        return await _context.Roles
            .Where(r => defaultRoleNames.Contains(r.Name))
            .ToListAsync();
    }
}
