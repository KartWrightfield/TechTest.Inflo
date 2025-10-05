using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Data.Entities;
using UserManagement.Services.Interfaces;

namespace UserManagement.Services.Implementations;

public class UserService(IDataContext dataAccess) : IUserService
{
    public async Task Create(User user) => await dataAccess.Create(user);

    public async Task DeleteById(long id)
    {
        var user = await dataAccess.GetById<User>(id);
        if (user == null)
        {
            throw new Exception($"User with ID {id} not found");
        }
        await dataAccess.Delete(user);
    }

    public async Task<IEnumerable<User>> FilterByActive(bool isActive)
    {
        return await dataAccess.GetAll<User>()
            .Where(u => u.IsActive == isActive)
            .ToListAsync();
    }

    public async Task<User?> GetById(long id) => await dataAccess.GetById<User>(id);

    public async Task<IEnumerable<User>> GetAll() =>
        await dataAccess.GetAll<User>().ToListAsync();

    public async Task Update(User user) => await dataAccess.Update(user);
}
