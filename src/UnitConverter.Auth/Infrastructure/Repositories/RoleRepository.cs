using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UnitConverter.Auth.Common.Interfaces;
using UnitConverter.Auth.Core.Domain.Entities;
using UnitConverter.Auth.Infrastructure.Data;

namespace UnitConverter.Auth.Infrastructure.Repositories;

/// <summary>
/// Repository for Role entity operations.
/// Implements both IRoleRepository (specialized) and IRepository<Role> (generic).
/// Provides CRUD operations and Role-specific queries.
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly AuthDbContext _context;

    public RoleRepository(AuthDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Role?> GetByIdAsync(object id)
    {
        if (id is not int roleId || roleId <= 0)
            return null;

        return await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
    }

    public async Task<Role?> FirstOrDefaultAsync(Expression<Func<Role, bool>> predicate)
    {
        return await _context.Roles.FirstOrDefaultAsync(predicate);
    }

    public async Task<IEnumerable<Role>> GetAllAsync()
    {
        return await _context.Roles.ToListAsync();
    }

    public async Task<IEnumerable<Role>> FindAsync(Expression<Func<Role, bool>> predicate)
    {
        return await _context.Roles.Where(predicate).ToListAsync();
    }

    public async Task AddAsync(Role entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        await _context.Roles.AddAsync(entity);
    }

    public void Update(Role entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        _context.Roles.Update(entity);
    }

    public void Remove(Role entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        _context.Roles.Remove(entity);
    }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<Role?> GetByNameAsync(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            throw new ArgumentException("Role name cannot be empty.", nameof(roleName));

        return await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
    }

    public async Task<IEnumerable<Role>> GetDefaultRolesAsync()
    {
        var defaultRoleNames = new[] { "Public", "Employee", "Partner", "Admin" };
        return await _context.Roles
            .Where(r => defaultRoleNames.Contains(r.Name))
            .ToListAsync();
    }
}
