using System.Collections.Generic;
using System.Threading.Tasks;
using UserManagement.Data.Entities;
using UserManagement.Shared.Filters;
using UserManagement.Shared.Models.Users;

namespace UserManagement.Services.Interfaces;

public interface IUserService
{
    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="viewModel">The user object to be created.</param>
    /// <returns></returns>
    Task<(bool IsSuccess, IEnumerable<string> Errors)> Create(UserInputViewModel viewModel);

    /// <summary>
    /// Deletes a user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete.</param>
    /// <returns></returns>
    Task<(bool IsSuccess, IEnumerable<string> Errors)> DeleteById(long id);

    /// <summary>
    /// Retrieves a list of users based on the specified filter.
    /// </summary>
    /// <param name="filter">An optional filter to refine the list of users. If not provided, defaults to no filtering.</param>
    /// <returns>A collection of user list item view models that match the filter criteria.</returns>
    Task<IEnumerable<UserListItemViewModel>> Get(UserFilter? filter = null);

    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user to retrieve.</param>
    /// <returns>The user object associated with the specified identifier if found, null otherwise.</returns>
    Task<(bool Found, UserViewModel? User)> GetById(long id);

    /// <summary>
    /// Updates the details of an existing user.
    /// </summary>
    /// <param name="viewModel">The view model object containing updated information.</param>
    /// <returns></returns>
    Task<(bool IsSuccess, IEnumerable<string> Errors)> Update(UserInputViewModel viewModel);
}
