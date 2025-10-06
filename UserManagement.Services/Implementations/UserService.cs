using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Data.Entities;
using UserManagement.Services.Interfaces;
using UserManagement.Shared.Filters;
using UserManagement.Shared.Models.Users;

namespace UserManagement.Services.Implementations;

public class UserService(
    IDataContext dataAccess,
    IValidator<UserInputViewModel> userInputViewModelValidator,
    IMapper mapper)
    : IUserService
{
    public async Task<(bool IsSuccess, IEnumerable<string> Errors)> Create(UserInputViewModel viewModel)
    {
        var validationResult = await userInputViewModelValidator.ValidateAsync(viewModel);
        if (!validationResult.IsValid)
        {
            return (false, validationResult.Errors.Select(x => x.ErrorMessage));
        }

        var user = mapper.Map<User>(viewModel);
        await dataAccess.Create(user);

        return (true, []);
    }

    public async Task<(bool IsSuccess, IEnumerable<string> Errors)> DeleteById(long id)
    {
        var user = await dataAccess.GetById<User>(id);
        if (user == null)
        {
            return (false, ["User not found"]);
        }

        await dataAccess.Delete(user);
        return (true, []);
    }

    public async Task<IEnumerable<UserListItemViewModel>> Get(UserFilter? filter = null)
    {
        var users = await GetUsersWithFilter(filter ?? new UserFilter());
        return users.Select(mapper.Map<UserListItemViewModel>);
    }

    private async Task<IEnumerable<User>> GetUsersWithFilter(UserFilter filter)
    {
        var query = dataAccess.GetAll<User>();

        if (filter.ActiveStatus.HasValue)
        {
            query = query.Where(u => u.IsActive == filter.ActiveStatus.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<(bool Found, UserViewModel? User)> GetById(long id)
    {
        var user = await dataAccess.GetById<User>(id);
        return user == null
            ? (Found: false, User: null)
            : (Found: true, User: mapper.Map<UserViewModel>(user));
    }

    public async Task<(bool IsSuccess, IEnumerable<string> Errors)> Update(UserInputViewModel viewModel)
    {
        var validationResult = await userInputViewModelValidator.ValidateAsync(viewModel);
        if (!validationResult.IsValid)
        {
            return (false, validationResult.Errors.Select(x => x.ErrorMessage));
        }

        var user = mapper.Map<User>(viewModel);
        await dataAccess.Update(user);

        return (true, []);
    }
}
