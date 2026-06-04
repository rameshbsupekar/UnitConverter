using System;
using System.Threading.Tasks;

namespace UnitConverter.UserManagement.Common.Interfaces;

/// <summary>
/// Unit of Work pattern implementation for coordinating multiple repositories
/// and transaction management across the Auth Service data layer.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Repository for User entity operations.
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Repository for Role entity operations.
    /// </summary>
    IRoleRepository Roles { get; }

    /// <summary>
    /// Persists all changes across all repositories in a single transaction.
    /// </summary>
    /// <returns>The number of entities persisted.</returns>
    Task<int> SaveChangesAsync();

    /// <summary>
    /// Begins a new database transaction.
    /// The returned disposable should be disposed to rollback or commit.
    /// </summary>
    /// <returns>A disposable transaction object.</returns>
    Task<IDisposable> BeginTransactionAsync();
}
