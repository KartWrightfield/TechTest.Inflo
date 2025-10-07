using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data.Entities;

namespace UserManagement.Data;

public interface IDataContext
{
    DbSet<Log> Logs { get; set; }

    /// <summary>
    /// Get a list of items
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    Task<IQueryable<TEntity>> GetAll<TEntity>() where TEntity : class;

    /// <summary>
    /// Get an item by its ID
    /// </summary>
    /// <param name="id">The primary key ID of the entity</param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns>The entity if found, null otherwise</returns>
    Task<TEntity?> GetById<TEntity>(long id) where TEntity : class;

    /// <summary>
    /// Create a new item
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task Create<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>
    /// Update an existing item matching the ID
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task Update<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>
    /// Delete an item matching the ID
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    Task Delete<TEntity>(TEntity entity) where TEntity : class;
}
