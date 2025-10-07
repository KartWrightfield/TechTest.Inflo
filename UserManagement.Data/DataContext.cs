using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data.Entities;

namespace UserManagement.Data;

public sealed class DataContext : DbContext, IDataContext
{
    public DataContext() => Database.EnsureCreated();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseInMemoryDatabase("UserManagement.Data.DataContext");

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<User>().HasData(
            new User { Id = 1, Forename = "Peter", Surname = "Loew", Email = "ploew@example.com", DateOfBirth = new DateOnly(1977, 1, 1), IsActive = true },
            new User { Id = 2, Forename = "Benjamin Franklin", Surname = "Gates", Email = "bfgates@example.com", DateOfBirth = new DateOnly(2000, 11, 11), IsActive = true },
            new User { Id = 3, Forename = "Castor", Surname = "Troy", Email = "ctroy@example.com", DateOfBirth = new DateOnly(1994, 5, 27), IsActive = false },
            new User { Id = 4, Forename = "Memphis", Surname = "Raines", Email = "mraines@example.com", DateOfBirth = new DateOnly(1959, 3, 7), IsActive = true },
            new User { Id = 5, Forename = "Stanley", Surname = "Goodspeed", Email = "sgodspeed@example.com", DateOfBirth = new DateOnly(1988, 7, 18), IsActive = true },
            new User { Id = 6, Forename = "H.I.", Surname = "McDunnough", Email = "himcdunnough@example.com", DateOfBirth = new DateOnly(1977, 1, 31), IsActive = true },
            new User { Id = 7, Forename = "Cameron", Surname = "Poe", Email = "cpoe@example.com", DateOfBirth = new DateOnly(2003, 8, 12), IsActive = false },
            new User { Id = 8, Forename = "Edward", Surname = "Malus", Email = "emalus@example.com", DateOfBirth = new DateOnly(1964, 12, 8), IsActive = false },
            new User { Id = 9, Forename = "Damon", Surname = "Macready", Email = "dmacready@example.com", DateOfBirth = new DateOnly(1932, 11, 26), IsActive = false },
            new User { Id = 10, Forename = "Johnny", Surname = "Blaze", Email = "jblaze@example.com", DateOfBirth = new DateOnly(1973, 1, 15), IsActive = true },
            new User { Id = 11, Forename = "Robin", Surname = "Feld", Email = "rfeld@example.com", DateOfBirth = new DateOnly(1973, 8, 6), IsActive = true });


        model.Entity<Log>().HasData(
            new Log { Id = 1, Action = "Create", EntityType = "User", EntityId = 1, Details = "Created user Peter Loew", Timestamp = DateTime.Parse("2025-09-01T10:00:00Z"), ActionById = 1 },
            new Log { Id = 2, Action = "Create", EntityType = "User", EntityId = 2, Details = "Created user Benjamin Franklin Gates", Timestamp = DateTime.Parse("2025-09-01T10:15:00Z"), ActionById = 1 },
            new Log { Id = 3, Action = "Create", EntityType = "User", EntityId = 3, Details = "Created user Castor Troy", Timestamp = DateTime.Parse("2025-09-02T09:30:00Z"), ActionById = 1 },
            new Log { Id = 4, Action = "Update", EntityType = "User", EntityId = 2, Details = "", Timestamp = DateTime.Parse("2025-09-05T14:22:00Z"), ActionById = 2 },
            new Log { Id = 5, Action = "Update", EntityType = "User", EntityId = 3, Details = "", Timestamp = DateTime.Parse("2025-09-10T11:05:00Z"), ActionById = 1 },
            new Log { Id = 6, Action = "Login", EntityType = "User", EntityId = 1, Details = "User logged in", Timestamp = DateTime.Parse("2025-09-12T08:45:00Z"), ActionById = 1 },
            new Log { Id = 7, Action = "Login", EntityType = "User", EntityId = 2, Details = "User logged in", Timestamp = DateTime.Parse("2025-09-12T09:30:00Z"), ActionById = 2 },
            new Log { Id = 8, Action = "Create", EntityType = "User", EntityId = 4, Details = "Created user Memphis Raines", Timestamp = DateTime.Parse("2025-09-15T16:00:00Z"), ActionById = 1 },
            new Log { Id = 9, Action = "Login", EntityType = "User", EntityId = 4, Details = "User logged in", Timestamp = DateTime.Parse("2025-10-01T08:12:00Z"), ActionById = 4 },
            new Log { Id = 10, Action = "Update", EntityType = "User", EntityId = 1, Details = "", Timestamp = DateTime.Parse("2025-10-05T13:45:00Z"), ActionById = 1 }
        );
    }

    public DbSet<Log> Logs { get; set; }
    public DbSet<User>? Users { get; set; }

    public IQueryable<TEntity> GetAll<TEntity>() where TEntity : class
        => Set<TEntity>();

    public async Task<TEntity?> GetById<TEntity>(long id) where TEntity : class
    {
        return await Set<TEntity>().FindAsync(id);
    }

    public async Task Create<TEntity>(TEntity entity) where TEntity : class
    {
        Add(entity);
        await SaveChangesAsync();
    }

    public new async Task Update<TEntity>(TEntity entity) where TEntity : class
    {
        base.Update(entity);
        await SaveChangesAsync();
    }

    public async Task Delete<TEntity>(TEntity entity) where TEntity : class
    {
        Remove(entity);
        await SaveChangesAsync();
    }
}
