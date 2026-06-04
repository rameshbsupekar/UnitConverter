using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using UnitConverter.UserManagement.Common.Interfaces;
using UnitConverter.UserManagement.DataAccess.Data;

namespace UnitConverter.UserManagement.DataAccess.Repositories;

/// <inheritdoc />
public sealed class UnitOfWork : IUnitOfWork, IDisposable, IAsyncDisposable
{
    private readonly AuthDbContext _context;
    private IUserRepository? _userRepository;
    private IRoleRepository? _roleRepository;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AuthDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public IUserRepository Users => _userRepository ??= new UserRepository(_context);

    /// <inheritdoc />
    public IRoleRepository Roles => _roleRepository ??= new RoleRepository(_context);

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
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
