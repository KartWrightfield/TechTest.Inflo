using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Services.Interfaces;

namespace UserManagement.Services.Implementations;

public class UserService(IDataContext dataAccess) : IUserService
{
    public async Task<IEnumerable<User>> FilterByActive(bool isActive)
    {
        return await dataAccess.GetAll<User>()
            .Where(u => u.IsActive == isActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetAll() =>
        await dataAccess.GetAll<User>().ToListAsync();
}
