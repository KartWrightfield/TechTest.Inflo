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
    public async Task<IActionResult> Add(UserInputViewModel userInputViewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(userInputViewModel);
        }

        if (userInputViewModel.Forename == null ||
            userInputViewModel.Surname == null ||
            userInputViewModel.Email == null ||
            userInputViewModel.DateOfBirth == null)
        {
            ModelState.AddModelError(string.Empty, "All required fields must be provided");
            return View(userInputViewModel);
        }

        try
        {
            var user = new User
            {
                Forename = userInputViewModel.Forename,
                Surname = userInputViewModel.Surname,
                Email = userInputViewModel.Email,
                DateOfBirth = (DateOnly)userInputViewModel.DateOfBirth,
                IsActive = userInputViewModel.IsActive,
            };

            await userService.Create(user);

            TempData["SuccessMessage"] = $"User {user.Forename} {user.Surname} was created successfully";

            return RedirectToAction(nameof(List));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Failed to create user: {ex.Message}");
            return View(userInputViewModel);
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

    [HttpGet("update")]
    public async Task<IActionResult> Update(int userId)
    {
        var user = await userService.GetById(userId);

        if (user == null)
        {
            TempData["ErrorMessage"] = $"Unable to find user with ID {userId}";
            return RedirectToAction(nameof(List));
        }

        var userInputViewModel = new UserInputViewModel
        {
            Id = user.Id,
            Forename = user.Forename,
            Surname = user.Surname,
            Email = user.Email,
            DateOfBirth = user.DateOfBirth,
            IsActive = user.IsActive
        };

        return View(userInputViewModel);
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update(UserInputViewModel userInputViewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(userInputViewModel);
        }

        if (userInputViewModel.Forename == null ||
            userInputViewModel.Surname == null ||
            userInputViewModel.Email == null ||
            userInputViewModel.DateOfBirth == null)
        {
            ModelState.AddModelError(string.Empty, "All required fields must be provided");
            return View(userInputViewModel);
        }

        try
        {
            var user = new User
            {
                Id = userInputViewModel.Id,
                Forename = userInputViewModel.Forename,
                Surname = userInputViewModel.Surname,
                Email = userInputViewModel.Email,
                DateOfBirth = (DateOnly)userInputViewModel.DateOfBirth,
                IsActive = userInputViewModel.IsActive,
            };

            await userService.Update(user);

            TempData["SuccessMessage"] = $"User {user.Forename} {user.Surname} was updated successfully";

            return RedirectToAction(nameof(List));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Failed to update user: {ex.Message}");
            return View(userInputViewModel);
        }
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
