using System.Linq;
using System.Threading.Tasks;
using UserManagement.Models;
using UserManagement.Services.Interfaces;
using UserManagement.Web.Models.Users;

namespace UserManagement.Web.Controllers;

[Route("users")]
public class UsersController(IUserService userService) : Controller
{
    [HttpGet]
    public async Task<ViewResult> List(string filter = "all")
    {
        IEnumerable<User> users = filter.ToLower() switch
        {
            "active" => await userService.FilterByActive(true),
            "inactive" => await userService.FilterByActive(false),
            _ => await userService.GetAll()
        };

        var items = users.Select(p => new UserListItemViewModel
        {
            Id = p.Id,
            Forename = p.Forename,
            Surname = p.Surname,
            Email = p.Email,
            DateOfBirth = p.DateOfBirth,
            IsActive = p.IsActive
        });

        var model = new UserListViewModel
        {
            Items = items.ToList()
        };

        return View(model);
    }
}
