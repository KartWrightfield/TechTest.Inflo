using System;
using System.Threading.Tasks;
using UserManagement.Models;
using UserManagement.Services.Interfaces;
using UserManagement.Web.Controllers;
using UserManagement.Web.Models.Users;

namespace UserManagement.Web.Tests;

public class UserControllerTests
{
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
    private UsersController CreateController() => new(_userService.Object);
}
