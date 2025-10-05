using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Data.Entities;
using UserManagement.Services.Implementations;

namespace UserManagement.Services.Tests;

public class UserServiceTests
{
    [Fact]
    public async Task Create_WhenNewUserCreated_UserShouldBePresentInDataContext()
    {
        //Arrange
        var context = CreateInMemoryContext();
        var newUser = new User
        {
            Forename = "New",
            Surname = "User",
            Email = "newuser@gmail.com",
            DateOfBirth = new DateOnly(1962, 5, 23),
            IsActive = true
        };

        var service = new UserService(context);

        //Act
        await service.Create(newUser);

        //Assert
        var users = await service.GetAll();

        users.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(newUser);
    }

    [Fact]
    public async Task DeleteById_WhenUserExists_UserShouldNotBePresentInDataContext()
    {
        //Arrange
        var context = CreateInMemoryContext();
        var userToBeDeleted = new User
        {
            Id = -2,
            Forename = "New",
            Surname = "User",
            Email = "newuser@gmail.com",
            DateOfBirth = new DateOnly(1962, 5, 23),
            IsActive = true
        };

        var service = new UserService(context);
        await service.Create(userToBeDeleted);

        //Act
        await service.DeleteById(userToBeDeleted.Id);

        //Assert
        var updatedUser = await service.GetById(userToBeDeleted.Id);
        updatedUser.Should().BeNull();
    }

    [Fact]
    public async Task Delete_WhenUserDoesNotExist_ShouldThrowException()
    {
        //Arrange
        var context = CreateInMemoryContext();
        var service = new UserService(context);

        //Act & Assert
        await service.Invoking(s => s.DeleteById(int.MaxValue))
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task FilterByActive_WhenFilteringForActiveUsers_MustReturnOnlyActiveUsers()
    {
        // Arrange
        var context = CreateInMemoryContext();
        await SeedUsers(context,
            new User
            {
                Forename = "Active",
                Surname = "User",
                Email = "active@example.com",
                DateOfBirth = new DateOnly(1972, 12, 28),
                IsActive = true
            },
            new User
            {
                Forename = "Inactive",
                Surname = "User",
                Email = "inactive@example.com",
                DateOfBirth = new DateOnly(1972, 12, 28),
                IsActive = false
            });
        var service = new UserService(context);

        // Act
        var result = await service.FilterByActive(true);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Forename = "Active", IsActive = true });
    }

    [Fact]
    public async Task FilterByActive_WhenFilteringForInactiveUsers_MustReturnOnlyInactiveUsers()
    {
        // Arrange
        var context = CreateInMemoryContext();
        await SeedUsers(context,
            new User
            {
                Forename = "Active",
                Surname = "User",
                Email = "active@example.com",
                DateOfBirth = new DateOnly(1972, 12, 28),
                IsActive = true
            },
            new User
            {
                Forename = "Inactive",
                Surname = "User",
                Email = "inactive@example.com",
                DateOfBirth = new DateOnly(1972, 12, 28),
                IsActive = false
            });
        var service =  new UserService(context);

        // Act
        var result = await service.FilterByActive(false);

        // Assert
        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Forename = "Inactive", IsActive = false });
    }

    [Fact]
    public async Task FilterByActive_WhenNoUsersMatchFilter_MustReturnEmptyCollection()
    {
        // Arrange
        var context = CreateInMemoryContext();
        await SeedUsers(context,
            new User
            {
                Forename = "Active",
                Surname = "User",
                Email = "active@example.com",
                DateOfBirth = new DateOnly(1972, 12, 28),
                IsActive = true
            });
        var service = new UserService(context);

        // Act
        var result = await service.FilterByActive(false);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_WhenContextReturnsEntities_MustReturnSameEntities()
    {
        // Arrange: Initialises objects and sets the value of the data that is passed to the method under test.
        var context = CreateInMemoryContext();
        var user = new User { Forename = "Johnny", Surname = "User", Email = "juser@example.com", DateOfBirth = new DateOnly(1972, 12, 28), IsActive = true };
        await SeedUsers(context, user);

        var service = new UserService(context);

        // Act: Invokes the method under test with the arranged parameters.
        var result = await service.GetAll();

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(user);
    }

    [Fact]
    public async Task GetById_WhenEntityExists_ShouldReturnCorrespondingEntity()
    {
        //Arrange
        var context = CreateInMemoryContext();
        var user = new User { Id = -2, Forename = "Johnny", Surname = "User", Email = "juser@example.com", DateOfBirth = new DateOnly(1972, 12, 28), IsActive = true };
        await SeedUsers(context, user);

        var service = new UserService(context);

        //Act
        var result = await service.GetById(-2);

        //Assert
        result.Should().BeEquivalentTo(user);
    }

    [Fact]
    public async Task GetById_WhenEntityDoesNotExist_ShouldReturnNull()
    {
        //Arrange
        var context = CreateInMemoryContext();
        var service = new UserService(context);

        //Act
        var result = await service.GetById(1);

        //Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_WhenUserExists_UserShouldBeUpdatedWithNewValues()
    {
        //Arrange
        var context = CreateInMemoryContext();
        var newUser = new User
        {
            Id = -2,
            Forename = "New",
            Surname = "User",
            Email = "newuser@gmail.com",
            DateOfBirth = new DateOnly(1962, 5, 23),
            IsActive = true
        };

        var service = new UserService(context);
        await service.Create(newUser);

        var userToUpdate = await service.GetById(newUser.Id);
        userToUpdate!.Forename = "Updated";
        userToUpdate.Surname = "Also";
        userToUpdate.Email = "updated@email.com";
        userToUpdate.DateOfBirth = new DateOnly(1972, 12, 28);
        userToUpdate.IsActive = false;

        //Act
        await service.Update(userToUpdate);

        //Assert
        var updatedUser = await service.GetById(newUser.Id);
        updatedUser.Should().BeEquivalentTo(userToUpdate);
    }

    [Fact]
    public async Task Update_WhenUserDoesNotExist_ShouldThrowException()
    {
        //Arrange
        var context = CreateInMemoryContext();
        var service = new UserService(context);

        //Act & Assert
        await service.Invoking(s => s.Update(new User { Id = 53, Email = "non.existant@email.com" }))
            .Should().ThrowAsync<Exception>();
    }

    private TestDataContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TestDataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDataContext(options);
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
        public DbSet<User> Users { get; set; }

        public IQueryable<TEntity> GetAll<TEntity>() where TEntity : class
            => base.Set<TEntity>();

        public async Task<TEntity?> GetById<TEntity>(long id) where TEntity : class
        {
            return await Set<TEntity>().FindAsync(id);
        }

        public async Task Create<TEntity>(TEntity entity) where TEntity : class
        {
            base.Add(entity);
            await SaveChangesAsync();
        }

        public new async Task Update<TEntity>(TEntity entity) where TEntity : class
        {
            base.Update(entity);
            await SaveChangesAsync();
        }

        public async Task Delete<TEntity>(TEntity entity) where TEntity : class
        {
            base.Remove(entity);
            await SaveChangesAsync();
        }
    }
}
