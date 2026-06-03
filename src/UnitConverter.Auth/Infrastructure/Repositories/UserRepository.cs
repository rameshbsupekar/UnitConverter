using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UnitConverter.Auth.Common.Interfaces;
using UnitConverter.Auth.Core.Domain.Entities;
using UnitConverter.Auth.Core.Domain.ValueObjects;
using UnitConverter.Auth.Infrastructure.Data;

namespace UnitConverter.Auth.Infrastructure.Repositories;

/// <summary>
/// Repository for User entity operations.
/// Implements both IUserRepository (specialized) and IRepository<User> (generic).
/// Provides CRUD operations and User-specific queries.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;

    public UserRepository(AuthDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<User?> GetByIdAsync(object id)
    {
        if (id is not long userId || userId <= 0)
            return null;

        return await _context.Users.FirstOrDefaultAsync(u => u.Id.Value == userId);
    }

    public async Task<User?> FirstOrDefaultAsync(Expression<Func<User, bool>> predicate)
    {
        return await _context.Users.FirstOrDefaultAsync(predicate);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<IEnumerable<User>> FindAsync(Expression<Func<User, bool>> predicate)
    {
        return await _context.Users.Where(predicate).ToListAsync();
    }

    public async Task AddAsync(User entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        await _context.Users.AddAsync(entity);
    }

    public void Update(User entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        _context.Users.Update(entity);
    }

    public void Remove(User entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        _context.Users.Remove(entity);
    }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<User?> GetByEmailAsync(Email email)
    {
        if (email is null)
            throw new ArgumentNullException(nameof(email));

        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> EmailExistsAsync(Email email)
    {
        if (email is null)
            throw new ArgumentNullException(nameof(email));

        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<User?> GetWithRolesAsync(UserId userId)
    {
        if (userId is null)
            throw new ArgumentNullException(nameof(userId));

        return await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
}
