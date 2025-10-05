using System.Collections.Generic;
using System.Threading.Tasks;
using UserManagement.Data.Entities;

namespace UserManagement.Services.Interfaces;

public interface IUserService
{
    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="user">The user object to be created.</param>
    /// <returns></returns>
    Task Create(User user);

    /// <summary>
    /// Retrieves a collection of users filtered by their active status.
    /// </summary>
    /// <param name="isActive">A boolean indicating whether to filter for active users (true) or inactive users (false).</param>
    /// <returns>A collection of users that match the specified active status.</returns>
    Task<IEnumerable<User>> FilterByActive(bool isActive);

    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user to retrieve.</param>
    /// <returns>The user object associated with the specified identifier if found, null otherwise.</returns>
    Task<User?> GetById(long id);

    /// <summary>
    /// Retrieves all users.
    /// </summary>
    /// <returns>A collection of all users.</returns>
    Task<IEnumerable<User>> GetAll();
}
