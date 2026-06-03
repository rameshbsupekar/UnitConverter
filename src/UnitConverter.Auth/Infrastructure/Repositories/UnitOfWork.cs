using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using UnitConverter.Auth.Common.Interfaces;
using UnitConverter.Auth.Infrastructure.Data;

namespace UnitConverter.Auth.Infrastructure.Repositories;

/// <summary>
/// Unit of Work pattern implementation for coordinating multiple repositories
/// and managing transactions across the Auth Service data layer.
/// 
/// Responsibilities:
/// - Provide access to specialized repositories (Users, Roles)
/// - Coordinate SaveChangesAsync across all repositories
/// - Manage database transactions (begin, commit, rollback)
/// - Ensure all changes are persisted as atomic units
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AuthDbContext _context;
    private IUserRepository? _userRepository;
    private IRoleRepository? _roleRepository;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AuthDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IUserRepository Users => _userRepository ??= new UserRepository(_context);

    public IRoleRepository Roles => _roleRepository ??= new RoleRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<IDisposable> BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
        return _transaction;
    }

    public async Task CommitAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            if (_transaction is not null)
            {
                await _transaction.CommitAsync();
            }
        }
        catch
        {
            if (_transaction is not null)
            {
                await _transaction.RollbackAsync();
            }
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackAsync()
    {
        try
        {
            if (_transaction is not null)
            {
                await _transaction.RollbackAsync();
            }
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
        }
        
        if (_context is not null)
        {
            await _context.DisposeAsync();
        }
    }
}
