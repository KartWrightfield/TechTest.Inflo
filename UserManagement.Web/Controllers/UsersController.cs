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
        return View(new UserInputViewModel());
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add(UserInputViewModel userViewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(userViewModel);
        }

        if (userViewModel.Forename == null ||
            userViewModel.Surname == null ||
            userViewModel.Email == null ||
            userViewModel.DateOfBirth == null)
        {
            ModelState.AddModelError(string.Empty, "All required fields must be provided");
            return View(userViewModel);
        }

        try
        {
            var user = new User
            {
                Forename = userViewModel.Forename,
                Surname = userViewModel.Surname,
                Email = userViewModel.Email,
                DateOfBirth = (DateOnly)userViewModel.DateOfBirth,
                IsActive = userViewModel.IsActive,
            };

            await userService.Create(user);

            TempData["SuccessMessage"] = $"User {user.Forename} {user.Surname} was created successfully";

            return RedirectToAction(nameof(List));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Failed to create user: {ex.Message}");
            return View(userViewModel);
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

    [HttpGet("view")]
    public async Task<IActionResult> View(int userId)
    {
        var user = await userService.GetById(userId);

        if (user == null)
        {
            TempData["ErrorMessage"] = $"Unable to find user with ID {userId}";
            return RedirectToAction(nameof(List));
        }

        var userViewModel = new UserViewModel
        {
            Id = user.Id,
            Forename = user.Forename,
            Surname = user.Surname,
            Email = user.Email,
            DateOfBirth = user.DateOfBirth,
            IsActive = user.IsActive
        };

        return View(userViewModel);
    }
}
