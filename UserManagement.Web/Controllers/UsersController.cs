using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using UserManagement.Services.Interfaces;
using UserManagement.Shared.Filters;
using UserManagement.Shared.Models.Users;

namespace UserManagement.Web.Controllers;

[Route("users")]
public class UsersController(
    IUserService userService,
    IMapper mapper)
    : Controller
{
    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new UserInputViewModel());
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(UserInputViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var (isSuccess, errors) = await userService.Create(viewModel);

        if (isSuccess)
        {
            TempData["SuccessMessage"] = $"User {viewModel.Forename} {viewModel.Surname} was created successfully";
            return RedirectToAction(nameof(List));
        }

        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return View(viewModel);
    }

    [HttpGet("delete")]
    public async Task<IActionResult> Delete(int userId)
    {
        var (found, userViewModel) = await userService.GetById(userId);

        if (found)
        {
            return View(userViewModel);
        }

        TempData["ErrorMessage"] = $"Unable to find user with ID {userId}";
        return RedirectToAction(nameof(List));
    }

    [HttpPost("delete")]
    public async Task<IActionResult> DeleteConfirmed(int userId)
    {
        var (isSuccess, errors) = await userService.DeleteById(userId);

        if (isSuccess)
        {
            TempData["SuccessMessage"] = "User deleted successfully";
        }
        else
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            TempData["ErrorMessage"] = $"Failed to delete user with ID: {userId}";
        }

        return RedirectToAction(nameof(List));
    }

    [HttpGet]
    public async Task<ViewResult> List(string filter = "all")
    {
        var userFilter = UserFilter.FromString(filter);
        var users = await userService.Get(userFilter);

        var model = new UserListViewModel
        {
            Items = users.ToList()
        };

        return View(model);
    }

    [HttpGet("update")]
    public async Task<IActionResult> Update(int userId)
    {
        var (found, userViewModel) = await userService.GetById(userId);

        if (found)
        {
            var userInputViewModel = mapper.Map<UserInputViewModel>(userViewModel);
            return View(userInputViewModel);
        }

        TempData["ErrorMessage"] = $"Unable to find user with ID {userId}";
        return RedirectToAction(nameof(List));
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update(UserInputViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var (isSuccess, errors) = await userService.Update(viewModel);

        if (isSuccess)
        {
            TempData["SuccessMessage"] = $"User {viewModel.Forename} {viewModel.Surname} was updated successfully";
            return RedirectToAction(nameof(List));
        }

        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return View(viewModel);
    }

    [HttpGet("view")]
    public async Task<IActionResult> View(int userId)
    {
        var (found, userViewModel) = await userService.GetById(userId);

        if (found)
        {
            return View(userViewModel);
        }

        TempData["ErrorMessage"] = $"Unable to find user with ID {userId}";
        return RedirectToAction(nameof(List));
    }
}
