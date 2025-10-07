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
    IMapper mapper,
    ILoggingService loggingService)
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

        await loggingService.LogAction(
            action: "Create",
            entityType: "User",
            entityId: user.Id,
            details: $"Created user: {viewModel.FullName}"
        );

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

        await loggingService.LogAction(
            action: "Delete",
            entityType: "User",
            entityId: user.Id,
            details: string.Empty
        );

        return (true, []);
    }

    public async Task<IEnumerable<UserListItemViewModel>> Get(UserFilter? filter = null)
    {
        var users = await GetUsersWithFilter(filter ?? new UserFilter());
        return users.Select(mapper.Map<UserListItemViewModel>);
    }

    private async Task<IEnumerable<User>> GetUsersWithFilter(UserFilter filter)
    {
        var query = await dataAccess.GetAll<User>();

        if (filter.ActiveStatus.HasValue)
        {
            query = query.Where(u => u.IsActive == filter.ActiveStatus.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<(bool Found, UserViewModel? User)> GetById(long id)
    {
        var user = await dataAccess.GetById<User>(id);
        if (user == null)
        {
            return (Found: false, User: null);
        }

        var userViewModel = mapper.Map<UserViewModel>(user);
        userViewModel.Logs = await loggingService.GetLogsByEntity("User", id);

        return (Found: true, User: userViewModel);
    }

    public async Task<(bool IsSuccess, IEnumerable<string> Errors)> Update(UserInputViewModel viewModel)
    {
        var validationResult = await userInputViewModelValidator.ValidateAsync(viewModel);
        if (!validationResult.IsValid)
        {
            return (false, validationResult.Errors.Select(x => x.ErrorMessage));
        }

        var userEntity = await dataAccess.GetById<User>(viewModel.Id);
        if (userEntity == null)
        {
            return (false, ["User not found"]);
        }

        var changes = new List<string>();

        if (userEntity.Forename != viewModel.Forename)
            changes.Add($"Forename changed from '{userEntity.Forename}' to '{viewModel.Forename}'");

        if (userEntity.Surname != viewModel.Surname)
            changes.Add($"Surname changed from '{userEntity.Surname}' to '{viewModel.Surname}'");

        if (userEntity.Email != viewModel.Email)
            changes.Add($"Email changed from '{userEntity.Email}' to '{viewModel.Email}'");

        if (userEntity.DateOfBirth != viewModel.DateOfBirth)
        {
            var originalDateString = userEntity.DateOfBirth.ToString("dd/MM/yyyy");
            var newDateString = viewModel.DateOfBirth?.ToString("dd/MM/yyyy");
            changes.Add($"Date of Birth changed from '{originalDateString}' to '{newDateString}'");
        }

        if (userEntity.IsActive != viewModel.IsActive)
            changes.Add($"Active status changed from '{userEntity.IsActive}' to '{viewModel.IsActive}'");

        mapper.Map(viewModel, userEntity);
        await dataAccess.Update(userEntity);

        var detailsMessage = changes.Count != 0
            ? $"Updated user: {string.Join(", ", changes)}"
            : "User updated with no property changes";

        await loggingService.LogAction(
            action: "Update",
            entityType: "User",
            entityId: userEntity.Id,
            details: detailsMessage
        );

        return (true, []);
    }
}
