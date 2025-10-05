using System;
using System.Linq;
using System.Threading.Tasks;
using UserManagement.Data.Entities;
using UserManagement.Services.Interfaces;
using UserManagement.Web.Models.Users;

namespace UserManagement.Web.Controllers;

[Route("users")]
public class UsersController(IUserService userService) : Controller
{
    [HttpGet("add")]
    public IActionResult Add()
    {
        return View(new User());
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add(User user)
    {
        if (!ModelState.IsValid)
        {
            return View(user);
        }

        try
        {
            await userService.Create(user);

            TempData["SuccessMessage"] = $"User {user.Forename} {user.Surname} was created successfully";

            return RedirectToAction(nameof(List));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Failed to create user: {ex.Message}");
            return View(user);
        }
    }

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
