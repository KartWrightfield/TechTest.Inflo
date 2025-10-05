using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Data.Entities;
using UserManagement.Services.Interfaces;
using UserManagement.Web.Controllers;
using UserManagement.Web.Models.Users;

namespace UserManagement.Web.Tests;

public class UserControllerTests
{
    [Fact]
    public void Add_Get_ReturnsViewWithEmptyUser()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.Add();

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
    public async Task Add_Post_WhenModelStateIsInvalid_ReturnsViewWithSameUser()
    {
        // Arrange
        var controller = CreateController();
        var user = new UserInputViewModel { Forename = "Test", Surname = "User" };
        controller.ModelState.AddModelError("Email", "Email is required");

        // Act
        var result = await controller.Add(user);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().Be(user);
        _userService.Verify(s => s.Create(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Add_Post_WhenModelStateIsValid_CreatesUserAndRedirectsToList()
    {
        // Arrange
        var controller = CreateController();
        var user = new UserInputViewModel
        {
            Forename = "George",
            Surname = "Romero",
            Email = "george.a.romero@example.com",
            DateOfBirth = new DateOnly(1940, 1, 1),
            IsActive = true
        };

        // Act
        var result = await controller.Add(user);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(UsersController.List));

        _userService.Verify(s => s.Create(It.Is<User>(u => u.Email == user.Email)), Times.Once);

        controller.TempData.Should().ContainKey("SuccessMessage")
            .WhoseValue.Should().Be($"User {user.Forename} {user.Surname} was created successfully");
    }

    [Theory]
    [MemberData(nameof(GetNullPropertyTestCases))]
    public async Task Add_Post_WhenRequiredPropertyOfModelIsNull_ReturnsViewWithErrorMessage(
        string testCase, UserInputViewModel model)
    {
        // Arrange
        var controller = CreateController();
        const string exceptionMessage = "All required fields must be provided";

        // Act
        var result = await controller.Add(model);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeOfType<UserInputViewModel>()
            .Which.Should().BeEquivalentTo(model);
        controller.ModelState.IsValid.Should().BeFalse();
        controller.ModelState.Keys.Should().Contain(string.Empty);
        controller.ModelState[string.Empty]
            ?.Errors.Should().ContainSingle(
                e => e.ErrorMessage.Contains(exceptionMessage));
    }

    private static IEnumerable<object[]> GetNullPropertyTestCases()
    {
        var defaultDate = new DateOnly(1960, 1, 1);

        yield return
        [
            "Null Forename",
            new UserInputViewModel
            {
                Forename = null,
                Surname = "User",
                Email = "test@email.com",
                DateOfBirth = defaultDate,
                IsActive = true
            }
        ];

        yield return
        [
            "Null Surname",
            new UserInputViewModel
            {
                Forename = "Test",
                Surname = null,
                Email = "test@email.com",
                DateOfBirth = defaultDate,
                IsActive = true
            }
        ];

        yield return
        [
            "Null Email",
            new UserInputViewModel
            {
                Forename = "Test",
                Surname = "User",
                Email = null,
                DateOfBirth = defaultDate,
                IsActive = true
            }
        ];

        yield return
        [
            "Null DateOfBirth",
            new UserInputViewModel
            {
                Forename = "Test",
                Surname = "User",
                Email = "test@email.com",
                DateOfBirth = null,
                IsActive = true
            }
        ];
    }

    [Fact]
    public async Task Add_Post_WhenServiceThrowsException_ReturnsViewWithErrorMessage()
    {
        // Arrange
        var controller = CreateController();
        var user = new User
        {
            Forename = "George",
            Surname = "Romero",
            Email = "active1@example.com",
            DateOfBirth = new DateOnly(1940, 1, 1)
        };
        const string exceptionMessage = "Database connection failed";

        _userService.Setup(s => s.Create(It.IsAny<User>())).ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await controller.Add(new UserInputViewModel { Forename = "George", Surname = "Romero", Email = "active1@example.com", DateOfBirth = new DateOnly(1940, 1, 1) });

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeEquivalentTo(user);

        controller.ModelState.IsValid.Should().BeFalse();
        controller.ModelState.Keys.Should().Contain(string.Empty);
        controller.ModelState[string.Empty]
            ?.Errors.Should().ContainSingle(
            e => e.ErrorMessage.Contains(exceptionMessage));
    }

    [Fact]
    public async Task List_WhenFilterIsActive_ModelMustContainOnlyActiveUsers()
    {
        // Arrange
        var controller = CreateController();
        var activeUsers = new[]
        {
            new User { Forename = "Active", Surname = "User1", Email = "active1@example.com", DateOfBirth = new DateOnly(1954, 6, 1), IsActive = true },
            new User { Forename = "Active", Surname = "User2", Email = "active2@example.com", DateOfBirth = new DateOnly(1972, 12, 28), IsActive = true }
        };

        _userService.Setup(s => s.FilterByActive(true)).ReturnsAsync(activeUsers);

        // Act
        var result = await controller.List("active");

        // Assert
        result.Model.Should().BeOfType<UserListViewModel>()
            .Which.Items.Should().HaveCount(2)
            .And.AllSatisfy(item => item.IsActive.Should().BeTrue());

        _userService.Verify(s => s.FilterByActive(true), Times.Once);
        _userService.Verify(s => s.GetAll(), Times.Never);
    }

    [Fact]
    public async Task List_WhenFilterIsInactive_ModelMustContainOnlyInactiveUsers()
    {
        // Arrange
        var controller = CreateController();
        var inactiveUsers = new[]
        {
            new User { Forename = "Inactive", Surname = "User1", Email = "inactive1@example.com", DateOfBirth = new DateOnly(1972, 12, 28), IsActive = false },
            new User { Forename = "Inactive", Surname = "User2", Email = "inactive2@example.com", DateOfBirth = new DateOnly(1972, 12, 28), IsActive = false }
        };

        _userService.Setup(s => s.FilterByActive(false)).ReturnsAsync(inactiveUsers);

        // Act
        var result = await controller.List("inactive");

        // Assert
        result.Model.Should().BeOfType<UserListViewModel>()
            .Which.Items.Should().HaveCount(2)
            .And.AllSatisfy(item => item.IsActive.Should().BeFalse());

        _userService.Verify(s => s.FilterByActive(false), Times.Once);
        _userService.Verify(s => s.GetAll(), Times.Never);
    }

    [Fact]
    public async Task List_WhenFilterIsEmpty_MustCallGetAllAndReturnAllUsers()
    {
        // Arrange
        var controller = CreateController();
        var allUsers = SetupUsers();

        // Act
        var result = await controller.List();

        // Assert
        result.Model.Should().BeOfType<UserListViewModel>()
            .Which.Items.Should().BeEquivalentTo(allUsers);

        _userService.Verify(s => s.GetAll(), Times.Once);
        _userService.Verify(s => s.FilterByActive(It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task List_WhenFilterIsUnrecognised_MustDefaultToShowingAllUsers()
    {
        // Arrange
        var controller = CreateController();
        var allUsers = SetupUsers();

        // Act
        var result = await controller.List("unrecognised-filter");

        // Assert
        result.Model.Should().BeOfType<UserListViewModel>()
            .Which.Items.Should().BeEquivalentTo(allUsers);

        _userService.Verify(s => s.GetAll(), Times.Once);
        _userService.Verify(s => s.FilterByActive(It.IsAny<bool>()), Times.Never);
    }

    [Theory]
    [InlineData("ACTIVE")]
    [InlineData("Active")]
    [InlineData("AcTiVe")]
    public async Task List_WhenFilterIsCaseVariationsOfActive_MustHandleCaseInsensitivity(string filter)
    {
        // Arrange
        var controller = CreateController();
        var activeUsers = new[]
        {
            new User { Forename = "Active", Surname = "User", Email = "active@example.com", DateOfBirth = new DateOnly(1972, 12, 28), IsActive = true }
        };

        _userService.Setup(s => s.FilterByActive(true)).ReturnsAsync(activeUsers);

        // Act
        var result = await controller.List(filter);

        // Assert
        result.Model.Should().BeOfType<UserListViewModel>()
            .Which.Items.Should().HaveCount(1)
            .And.AllSatisfy(item => item.IsActive.Should().BeTrue());

        _userService.Verify(s => s.FilterByActive(true), Times.Once);
    }

    [Fact]
    public async Task List_WhenServiceReturnsUsers_ModelMustContainUsers()
    {
        // Arrange: Initialises objects and sets the value of the data that is passed to the method under test.
        var controller = CreateController();
        var users = SetupUsers();

        // Act: Invokes the method under test with the arranged parameters.
        var result = await controller.List();

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Model
            .Should().BeOfType<UserListViewModel>()
            .Which.Items.Should().BeEquivalentTo(users);
    }

    [Fact]
    public async Task View_WhenServiceReturnsNull_ReturnsToListViewWithErrorMessage()
    {
        // Arrange
        var controller = CreateController();

        _userService.Setup(s => s.GetById(1)).ReturnsAsync((User?)null);

        //Act
        var result = await  controller.View(1);

        //Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(UsersController.List));

        _userService.Verify(s => s.GetById(1), Times.Once);

        controller.TempData.Should().ContainKey("ErrorMessage")
            .WhoseValue.Should().Be("Unable to find user with ID 1");
    }

    [Fact]
    public async Task View_WhenServiceReturnsUser_ReturnsCorrespondingUserViewModel()
    {
        //Arrange
        var controller = CreateController();
        var users = new[]
        {
            new User { Id = 1, Forename = "Test", Surname = "User1", Email = "active1@example.com", DateOfBirth = new DateOnly(1954, 6, 1), IsActive = true },
            new User { Id = 2, Forename = "Test", Surname = "User2", Email = "active2@example.com", DateOfBirth = new DateOnly(1972, 12, 28), IsActive = true }
        };

        _userService.Setup(s => s.GetById(1)).ReturnsAsync(users[0]);

        //Act
        var result = await controller.View(1);

        //Assert
        result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().BeOfType<UserViewModel>()
            .Which.Should().BeEquivalentTo(users[0]);
    }

    private User[] SetupUsers(string forename = "Johnny", string surname = "User", string email = "juser@example.com", DateOnly dateOfBirth = default, bool isActive = true)
    {
        var users = new[]
        {
            new User
            {
                Forename = forename,
                Surname = surname,
                Email = email,
                DateOfBirth = dateOfBirth,
                IsActive = isActive
            }
        };

        _userService
            .Setup(s => s.GetAll())
            .ReturnsAsync(users);

        return users;
    }

    private readonly Mock<IUserService> _userService = new();

    private UsersController CreateController()
    {
        var controller = new UsersController(_userService.Object);
        controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
        return controller;
    }
}
