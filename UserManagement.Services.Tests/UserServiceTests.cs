using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Data.Entities;
using UserManagement.Services.Implementations;
using UserManagement.Services.Interfaces;
using UserManagement.Shared.Filters;
using UserManagement.Shared.Models.Users;

namespace UserManagement.Services.Tests;

public class UserServiceTests
{
    private readonly Mock<IValidator<UserInputViewModel>> _validatorMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILoggingService> _loggingServiceMock;
    private readonly TestDataContext _context;
    private readonly IUserService _service;

    public UserServiceTests()
    {
        _context = CreateInMemoryContext();
        _validatorMock = new Mock<IValidator<UserInputViewModel>>();
        _mapperMock = new Mock<IMapper>();
        _loggingServiceMock = new Mock<ILoggingService>();

        _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<UserInputViewModel>(), CancellationToken.None))
            .ReturnsAsync(new ValidationResult());

        _service = new UserService(_context, _validatorMock.Object, _mapperMock.Object, _loggingServiceMock.Object);
    }

    private TestDataContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TestDataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDataContext(options);
    }

    [Fact]
    public async Task Create_EnsuresCorrectOrderOfOperations()
    {
        // Arrange
        var userViewModel = new UserInputViewModel
        {
            Forename = "New",
            Surname = "User",
            Email = "newuser@example.com",
            DateOfBirth = new DateOnly(1990, 1, 1),
            IsActive = true
        };

        var user = new User
        {
            Forename = "New",
            Surname = "User",
            Email = "newuser@example.com",
            DateOfBirth = new DateOnly(1990, 1, 1),
            IsActive = true
        };

        // Using callback to track execution order
        var operationSequence = new List<string>();

        _validatorMock.Setup(v => v.ValidateAsync(userViewModel, CancellationToken.None))
            .Callback(() => operationSequence.Add("Validation"))
            .ReturnsAsync(new ValidationResult());

        _mapperMock.Setup(m => m.Map<User>(userViewModel))
            .Callback(() => operationSequence.Add("Mapping"))
            .Returns(user);

        _context.SetupCreateTracking(() => operationSequence.Add("Database"));

        _loggingServiceMock.Setup(l => l.LogAction(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                null))
            .Callback(() => operationSequence.Add("Logging"))
            .Returns(Task.CompletedTask);

        // Act
        await _service.Create(userViewModel);

        // Assert
        operationSequence.Count.Should().Be(4);
        operationSequence[0].Should().Be("Validation");
        operationSequence[1].Should().Be("Mapping");
        operationSequence[2].Should().Be("Database");
        operationSequence[3].Should().Be("Logging");

        _validatorMock.Verify(v => v.ValidateAsync(userViewModel, CancellationToken.None), Times.Once);
        _mapperMock.Verify(m => m.Map<User>(userViewModel), Times.Once);
        _loggingServiceMock.Verify(l => l.LogAction(
            "Create",
            "User",
            user.Id,
            $"Created user: {userViewModel.FullName}",
            null
        ), Times.Once);
    }

    [Fact]
    public async Task Create_WhenNewUserCreated_UserShouldBePresentInDataContext()
    {
        // Arrange
        var userViewModel = new UserInputViewModel
        {
            Forename = "New",
            Surname = "User",
            Email = "newuser@gmail.com",
            DateOfBirth = new DateOnly(1962, 5, 23),
            IsActive = true
        };

        var user = new User
        {
            Forename = "New",
            Surname = "User",
            Email = "newuser@gmail.com",
            DateOfBirth = new DateOnly(1962, 5, 23),
            IsActive = true
        };

        _mapperMock.Setup(m => m.Map<User>(userViewModel)).Returns(user);

        // Act
        var result = await _service.Create(userViewModel);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        var users = await _context.Users.ToListAsync();
        users.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(user);

        _loggingServiceMock.Verify(l => l.LogAction(
            "Create",
            "User",
            user.Id,
            $"Created user: {userViewModel.FullName}",
            null
        ), Times.Once);
    }

    [Fact]
    public async Task Create_WhenValidationFails_ShouldReturnErrors()
    {
        // Arrange
        var invalidUser = new UserInputViewModel
        {
            // Missing required fields
            Email = "invalid-email"  // Invalid email format
        };

        var validationFailures = new[]
        {
            new ValidationFailure("Forename", "Forename is required"),
            new ValidationFailure("Surname", "Surname is required"),
            new ValidationFailure("Email", "Invalid email format")
        };

        var validationResult = new ValidationResult(validationFailures);

        _validatorMock.Setup(v => v.ValidateAsync(invalidUser, CancellationToken.None))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _service.Create(invalidUser);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain(["Forename is required", "Surname is required", "Invalid email format"]);

        // Verify that neither Create nor logAction was never called on the data context
        var users = await _context.Users.ToListAsync();
        users.Should().BeEmpty();

        _loggingServiceMock.Verify(l => l.LogAction(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            It.IsAny<string>(),
            null
        ), Times.Never);
    }

    [Fact]
    public async Task Create_WithInvalidDateOfBirth_ShouldFailValidation()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1));
        var userWithFutureDate = new UserInputViewModel
        {
            Forename = "Future",
            Surname = "User",
            Email = "future@example.com",
            DateOfBirth = futureDate,
            IsActive = true
        };

        var validationFailure = new ValidationFailure("DateOfBirth", "Date of birth cannot be in the future");
        var validationResult = new ValidationResult([validationFailure]);

        _validatorMock.Setup(v => v.ValidateAsync(userWithFutureDate, CancellationToken.None))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _service.Create(userWithFutureDate);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Be("Date of birth cannot be in the future");
    }

    [Fact]
    public async Task DeleteById_EnsuresCorrectOrderOfOperations()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Forename = "Test",
            Surname = "User",
            Email = "test@example.com",
            DateOfBirth = new DateOnly(1980, 1, 1),
            IsActive = true
        };

        await _context.Create(user);

        // Using callback to track execution order
        var operationSequence = new List<string>();

        // Setup delete tracking
        _context.SetupDeleteTracking(() => operationSequence.Add("Database"));

        _loggingServiceMock.Setup(l => l.LogAction(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                null))
            .Callback(() => operationSequence.Add("Logging"))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteById(user.Id);

        // Assert
        operationSequence.Count.Should().Be(2);
        operationSequence[0].Should().Be("Database");
        operationSequence[1].Should().Be("Logging");

        _loggingServiceMock.Verify(l => l.LogAction(
            "Delete",
            "User",
            user.Id,
            string.Empty,
            null
        ), Times.Once);
    }

    [Fact]
    public async Task DeleteById_WhenUserExists_UserShouldNotBePresentInDataContext()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Forename = "New",
            Surname = "User",
            Email = "newuser@gmail.com",
            DateOfBirth = new DateOnly(1962, 5, 23),
            IsActive = true
        };

        await _context.Create(user);

        // Act
        var result = await _service.DeleteById(user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        var deletedUser = await _context.GetById<User>(user.Id);
        deletedUser.Should().BeNull();

        _loggingServiceMock.Verify(l => l.LogAction(
            "Delete",
            "User",
            user.Id,
            string.Empty,
            null
        ), Times.Once);
    }

    [Fact]
    public async Task DeleteById_WhenUserDoesNotExist_ShouldReturnError()
    {
        // Arrange
        const long nonExistentUserId = long.MaxValue;

        // Act
        var result = await _service.DeleteById(nonExistentUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("User not found");

        _loggingServiceMock.Verify(l => l.LogAction(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            It.IsAny<string>(),
            null
        ), Times.Never);
    }

    [Fact]
    public async Task GetById_VerifyMapperIsCalledWithCorrectUser()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Forename = "Test",
            Surname = "User",
            Email = "test@example.com",
            DateOfBirth = new DateOnly(1980, 1, 1),
            IsActive = true
        };

        await SeedUsers(_context, user);

        var userViewModel = new UserViewModel
        {
            Id = 1,
            Forename = "Test",
            Surname = "User",
            Email = "test@example.com",
            DateOfBirth = new DateOnly(1980, 1, 1),
            IsActive = true
        };

        _mapperMock.Setup(m => m.Map<UserViewModel>(It.IsAny<User>()))
            .Returns(userViewModel);

        // Act
        await _service.GetById(1);

        // Assert
        _mapperMock.Verify(m => m.Map<UserViewModel>(It.Is<User>(u => u.Id == 1)), Times.Once);
    }

    [Fact]
    public async Task Get_VerifyMapperIsCalledForEachUser()
    {
        // Arrange
        var users = new[]
        {
            new User { Id = 1, Forename = "User1", Surname = "Test", Email = "user1@example.com", DateOfBirth = new DateOnly(1980, 1, 1), IsActive = true },
            new User { Id = 2, Forename = "User2", Surname = "Test", Email = "user2@example.com", DateOfBirth = new DateOnly(1985, 5, 5), IsActive = true }
        };

        await SeedUsers(_context, users);

        var mapperCallCount = 0;

        _mapperMock.Setup(m => m.Map<UserListItemViewModel>(It.IsAny<User>()))
            .Returns<User>(u => {
                mapperCallCount++;
                return new UserListItemViewModel
                {
                    Id = u.Id,
                    Forename = u.Forename,
                    Surname = u.Surname,
                    Email = u.Email,
                    IsActive = u.IsActive
                };
            });

        // Act
        var result = await _service.Get();

        // Assert
        var resultList = result.ToList();

        mapperCallCount.Should().Be(2);

        resultList.Should().HaveCount(2);
        resultList.Should().Contain(vm => vm.Id == 1 && vm.Forename == "User1");
        resultList.Should().Contain(vm => vm.Id == 2 && vm.Forename == "User2");
    }

    [Fact]
    public async Task Get_WithComplexFilter_ShouldApplyAllCriteria()
    {
        // Arrange
        // This test anticipates future filter expansion
        var users = new[]
        {
            new User { Id = 1, Forename = "Ben", Surname = "Active", Email = "ben@example.com", DateOfBirth = new DateOnly(1980, 1, 1), IsActive = true },
            new User { Id = 2, Forename = "Barbra", Surname = "Active", Email = "barbra@example.com", DateOfBirth = new DateOnly(1985, 5, 5), IsActive = true },
            new User { Id = 3, Forename = "Harry", Surname = "Inactive", Email = "harry@example.com", DateOfBirth = new DateOnly(1990, 10, 10), IsActive = false }
        };

        await SeedUsers(_context, users);

        var activeUserViewModels = new[]
        {
            new UserListItemViewModel { Id = 1, Forename = "Ben", Surname = "Active", Email = "ben@example.com", IsActive = true },
            new UserListItemViewModel { Id = 2, Forename = "Barbra", Surname = "Active", Email = "barbra@example.com", IsActive = true }
        };

        // Set up mapping for all active users
        _mapperMock.Setup(m => m.Map<UserListItemViewModel>(It.Is<User>(u => u.IsActive)))
            .Returns<User>(u => new UserListItemViewModel
            {
                Id = u.Id,
                Forename = u.Forename,
                Surname = u.Surname,
                Email = u.Email,
                IsActive = u.IsActive
            });

        // Using existing filter with ActiveStatus
        var filter = new UserFilter { ActiveStatus = true };

        // Act
        var result = await _service.Get(filter);

        // Assert
        // Currently only testing Active Status but demonstrates how to test when if the filter gets more complex
        result.Should().HaveCount(2);
        result.Should().Contain(viewModel => viewModel.Id == 1);
        result.Should().Contain(viewModel => viewModel.Id == 2);
        result.Should().NotContain(viewModel => viewModel.Id == 3);
    }

    [Fact]
    public async Task Get_WhenFilteringForActiveUsers_MustReturnOnlyActiveUsers()
    {
        // Arrange
        var activeUser = new User
        {
            Id = 1,
            Forename = "Active",
            Surname = "User",
            Email = "active@example.com",
            DateOfBirth = new DateOnly(1972, 12, 28),
            IsActive = true
        };

        var inactiveUser = new User
        {
            Id = 2,
            Forename = "Inactive",
            Surname = "User",
            Email = "inactive@example.com",
            DateOfBirth = new DateOnly(1972, 12, 28),
            IsActive = false
        };

        await SeedUsers(_context, activeUser, inactiveUser);

        var activeUserViewModel = new UserListItemViewModel
        {
            Id = 1,
            Forename = "Active",
            Surname = "User",
            Email = "active@example.com",
            IsActive = true
        };

        _mapperMock.Setup(m => m.Map<UserListItemViewModel>(activeUser))
            .Returns(activeUserViewModel);

        // Act
        var result = await _service.Get(new UserFilter { ActiveStatus = true });

        // Assert
        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(activeUserViewModel);
    }

    [Fact]
    public async Task Get_WhenFilteringForInactiveUsers_MustReturnOnlyInactiveUsers()
    {
        // Arrange
        var activeUser = new User
        {
            Id = 1,
            Forename = "Active",
            Surname = "User",
            Email = "active@example.com",
            DateOfBirth = new DateOnly(1972, 12, 28),
            IsActive = true
        };

        var inactiveUser = new User
        {
            Id = 2,
            Forename = "Inactive",
            Surname = "User",
            Email = "inactive@example.com",
            DateOfBirth = new DateOnly(1972, 12, 28),
            IsActive = false
        };

        await SeedUsers(_context, activeUser, inactiveUser);

        var inactiveUserViewModel = new UserListItemViewModel
        {
            Id = 2,
            Forename = "Inactive",
            Surname = "User",
            Email = "inactive@example.com",
            IsActive = false
        };

        _mapperMock.Setup(m => m.Map<UserListItemViewModel>(inactiveUser))
            .Returns(inactiveUserViewModel);

        // Act
        var result = await _service.Get(new UserFilter { ActiveStatus = false });

        // Assert
        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(inactiveUserViewModel);
    }

    [Fact]
    public async Task Get_WhenNoUsersMatchFilter_MustReturnEmptyCollection()
    {
        // Arrange
        var activeUser = new User
        {
            Id = 1,
            Forename = "Active",
            Surname = "User",
            Email = "active@example.com",
            DateOfBirth = new DateOnly(1972, 12, 28),
            IsActive = true
        };

        await SeedUsers(_context, activeUser);

        // Act
        var result = await _service.Get(new UserFilter { ActiveStatus = false });

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_WhenNoFilterIsProvided_MustReturnFullCollection()
    {
        // Arrange
        var users = new[]
        {
            new User { Id = 1, Forename = "Active", Surname = "User", Email = "active@example.com", DateOfBirth = new DateOnly(1972, 12, 28), IsActive = true },
            new User { Id = 2, Forename = "Inactive", Surname = "User", Email = "inactive@example.com", DateOfBirth = new DateOnly(1972, 12, 28), IsActive = false }
        };

        await SeedUsers(_context, users);

        var userViewModels = new[]
        {
            new UserListItemViewModel { Id = 1, Forename = "Active", Surname = "User", Email = "active@example.com", IsActive = true },
            new UserListItemViewModel { Id = 2, Forename = "Inactive", Surname = "User", Email = "inactive@example.com", IsActive = false }
        };

        _mapperMock.Setup(m => m.Map<UserListItemViewModel>(users[0])).Returns(userViewModels[0]);
        _mapperMock.Setup(m => m.Map<UserListItemViewModel>(users[1])).Returns(userViewModels[1]);

        // Act
        var result = await _service.Get();

        // Assert
        result.Should().HaveCount(2)
            .And.BeEquivalentTo(userViewModels);
    }

    [Fact]
    public async Task GetById_WhenEntityExists_ShouldReturnCorrespondingEntity()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Forename = "Johnny",
            Surname = "User",
            Email = "juser@example.com",
            DateOfBirth = new DateOnly(1972, 12, 28),
            IsActive = true
        };

        await SeedUsers(_context, user);

        var userViewModel = new UserViewModel
        {
            Id = 1,
            Forename = "Johnny",
            Surname = "User",
            Email = "juser@example.com",
            DateOfBirth = new DateOnly(1972, 12, 28),
            IsActive = true
        };

        _mapperMock.Setup(m => m.Map<UserViewModel>(user))
            .Returns(userViewModel);

        // Act
        var result = await _service.GetById(1);

        // Assert
        result.Found.Should().BeTrue();
        result.User.Should().BeEquivalentTo(userViewModel);
    }

    [Fact]
    public async Task GetById_WhenEntityDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        // Act
        var result = await _service.GetById(1);

        // Assert
        result.Found.Should().BeFalse();
        result.User.Should().BeNull();
    }

    [Fact]
    public async Task Update_EnsuresCorrectOrderOfOperations()
    {
        // Arrange
        var existingUser = new User
        {
            Id = 1,
            Forename = "Old",
            Surname = "User",
            Email = "old@example.com",
            DateOfBirth = new DateOnly(1962, 5, 23),
            IsActive = true
        };

        await _context.Create(existingUser);

        var updatedUserViewModel = new UserInputViewModel
        {
            Id = 1,
            Forename = "Updated",
            Surname = "User",
            Email = "old@example.com",
            DateOfBirth = new DateOnly(1962, 5, 23),
            IsActive = true
        };

        // Using callback to track execution order
        var operationSequence = new List<string>();

        _validatorMock.Setup(v => v.ValidateAsync(updatedUserViewModel, CancellationToken.None))
            .Callback(() => operationSequence.Add("Validation"))
            .ReturnsAsync(new ValidationResult());

        _mapperMock.Setup(m => m.Map(updatedUserViewModel, existingUser))
            .Callback(() => operationSequence.Add("Mapping"));

        _context.SetupUpdateTracking(() => operationSequence.Add("Database"));

        _loggingServiceMock.Setup(l => l.LogAction(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<string>(),
                null))
            .Callback(() => operationSequence.Add("Logging"))
            .Returns(Task.CompletedTask);

        // Act
        await _service.Update(updatedUserViewModel);

        // Assert
        operationSequence.Count.Should().Be(4);
        operationSequence[0].Should().Be("Validation");
        operationSequence[1].Should().Be("Mapping");
        operationSequence[2].Should().Be("Database");
        operationSequence[3].Should().Be("Logging");
    }

    [Fact]
    public async Task Update_WhenUserExists_UserShouldBeUpdatedWithNewValues()
    {
        // Arrange
        var existingUser = new User
        {
            Id = 1,
            Forename = "Old",
            Surname = "User",
            Email = "old@example.com",
            DateOfBirth = new DateOnly(1962, 5, 23),
            IsActive = true
        };

        await _context.Create(existingUser);

        var updatedUserViewModel = new UserInputViewModel
        {
            Id = 1,
            Forename = "Updated",
            Surname = "Also",
            Email = "updated@email.com",
            DateOfBirth = new DateOnly(1972, 12, 28),
            IsActive = false
        };

        _mapperMock.Setup(m => m.Map(updatedUserViewModel, It.IsAny<User>()))
            .Callback<UserInputViewModel, User>((vm, user) => {
                // Manually update the user properties here
                user.Forename = vm.Forename!;
                user.Surname = vm.Surname!;
                user.Email = vm.Email!;
                user.DateOfBirth = (DateOnly)vm.DateOfBirth!;
                user.IsActive = vm.IsActive;
            });

        // Act
        var result = await _service.Update(updatedUserViewModel);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        var userInDb = await _context.GetById<User>(1);
        userInDb.Should().NotBeNull();
        userInDb!.Forename.Should().Be("Updated");
        userInDb.Surname.Should().Be("Also");
        userInDb.Email.Should().Be("updated@email.com");
        userInDb.DateOfBirth.Should().Be(new DateOnly(1972, 12, 28));
        userInDb.IsActive.Should().BeFalse();

        _loggingServiceMock.Verify(l => l.LogAction(
            "Update",
            "User",
            1,
            It.Is<string>(s =>
                s.Contains("Forename changed from 'Old' to 'Updated'") &&
                s.Contains("Surname changed from 'User' to 'Also'") &&
                s.Contains("Email changed from 'old@example.com' to 'updated@email.com'") &&
                s.Contains("Date of Birth changed from '23/05/1962' to '28/12/1972'") &&
                s.Contains("Active status changed from 'True' to 'False'")),
            null
        ), Times.Once);
    }

    [Fact]
    public async Task Update_WhenUserDoesNotExist_ShouldReturnErrorAndNotLog()
    {
        // Arrange
        var nonExistentUserViewModel = new UserInputViewModel
        {
            Id = 999,
            Forename = "NonExistent",
            Surname = "User",
            Email = "nonexistent@example.com",
            DateOfBirth = new DateOnly(1980, 1, 1),
            IsActive = true
        };

        // Act
        var result = await _service.Update(nonExistentUserViewModel);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("User not found");

        // Verify that logging was never called
        _loggingServiceMock.Verify(l => l.LogAction(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<long>(),
            It.IsAny<string>(),
            null
        ), Times.Never);
    }

    [Fact]
    public async Task Update_WhenValidationFails_ShouldReturnErrors()
    {
        // Arrange
        var validationFailure = new ValidationFailure("Email", "Email is required");
        var validationResult = new ValidationResult([validationFailure]);

        var userViewModel = new UserInputViewModel
        {
            Id = 1,
            Forename = "Updated",
            Surname = "Also",
            // Missing email
            DateOfBirth = new DateOnly(1972, 12, 28),
            IsActive = false
        };

        _validatorMock.Setup(v => v.ValidateAsync(userViewModel, CancellationToken.None))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _service.Update(userViewModel);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Be("Email is required");
    }

    [Fact]
    public async Task Update_WithChangedProperties_LogsCorrectChanges()
    {
        // Arrange
        var existingUser = new User
        {
            Id = 1,
            Forename = "Old",
            Surname = "User",
            Email = "old@example.com",
            DateOfBirth = new DateOnly(1962, 5, 23),
            IsActive = true
        };

        await _context.Create(existingUser);

        var updatedUserViewModel = new UserInputViewModel
        {
            Id = 1,
            Forename = "Updated", // Changed
            Surname = "NewSurname", // Changed
            Email = "old@example.com", // Unchanged
            DateOfBirth = new DateOnly(1962, 5, 23), // Unchanged
            IsActive = true // Unchanged
        };

        // Act
        var result = await _service.Update(updatedUserViewModel);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _loggingServiceMock.Verify(l => l.LogAction(
            "Update",
            "User",
            1,
            It.Is<string>(s =>
                s.Contains("Forename changed from 'Old' to 'Updated'") &&
                s.Contains("Surname changed from 'User' to 'NewSurname'")),
            null
        ), Times.Once);
    }



    private async Task SeedUsers(TestDataContext context, params User[] users)
    {
        foreach (var user in users)
        {
            await context.Users.AddAsync(user);
        }
        await context.SaveChangesAsync();
    }

    // A simplified version of DataContext for testing
    public class TestDataContext(DbContextOptions<TestDataContext> options) : DbContext(options), IDataContext
    {
        private Action? _createCallback;
        private Action? _deleteCallback;
        private Action? _updateCallback;

        public DbSet<Log> Logs { get; set; }
        public DbSet<User> Users { get; set; }

        public IQueryable<TEntity> GetAll<TEntity>() where TEntity : class
            => base.Set<TEntity>();

        public async Task<TEntity?> GetById<TEntity>(long id) where TEntity : class
        {
            return await Set<TEntity>().FindAsync(id);
        }

        public async Task Create<TEntity>(TEntity entity) where TEntity : class
        {
            _createCallback?.Invoke();
            base.Add(entity);
            await SaveChangesAsync();
        }

        public new async Task Update<TEntity>(TEntity entity) where TEntity : class
        {
            base.Update(entity);
            _updateCallback?.Invoke();
            await SaveChangesAsync();
        }

        public async Task Delete<TEntity>(TEntity entity) where TEntity : class
        {
            base.Remove(entity);
            _deleteCallback?.Invoke();
            await SaveChangesAsync();
        }


        // --- Test helper methods ---
        public void SetupCreateTracking(Action callback)
        {
            _createCallback = callback;
        }

        public void SetupDeleteTracking(Action callback)
        {
            _deleteCallback = callback;
        }

        public void SetupUpdateTracking(Action callback)
        {
            _updateCallback = callback;
        }
    }
}
