using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using UserManagement.Services.Interfaces;
using UserManagement.Shared.Filters;
using UserManagement.Shared.Models.Users;
using UserManagement.Web.Controllers;

namespace UserManagement.Web.Tests;

public class UserControllerTests
{
    [Fact]
    public void Create_Get_ReturnsViewWithEmptyUser()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.Create();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeOfType<UserInputViewModel>()
            .Which.Should().NotBeNull();

        // Verify it's a new User with default values
        viewResult.Model.As<UserInputViewModel>().Id.Should().Be(0);
        viewResult.Model.As<UserInputViewModel>().Forename.Should().BeNull();
        viewResult.Model.As<UserInputViewModel>().Surname.Should().BeNull();
        viewResult.Model.As<UserInputViewModel>().Email.Should().BeNull();
    }

    [Fact]
    public async Task Create_Post_WhenModelStateIsInvalid_ReturnsViewWithModel()
    {
        // Arrange
        var controller = CreateController();
        var viewModel = new UserInputViewModel { Forename = "Test", Surname = "User" };
        controller.ModelState.AddModelError("Email", "Email is required");

        // Act
        var result = await controller.Create(viewModel);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().Be(viewModel);
        _userService.Verify(s => s.Create(It.IsAny<UserInputViewModel>()), Times.Never);
    }

    [Fact]
    public async Task Create_Post_WhenServiceSucceeds_RedirectsToList()
    {
        // Arrange
        var controller = CreateController();
        var viewModel = new UserInputViewModel
        {
            Forename = "George",
            Surname = "Romero",
            Email = "george.a.romero@example.com",
            DateOfBirth = new DateOnly(1940, 1, 1),
            IsActive = true
        };

        _userService.Setup(s => s.Create(viewModel))
            .ReturnsAsync((true, new List<string>()));

        // Act
        var result = await controller.Create(viewModel);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(UsersController.List));

        _userService.Verify(s => s.Create(viewModel), Times.Once);

        controller.TempData.Should().ContainKey("SuccessMessage")
            .WhoseValue.Should().Be($"User {viewModel.Forename} {viewModel.Surname} was created successfully");
    }

    [Fact]
    public async Task Create_Post_WhenServiceFails_ReturnsViewWithErrors()
    {
        // Arrange
        var controller = CreateController();
        var viewModel = new UserInputViewModel
        {
            Forename = "George",
            Surname = "Romero",
            Email = "george.a.romero@example.com",
            DateOfBirth = new DateOnly(1940, 1, 1)
        };
        var errors = new List<string> { "Database connection failed" };

        _userService.Setup(s => s.Create(viewModel))
            .ReturnsAsync((false, errors));

        // Act
        var result = await controller.Create(viewModel);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().Be(viewModel);

        controller.ModelState.IsValid.Should().BeFalse();
        controller.ModelState.Keys.Should().Contain(string.Empty);
        controller.ModelState[string.Empty]
            ?.Errors.Should().ContainSingle(e => e.ErrorMessage == errors[0]);
    }

    [Fact]
    public async Task Delete_Get_WhenUserExists_ShouldReturnViewWithPopulatedViewModel()
    {
        // Arrange
        var controller = CreateController();
        var userViewModel = new UserViewModel
        {
            Id = 1,
            Forename = "Test",
            Surname = "User1",
            Email = "active1@example.com",
            DateOfBirth = new DateOnly(1954, 6, 1),
            IsActive = true
        };

        _userService.Setup(s => s.GetById(1))
            .ReturnsAsync((true, userViewModel));

        // Act
        var result = await controller.Delete(1);

        // Assert
        result.Should().BeOfType<ViewResult>()
            .Which.ViewData.Model.Should().BeOfType<UserViewModel>()
            .Which.Should().BeEquivalentTo(userViewModel);
    }

    [Fact]
    public async Task Delete_Get_WhenUserDoesNotExist_ReturnsToListViewWithErrorMessage()
    {
        // Arrange
        var controller = CreateController();

        _userService.Setup(s => s.GetById(1))
            .ReturnsAsync((false, null));

        // Act
        var result = await controller.Delete(1);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(UsersController.List));

        _userService.Verify(s => s.GetById(1), Times.Once);

        controller.TempData.Should().ContainKey("ErrorMessage")
            .WhoseValue.Should().Be("Unable to find user with ID 1");
    }

    [Fact]
    public async Task DeleteConfirmed_WhenServiceSucceeds_DeletesUserAndRedirectsToList()
    {
        // Arrange
        var controller = CreateController();

        _userService.Setup(s => s.DeleteById(1))
            .ReturnsAsync((true, new List<string>()));

        // Act
        var result = await controller.DeleteConfirmed(1);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(UsersController.List));

        _userService.Verify(s => s.DeleteById(1), Times.Once);

        controller.TempData.Should().ContainKey("SuccessMessage")
            .WhoseValue.Should().Be("User deleted successfully");
    }

    [Fact]
    public async Task DeleteConfirmed_WhenServiceFails_RedirectsWithErrorMessages()
    {
        // Arrange
        var controller = CreateController();
        var errors = new List<string> { "Database connection failed" };

        _userService.Setup(s => s.DeleteById(1))
            .ReturnsAsync((false, errors));

        // Act
        var result = await controller.DeleteConfirmed(1);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(UsersController.List));

        controller.ModelState.IsValid.Should().BeFalse();
        controller.ModelState.Keys.Should().Contain(string.Empty);
        controller.ModelState[string.Empty]
            ?.Errors.Should().ContainSingle(e => e.ErrorMessage == errors[0]);

        controller.TempData.Should().ContainKey("ErrorMessage")
            .WhoseValue.Should().Be("Failed to delete user with ID: 1");
    }

    [Fact]
    public async Task List_ReturnsAllUsersFromService()
    {
        // Arrange
        var controller = CreateController();
        var userViewModels = new List<UserListItemViewModel>
        {
            new() { Id = 1, Forename = "Johnny", Surname = "User", Email = "juser@example.com", DateOfBirth = null, IsActive = true }
        };

        _userService.Setup(s => s.Get(It.IsAny<UserFilter>()))
            .ReturnsAsync(userViewModels);

        // Act
        var result = await controller.List();

        // Assert
        result.Model.Should().BeOfType<UserListViewModel>()
            .Which.Items.Should().BeEquivalentTo(userViewModels);

        _userService.Verify(s => s.Get(It.IsAny<UserFilter>()), Times.Once);
    }

    [Fact]
    public async Task Update_Get_WhenUserExists_ShouldReturnViewWithPopulatedViewModel()
    {
        // Arrange
        var controller = CreateController();
        var userViewModel = new UserViewModel
        {
            Id = 1,
            Forename = "Test",
            Surname = "User1",
            Email = "active1@example.com",
            DateOfBirth = new DateOnly(1954, 6, 1),
            IsActive = true
        };
        var userInputViewModel = new UserInputViewModel
        {
            Id = 1,
            Forename = "Test",
            Surname = "User1",
            Email = "active1@example.com",
            DateOfBirth = new DateOnly(1954, 6, 1),
            IsActive = true
        };

        _userService.Setup(s => s.GetById(1))
            .ReturnsAsync((true, userViewModel));

        _mapper.Setup(m => m.Map<UserInputViewModel>(userViewModel))
            .Returns(userInputViewModel);

        // Act
        var result = await controller.Update(1);

        // Assert
        result.Should().BeOfType<ViewResult>()
            .Which.ViewData.Model.Should().BeOfType<UserInputViewModel>()
            .Which.Should().BeEquivalentTo(userInputViewModel);

        _mapper.Verify(m => m.Map<UserInputViewModel>(userViewModel), Times.Once);
    }

    [Fact]
    public async Task Update_Get_WhenUserDoesNotExist_ReturnsToListViewWithErrorMessage()
    {
        // Arrange
        var controller = CreateController();

        _userService.Setup(s => s.GetById(1))
            .ReturnsAsync((false, null));

        // Act
        var result = await controller.Update(1);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(UsersController.List));

        _userService.Verify(s => s.GetById(1), Times.Once);

        controller.TempData.Should().ContainKey("ErrorMessage")
            .WhoseValue.Should().Be("Unable to find user with ID 1");
    }

    [Fact]
    public async Task Update_Post_WhenModelStateIsInvalid_ReturnsViewWithModel()
    {
        // Arrange
        var controller = CreateController();
        var viewModel = new UserInputViewModel { Forename = "Test", Surname = "User" };
        controller.ModelState.AddModelError("Email", "Email is required");

        // Act
        var result = await controller.Update(viewModel);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().Be(viewModel);
        _userService.Verify(s => s.Update(It.IsAny<UserInputViewModel>()), Times.Never);
    }

    [Fact]
    public async Task Update_Post_WhenServiceSucceeds_RedirectsToList()
    {
        // Arrange
        var controller = CreateController();
        var viewModel = new UserInputViewModel
        {
            Id = 1,
            Forename = "George",
            Surname = "Romero",
            Email = "george.a.romero@example.com",
            DateOfBirth = new DateOnly(1940, 1, 1),
            IsActive = true
        };

        _userService.Setup(s => s.Update(viewModel))
            .ReturnsAsync((true, new List<string>()));

        // Act
        var result = await controller.Update(viewModel);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(UsersController.List));

        _userService.Verify(s => s.Update(viewModel), Times.Once);

        controller.TempData.Should().ContainKey("SuccessMessage")
            .WhoseValue.Should().Be($"User {viewModel.Forename} {viewModel.Surname} was updated successfully");
    }

    [Fact]
    public async Task Update_Post_WhenServiceFails_ReturnsViewWithErrors()
    {
        // Arrange
        var controller = CreateController();
        var viewModel = new UserInputViewModel
        {
            Id = 1,
            Forename = "George",
            Surname = "Romero",
            Email = "george.a.romero@example.com",
            DateOfBirth = new DateOnly(1940, 1, 1)
        };
        var errors = new List<string> { "Database connection failed" };

        _userService.Setup(s => s.Update(viewModel))
            .ReturnsAsync((false, errors));

        // Act
        var result = await controller.Update(viewModel);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().Be(viewModel);

        controller.ModelState.IsValid.Should().BeFalse();
        controller.ModelState.Keys.Should().Contain(string.Empty);
        controller.ModelState[string.Empty]
            ?.Errors.Should().ContainSingle(e => e.ErrorMessage == errors[0]);
    }

    [Fact]
    public async Task View_WhenUserExists_ReturnsViewWithUserViewModel()
    {
        // Arrange
        var controller = CreateController();
        var userViewModel = new UserViewModel
        {
            Id = 1,
            Forename = "Test",
            Surname = "User1",
            Email = "active1@example.com",
            DateOfBirth = new DateOnly(1954, 6, 1),
            IsActive = true
        };

        _userService.Setup(s => s.GetById(1))
            .ReturnsAsync((true, userViewModel));

        // Act
        var result = await controller.View(1);

        // Assert
        result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeOfType<UserViewModel>()
            .Which.Should().BeEquivalentTo(userViewModel);
    }

    [Fact]
    public async Task View_WhenUserDoesNotExist_ReturnsToListViewWithErrorMessage()
    {
        // Arrange
        var controller = CreateController();

        _userService.Setup(s => s.GetById(1))
            .ReturnsAsync((false, null));

        // Act
        var result = await controller.View(1);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(UsersController.List));

        _userService.Verify(s => s.GetById(1), Times.Once);

        controller.TempData.Should().ContainKey("ErrorMessage")
            .WhoseValue.Should().Be("Unable to find user with ID 1");
    }

    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IMapper> _mapper = new();

    private UsersController CreateController()
    {
        var controller = new UsersController(_userService.Object, _mapper.Object);
        controller.TempData = new TempDataDictionary(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());
        return controller;
    }
}
