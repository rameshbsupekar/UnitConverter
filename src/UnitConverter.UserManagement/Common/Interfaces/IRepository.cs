using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace UnitConverter.UserManagement.Common.Interfaces;

/// <summary>
/// Generic repository interface for data access abstraction.
/// Provides CRUD operations and query capabilities for any entity type.
/// </summary>
/// <typeparam name="TEntity">The entity type this repository manages.</typeparam>
public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The entity''s unique identifier.</param>
    /// <returns>The entity if found; otherwise null.</returns>
    Task<TEntity?> GetByIdAsync(object id);

    /// <summary>
    /// Retrieves the first entity matching the predicate.
    /// </summary>
    /// <param name="predicate">The condition to match.</param>
    /// <returns>The first entity matching the condition; otherwise null.</returns>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Retrieves all entities.
    /// </summary>
    /// <returns>All entities of this type.</returns>
    Task<IEnumerable<TEntity>> GetAllAsync();

    /// <summary>
    /// Retrieves all entities matching the predicate.
    /// </summary>
    /// <param name="predicate">The condition to match.</param>
    /// <returns>All entities matching the condition.</returns>
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    Task AddAsync(TEntity entity);

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity with updated values.</param>
    void Update(TEntity entity);

    /// <summary>
    /// Removes an entity from the repository.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    void Remove(TEntity entity);

    /// <summary>
    /// Persists all changes to the repository.
    /// </summary>
    Task SaveAsync();
}
