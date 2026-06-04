using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UnitConverter.UserManagement.Common.Interfaces;
using UnitConverter.UserManagement.Core.Domain.Entities;
using UnitConverter.UserManagement.Core.Domain.ValueObjects;
using UnitConverter.UserManagement.DataAccess.Data;

namespace UnitConverter.UserManagement.DataAccess.Repositories;

/// <inheritdoc />
public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;

    public UserRepository(AuthDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(object id)
    {
        if (id is not long userId || userId <= 0)
            return null;

        return await _context.Users.FirstOrDefaultAsync(u => u.Id == UserId.Create(userId));
    }

    /// <inheritdoc />
    public async Task<User?> FirstOrDefaultAsync(Expression<Func<User, bool>> predicate)
    {
        return await _context.Users.FirstOrDefaultAsync(predicate);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> FindAsync(Expression<Func<User, bool>> predicate)
    {
        return await _context.Users.Where(predicate).ToListAsync();
    }

    /// <inheritdoc />
    public async Task AddAsync(User entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        await _context.Users.AddAsync(entity);
    }

    /// <inheritdoc />
    public void Update(User entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        _context.Users.Update(entity);
    }

    /// <inheritdoc />
    public void Remove(User entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        _context.Users.Remove(entity);
    }

    /// <inheritdoc />
    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(Email email)
    {
        if (email is null)
            throw new ArgumentNullException(nameof(email));

        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <inheritdoc />
    public async Task<bool> EmailExistsAsync(Email email)
    {
        if (email is null)
            throw new ArgumentNullException(nameof(email));

        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    /// <inheritdoc />
    public async Task<User?> GetWithRolesAsync(UserId userId)
    {
        if (userId is null)
            throw new ArgumentNullException(nameof(userId));

        return await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
}
