using System;
using System.Linq;
using System.Threading.Tasks;
using UserManagement.Data.Entities;

namespace UserManagement.Data.Tests;

public class DataContextTests
{
    [Fact]
    public async Task Create_ShouldAddNewEntityToSet()
    {
        //Arrange
        var context = CreateContext();
        var newEntity = new User
        {
            Forename = "Duane",
            Surname = "Jones",
            Email = "duane.jones@notld.com",
            DateOfBirth = new DateOnly(1937, 4, 11)
        };

        //Act
        await context.Create(newEntity);

        //Assert
        var userSet = context.GetAll<User>();

        userSet.AsEnumerable().Should().Contain(s => s.Email == newEntity.Email)
            .Which.Should().BeEquivalentTo(newEntity);
    }

    [Fact]
    public async Task GetAll_WhenNewEntityAdded_MustIncludeNewEntity()
    {
        // Arrange: Initialises objects and sets the value of the data that is passed to the method under test.
        var context = CreateContext();

        var entity = new User
        {
            Forename = "Brand New",
            Surname = "User",
            Email = "brandnewuser@example.com",
            DateOfBirth = new DateOnly(1959, 1, 25)
        };
        await context.Create(entity);

        // Act: Invokes the method under test with the arranged parameters.
        var result = context.GetAll<User>();

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.AsEnumerable()
            .Should().Contain(s => s.Email == entity.Email)
            .Which.Should().BeEquivalentTo(entity);
    }

    [Fact]
    public async Task GetAll_WhenDeleted_MustNotIncludeDeletedEntity()
    {
        // Arrange: Initialises objects and sets the value of the data that is passed to the method under test.
        var context = CreateContext();
        var entity = context.GetAll<User>().First();
        await context.Delete(entity);

        // Act: Invokes the method under test with the arranged parameters.
        var result = context.GetAll<User>();

        // Assert: Verifies that the action of the method under test behaves as expected.
        result.AsEnumerable().Should().NotContain(s => s.Email == entity.Email);
    }

    [Fact]
    public async Task GetById_WhenEntityExists_ShouldReturnEntity()
    {
        //Arrange
        var context = CreateContext();
        var entity = new User
        {
            Id = -1,
            Forename = "Duane",
            Surname = "Jones",
            Email = "duane.jones@notld.com",
            DateOfBirth = new DateOnly(1937, 4, 11)
        };
        await context.Create(entity);

        //Act
        var result = await context.GetById<User>(-1);

        //Assert
        result.Should().BeEquivalentTo(entity);
    }

    [Fact]
    public async Task GetById_WhenEntityDoesNotExist_ShouldReturnNull()
    {
        //Arrange
        var context = CreateContext();

        //Act
        var result = await context.GetById<User>(-1);

        //Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_ShouldUpdateEntity()
    {
        //Arrange
        var context = CreateContext();
        var newEntity = new User
        {
            Id = -2,
            Forename = "Duane",
            Surname = "Jones",
            Email = "duane.jones@notld.com",
            DateOfBirth = new DateOnly(1937, 4, 11)
        };
        await context.Create(newEntity);

        var entityToUpdate = await context.GetById<User>(newEntity.Id);
        entityToUpdate!.Email = "new.email@notld.com";

        //Act
        await context.Update(entityToUpdate);

        //Assert
        var userSet = context.GetAll<User>();

        userSet.AsEnumerable().Should().Contain(s => s.Email == "new.email@notld.com")
            .Which.Id.Should().Be(newEntity.Id);
    }

    private DataContext CreateContext() => new();
}
