using System;
using System.Linq;
using UserManagement.Data;
using UserManagement.Models;
using UserManagement.Services.Domain.Implementations;

namespace UserManagement.Services.Tests;

public class UserServiceTests
{
    [Fact]
    public void FilterByActive_WhenFilteringForActiveUsers_MustReturnOnlyActiveUsers()
    {
        // Arrange
        var service = CreateService();
        var activeUser = new User { Forename = "Active", Surname = "User", Email = "active@example.com", DateOfBirth = new DateOnly(1972, 12, 28), IsActive = true };
        var inactiveUser = new User { Forename = "Inactive", Surname = "User", Email = "inactive@example.com", DateOfBirth = new DateOnly(1972, 12, 28), IsActive = false };
        SetupUsers(activeUser, inactiveUser);

        // Act
        var result = service.FilterByActive(true);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(activeUser);
    }

    [Fact]
    public void FilterByActive_WhenFilteringForInactiveUsers_MustReturnOnlyInactiveUsers()
    {
        // Arrange
        var service = CreateService();
        var activeUser = new User { Forename = "Active", Surname = "User", Email = "active@example.com", DateOfBirth = new DateOnly(1972, 12, 28), IsActive = true };
        var inactiveUser = new User { Forename = "Inactive", Surname = "User", Email = "inactive@example.com", DateOfBirth = new DateOnly(1972, 12, 28), IsActive = false };
        SetupUsers(activeUser, inactiveUser);

        // Act
        var result = service.FilterByActive(false);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(inactiveUser);
    }

    [Fact]
    public void FilterByActive_WhenNoUsersMatchFilter_MustReturnEmptyCollection()
    {
        // Arrange
        var service = CreateService();
        var activeUser = new User { Forename = "Active", Surname = "User", Email = "active@example.com", DateOfBirth = new DateOnly(1972, 12, 28), IsActive = true };
        SetupUsers(activeUser);

        // Act
        var result = service.FilterByActive(false);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAll_WhenContextReturnsEntities_MustReturnSameEntities()
    {
        // Arrange: Initializes objects and sets the value of the data that is passed to the method under test.
        var service = CreateService();
        var users = SetupUsers();

        // Act: Invokes the method under test with the arranged parameters.
        var result = service.GetAll();

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().BeSameAs(users);
    }

    private IQueryable<User> SetupUsers(params User[] users)
    {
        var usersQueryable = users.AsQueryable();
        _dataContext.Setup(s => s.GetAll<User>()).Returns(usersQueryable);
        return usersQueryable;
    }

    private IQueryable<User> SetupUsers(string forename = "Johnny", string surname = "User", string email = "juser@example.com", DateOnly dateOfBirth = default, bool isActive = true)
    {
        return SetupUsers(new User { Forename = forename, Surname = surname, Email = email, DateOfBirth = dateOfBirth, IsActive = isActive });
    }

    private readonly Mock<IDataContext> _dataContext = new();
    private UserService CreateService() => new(_dataContext.Object);
}
