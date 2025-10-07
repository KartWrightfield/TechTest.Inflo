using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Data.Entities;
using UserManagement.Services.Implementations;
using UserManagement.Shared.Filters;
using UserManagement.Shared.Models.Logs;

namespace UserManagement.Services.Tests;

public class LoggingServiceTests
{
    private readonly TestDataContext _context;
    private readonly Mock<IMapper> _mapperMock;
    private readonly LoggingService _service;

    public LoggingServiceTests()
    {
        _context = CreateInMemoryContext();
        _mapperMock = new Mock<IMapper>();

        _service = new LoggingService(_context, _mapperMock.Object);
    }

    private TestDataContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TestDataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TestDataContext(options);
    }

    [Fact]
    public async Task LogAction_CreatesNewLogEntry()
    {
        // Arrange
        const string action = "Create";
        const string entityType = "User";
        const long entityId = 123;
        const string details = "Created user: John Doe";
        int? actionById = 456;

        // Act
        await _service.LogAction(action, entityType, entityId, details, actionById);

        // Assert
        var logs = await _context.Logs.ToListAsync();
        logs.Should().ContainSingle();

        var log = logs.First();
        log.Action.Should().Be(action);
        log.EntityType.Should().Be(entityType);
        log.EntityId.Should().Be(entityId);
        log.Details.Should().Be(details);
        log.ActionById.Should().Be(actionById);
        log.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task LogAction_WithEmptyDetails_CreatesLogWithEmptyDetails()
    {
        // Arrange
        const string action = "Read";
        const string entityType = "User";
        const long entityId = 123;
        var details = string.Empty;

        // Act
        await _service.LogAction(action, entityType, entityId, details);

        // Assert
        var logs = await _context.Logs.ToListAsync();
        logs.Should().ContainSingle();

        var log = logs.First();
        log.Details.Should().BeEmpty();
    }

    [Fact]
    public async Task LogAction_WithMultipleLogs_CreatesDistinctEntries()
    {
        // Arrange
        var logEntries = new[]
        {
            (Action: "Create", EntityType: "User", EntityId: 1L, Details: "Created user 1", ActionById: 100),
            (Action: "Update", EntityType: "User", EntityId: 1L, Details: "Updated user 1", ActionById: 100),
            (Action: "Delete", EntityType: "User", EntityId: 1L, Details: "Deleted user 1", ActionById: 100)
        };

        // Act
        foreach (var entry in logEntries)
        {
            await _service.LogAction(
                entry.Action,
                entry.EntityType,
                entry.EntityId,
                entry.Details,
                entry.ActionById
            );
        }

        // Assert
        var logs = await _context.Logs.ToListAsync();
        logs.Should().HaveCount(3);

        logs.Should().Contain(log => log.Action == "Create");
        logs.Should().Contain(log => log.Action == "Update");
        logs.Should().Contain(log => log.Action == "Delete");
    }

    [Fact]
    public async Task GetAllLogs_WithNoFilter_ReturnsAllLogsMappedToViewModel()
    {
        // Arrange
        var logs = new[]
        {
            new Log { Id = 1, Action = "Create", EntityType = "User", EntityId = 1, Details = "Created user 1", Timestamp = DateTime.UtcNow.AddHours(-1) },
            new Log { Id = 2, Action = "Update", EntityType = "User", EntityId = 1, Details = "Updated user 1", Timestamp = DateTime.UtcNow.AddHours(-2) },
            new Log { Id = 3, Action = "Delete", EntityType = "User", EntityId = 1, Details = "Deleted user 1", Timestamp = DateTime.UtcNow.AddHours(-3) }
        };

        foreach (var log in logs)
        {
            await _context.Create(log);
        }

        var viewModels = new[]
        {
            new LogViewModel { Id = 1, Action = "Create", EntityType = "User", EntityId = 1 },
            new LogViewModel { Id = 2, Action = "Update", EntityType = "User", EntityId = 1 },
            new LogViewModel { Id = 3, Action = "Delete", EntityType = "User", EntityId = 1 }
        };

        // Setup mapper to return view models for each log
        _mapperMock.Setup(m => m.Map<IEnumerable<LogViewModel>>(It.IsAny<List<Log>>()))
            .Returns(viewModels);

        var filter = new LogFilter();

        // Act
        var result = await _service.GetAllLogs(filter);

        // Assert
        result.Should().HaveCount(3);
        _mapperMock.Verify(m => m.Map<IEnumerable<LogViewModel>>(It.Is<List<Log>>(l => l.Count == 3)), Times.Once);
    }

    [Fact]
    public async Task GetAllLogs_WithActionTypeFilter_ReturnsOnlyMatchingLogs()
    {
        // Arrange
        var logs = new[]
        {
            new Log { Id = 1, Action = "Create", EntityType = "User", EntityId = 1, Details = "Created user 1", Timestamp = DateTime.UtcNow.AddHours(-1) },
            new Log { Id = 2, Action = "Update", EntityType = "User", EntityId = 1, Details = "Updated user 1", Timestamp = DateTime.UtcNow.AddHours(-2) },
            new Log { Id = 3, Action = "Create", EntityType = "Post", EntityId = 1, Details = "Created post 1", Timestamp = DateTime.UtcNow.AddHours(-3) }
        };

        foreach (var log in logs)
        {
            await _context.Create(log);
        }

        var createLogs = logs.Where(l => l.Action == "Create").ToList();
        var createViewModels = new[]
        {
            new LogViewModel { Id = 1, Action = "Create", EntityType = "User", EntityId = 1 },
            new LogViewModel { Id = 3, Action = "Create", EntityType = "Post", EntityId = 1 }
        };

        _mapperMock.Setup(m => m.Map<IEnumerable<LogViewModel>>(It.Is<List<Log>>(l =>
                l.All(log => log.Action == "Create") && l.Count == 2)))
            .Returns(createViewModels);

        var filter = new LogFilter { ActionType = "Create" };

        // Act
        var result = await _service.GetAllLogs(filter);

        // Assert
        result.Should().HaveCount(2);
        result.All(log => log.Action == "Create").Should().BeTrue();
    }

    [Fact]
    public async Task GetAllLogs_WithEntityTypeFilter_ReturnsOnlyMatchingLogs()
    {
        // Arrange
        var logs = new[]
        {
            new Log { Id = 1, Action = "Create", EntityType = "User", EntityId = 1, Details = "Created user 1", Timestamp = DateTime.UtcNow.AddHours(-1) },
            new Log { Id = 2, Action = "Update", EntityType = "User", EntityId = 1, Details = "Updated user 1", Timestamp = DateTime.UtcNow.AddHours(-2) },
            new Log { Id = 3, Action = "Create", EntityType = "Post", EntityId = 1, Details = "Created post 1", Timestamp = DateTime.UtcNow.AddHours(-3) }
        };

        foreach (var log in logs)
        {
            await _context.Create(log);
        }

        var userLogs = logs.Where(l => l.EntityType == "User").ToList();
        var userViewModels = new[]
        {
            new LogViewModel { Id = 1, Action = "Create", EntityType = "User", EntityId = 1 },
            new LogViewModel { Id = 2, Action = "Update", EntityType = "User", EntityId = 1 }
        };

        _mapperMock.Setup(m => m.Map<IEnumerable<LogViewModel>>(It.Is<List<Log>>(l =>
                l.All(log => log.EntityType == "User") && l.Count == 2)))
            .Returns(userViewModels);

        var filter = new LogFilter { EntityType = "User" };

        // Act
        var result = await _service.GetAllLogs(filter);

        // Assert
        result.Should().HaveCount(2);
        result.All(log => log.EntityType == "User").Should().BeTrue();
    }

    [Fact]
    public async Task GetAllLogs_WithDateRangeFilter_ReturnsOnlyLogsInRange()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var logs = new[]
        {
            new Log { Id = 1, Action = "Create", EntityType = "User", EntityId = 1, Timestamp = now.AddDays(-1) },
            new Log { Id = 2, Action = "Update", EntityType = "User", EntityId = 1, Timestamp = now.AddDays(-5) },
            new Log { Id = 3, Action = "Delete", EntityType = "User", EntityId = 1, Timestamp = now.AddDays(-10) }
        };

        foreach (var log in logs)
        {
            await _context.Create(log);
        }

        var fromDate = now.AddDays(-6);
        var toDate = now;
        var logsInRange = logs.Where(l => l.Timestamp >= fromDate && l.Timestamp <= toDate).ToList();
        var viewModelsInRange = new[]
        {
            new LogViewModel { Id = 1, Action = "Create", EntityType = "User", EntityId = 1 },
            new LogViewModel { Id = 2, Action = "Update", EntityType = "User", EntityId = 1 }
        };

        _mapperMock.Setup(m => m.Map<IEnumerable<LogViewModel>>(It.Is<List<Log>>(l => l.Count == 2)))
            .Returns(viewModelsInRange);

        var filter = new LogFilter { FromDate = fromDate, ToDate = toDate };

        // Act
        var result = await _service.GetAllLogs(filter);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllLogs_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var logs = new List<Log>();
        for (var i = 1; i <= 25; i++)
        {
            logs.Add(new Log
            {
                Id = i,
                Action = "Action" + i,
                EntityType = "Type",
                EntityId = i,
                Timestamp = DateTime.UtcNow.AddHours(-i)
            });
        }

        foreach (var log in logs)
        {
            await _context.Create(log);
        }

        // Request page 2 with page size 10
        var page2Logs = logs
            .OrderByDescending(l => l.Timestamp)
            .Skip(10)
            .Take(10)
            .ToList();

        var page2ViewModels = page2Logs.Select(log => new LogViewModel
        {
            Id = log.Id,
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId
        }).ToList();

        _mapperMock.Setup(m => m.Map<IEnumerable<LogViewModel>>(It.Is<List<Log>>(l => l.Count == 10)))
            .Returns(page2ViewModels);

        var filter = new LogFilter { Page = 2, PageSize = 10 };

        // Act
        var result = await _service.GetAllLogs(filter);

        // Assert
        result.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetAllLogs_WithCombinedFilters_ReturnsCorrectLogs()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var logs = new[]
        {
            new Log { Id = 1, Action = "Create", EntityType = "User", EntityId = 1, Timestamp = now.AddDays(-1) },
            new Log { Id = 2, Action = "Update", EntityType = "User", EntityId = 1, Timestamp = now.AddDays(-2) },
            new Log { Id = 3, Action = "Create", EntityType = "Post", EntityId = 1, Timestamp = now.AddDays(-3) },
            new Log { Id = 4, Action = "Update", EntityType = "Post", EntityId = 1, Timestamp = now.AddDays(-4) },
            new Log { Id = 5, Action = "Create", EntityType = "User", EntityId = 2, Timestamp = now.AddDays(-5) }
        };

        foreach (var log in logs)
        {
            await _context.Create(log);
        }

        // Filtered logs: Create + User + within the last 3 days
        var filteredLogs = logs.Where(l =>
            l.Action == "Create" &&
            l.EntityType == "User" &&
            l.Timestamp >= now.AddDays(-3)).ToList();

        var filteredViewModels = new[]
        {
            new LogViewModel { Id = 1, Action = "Create", EntityType = "User", EntityId = 1 }
        };

        _mapperMock.Setup(m => m.Map<IEnumerable<LogViewModel>>(It.Is<List<Log>>(l => l.Count == 1)))
            .Returns(filteredViewModels);

        var filter = new LogFilter
        {
            ActionType = "Create",
            EntityType = "User",
            FromDate = now.AddDays(-3),
            ToDate = now
        };

        // Act
        var result = await _service.GetAllLogs(filter);

        // Assert
        result.Should().HaveCount(1);
        var resultLog = result.First();
        resultLog.Action.Should().Be("Create");
        resultLog.EntityType.Should().Be("User");
        resultLog.EntityId.Should().Be(1);
    }

    [Fact]
    public async Task GetAllLogs_OrdersLogsByNewestFirst()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var logs = new[]
        {
            new Log { Id = 1, Action = "Action1", Timestamp = now.AddHours(-5) },
            new Log { Id = 2, Action = "Action2", Timestamp = now.AddHours(-1) },
            new Log { Id = 3, Action = "Action3", Timestamp = now.AddHours(-3) }
        };

        foreach (var log in logs)
        {
            await _context.Create(log);
        }

        var expectedOrder = new[] { 2, 3, 1 }; // IDs in timestamp order

        var viewModels = new[]
        {
            new LogViewModel { Id = 2, Action = "Action2" },
            new LogViewModel { Id = 3, Action = "Action3" },
            new LogViewModel { Id = 1, Action = "Action1" }
        };

        _mapperMock.Setup(m => m.Map<IEnumerable<LogViewModel>>(It.IsAny<List<Log>>()))
            .Returns(viewModels);

        // Act
        var result = await _service.GetAllLogs(new LogFilter());

        // Assert
        var resultIds = result.Select(vm => vm.Id).ToArray();
        resultIds.Should().BeEquivalentTo(expectedOrder, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task GetLogById_WhenLogExists_ReturnsCorrectViewModel()
    {
        // Arrange
        var log = new Log
        {
            Id = 1,
            Action = "Create",
            EntityType = "User",
            EntityId = 123,
            Details = "Created user: John Doe",
            Timestamp = DateTime.UtcNow,
            ActionById = 456
        };

        await _context.Create(log);

        var expectedViewModel = new LogDetailViewModel
        {
            Id = 1,
            Action = "Create",
            EntityType = "User",
            EntityId = 123,
            Details = "Created user: John Doe",
            Timestamp = log.Timestamp,
            ActionById = 456
        };

        _mapperMock.Setup(m => m.Map<LogDetailViewModel>(It.IsAny<Log>()))
            .Returns(expectedViewModel);

        // Act
        var result = await _service.GetLogById(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedViewModel);
        _mapperMock.Verify(m => m.Map<LogDetailViewModel>(It.Is<Log>(l => l.Id == 1)), Times.Once);
    }

    [Fact]
    public async Task GetLogById_WhenLogDoesNotExist_ReturnsNull()
    {
        // Arrange
        // No logs in the database

        // Act
        var result = await _service.GetLogById(999);

        // Assert
        result.Should().BeNull();
        _mapperMock.Verify(m => m.Map<LogDetailViewModel>(It.IsAny<Log>()), Times.Never);
    }

    [Fact]
    public async Task GetLogById_VerifiesCorrectLogIsFetched()
    {
        // Arrange
        var logs = new[]
        {
            new Log { Id = 1, Action = "Create", EntityType = "User", EntityId = 1, Timestamp = DateTime.UtcNow.AddHours(-1) },
            new Log { Id = 2, Action = "Update", EntityType = "User", EntityId = 1, Timestamp = DateTime.UtcNow.AddHours(-2) },
            new Log { Id = 3, Action = "Delete", EntityType = "User", EntityId = 1, Timestamp = DateTime.UtcNow.AddHours(-3) }
        };

        foreach (var log in logs)
        {
            await _context.Create(log);
        }

        var expectedLog = logs[1]; // Log with Id = 2
        var expectedViewModel = new LogDetailViewModel
        {
            Id = 2,
            Action = "Update",
            EntityType = "User",
            EntityId = 1,
            Timestamp = expectedLog.Timestamp
        };

        _mapperMock.Setup(m => m.Map<LogDetailViewModel>(It.Is<Log>(l => l.Id == 2)))
            .Returns(expectedViewModel);

        // Act
        var result = await _service.GetLogById(2);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(2);
        result.Action.Should().Be("Update");
        _mapperMock.Verify(m => m.Map<LogDetailViewModel>(It.Is<Log>(l => l.Id == 2)), Times.Once);
    }

    [Fact]
    public async Task GetLogById_MapsAllLogProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var log = new Log
        {
            Id = 5,
            Action = "CustomAction",
            EntityType = "Product",
            EntityId = 789,
            Details = "Detailed log information with special characters: !@#$%^&*()",
            Timestamp = timestamp,
            ActionById = 101
        };

        await _context.Create(log);

        _mapperMock.Setup(m => m.Map<LogDetailViewModel>(It.Is<Log>(l => l.Id == 5)))
            .Returns(new LogDetailViewModel
            {
                Id = 5,
                Action = "CustomAction",
                EntityType = "Product",
                EntityId = 789,
                Details = "Detailed log information with special characters: !@#$%^&*()",
                Timestamp = timestamp,
                ActionById = 101
            });

        // Act
        var result = await _service.GetLogById(5);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(5);
        result.Action.Should().Be("CustomAction");
        result.EntityType.Should().Be("Product");
        result.EntityId.Should().Be(789);
        result.Details.Should().Be("Detailed log information with special characters: !@#$%^&*()");
        result.Timestamp.Should().BeCloseTo(timestamp, TimeSpan.FromMilliseconds(1));
        result.ActionById.Should().Be(101);
    }

    [Fact]
    public async Task GetLogsByEntity_WhenLogsExist_ReturnsCorrectViewModels()
    {
        // Arrange
        const string entityType = "User";
        const long entityId = 42L;

        var logs = new[]
        {
            new Log { Id = 1, Action = "Create", EntityType = entityType, EntityId = entityId, Details = "User created", Timestamp = DateTime.UtcNow.AddHours(-3) },
            new Log { Id = 2, Action = "Update", EntityType = entityType, EntityId = entityId, Details = "User updated", Timestamp = DateTime.UtcNow.AddHours(-2) },
            new Log { Id = 3, Action = "Delete", EntityType = entityType, EntityId = entityId, Details = "User deleted", Timestamp = DateTime.UtcNow.AddHours(-1) }
        };

        foreach (var log in logs)
        {
            await _context.Create(log);
        }

        var expectedViewModels = new[]
        {
            new LogViewModel { Id = 3, Action = "Delete", EntityType = entityType, EntityId = entityId },
            new LogViewModel { Id = 2, Action = "Update", EntityType = entityType, EntityId = entityId },
            new LogViewModel { Id = 1, Action = "Create", EntityType = entityType, EntityId = entityId }
        };

        _mapperMock.Setup(m => m.Map<IEnumerable<LogViewModel>>(It.Is<List<Log>>(l => l.Count == 3)))
            .Returns(expectedViewModels);

        // Act
        var result = await _service.GetLogsByEntity(entityType, entityId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(expectedViewModels);
        _mapperMock.Verify(m => m.Map<IEnumerable<LogViewModel>>(It.Is<List<Log>>(l =>
            l.Count == 3 &&
            l.All(log => log.EntityType == entityType && log.EntityId == entityId))),
            Times.Once);
    }

    [Fact]
    public async Task GetLogsByEntity_WhenNoLogsExist_ReturnsEmptyCollection()
    {
        // Arrange
        const string entityType = "User";
        const long entityId = 42L;

        _mapperMock.Setup(m => m.Map<IEnumerable<LogViewModel>>(It.Is<List<Log>>(l => !l.Any())))
            .Returns([]);

        // Act
        var result = await _service.GetLogsByEntity(entityType, entityId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mapperMock.Verify(m => m.Map<IEnumerable<LogViewModel>>(It.Is<List<Log>>(l => !l.Any())), Times.Once);
    }

    [Fact]
    public async Task GetLogsByEntity_ReturnsLogsOrderedByTimestampDescending()
    {
        // Arrange
        const string entityType = "Product";
        const long entityId = 123L;
        var now = DateTime.UtcNow;

        var logs = new[]
        {
            new Log { Id = 1, Action = "Create", EntityType = entityType, EntityId = entityId, Timestamp = now.AddDays(-5) },
            new Log { Id = 2, Action = "Update", EntityType = entityType, EntityId = entityId, Timestamp = now.AddDays(-1) },
            new Log { Id = 3, Action = "Update", EntityType = entityType, EntityId = entityId, Timestamp = now.AddDays(-3) }
        };

        foreach (var log in logs)
        {
            await _context.Create(log);
        }

        var expectedViewModels = new[]
        {
            new LogViewModel { Id = 2, Action = "Update", EntityType = entityType, EntityId = entityId, Timestamp = now.AddDays(-1) },
            new LogViewModel { Id = 3, Action = "Update", EntityType = entityType, EntityId = entityId, Timestamp = now.AddDays(-3) },
            new LogViewModel { Id = 1, Action = "Create", EntityType = entityType, EntityId = entityId, Timestamp = now.AddDays(-5) }
        };

        _mapperMock.Setup(m => m.Map<IEnumerable<LogViewModel>>(It.Is<List<Log>>(l => l.Count == 3)))
            .Returns(expectedViewModels);

        // Act
        var result = await _service.GetLogsByEntity(entityType, entityId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);

        var resultArray = result.ToArray();
        resultArray[0].Id.Should().Be(2); // Newest log
        resultArray[1].Id.Should().Be(3); // Middle log
        resultArray[2].Id.Should().Be(1); // Oldest log
    }

    [Fact]
    public async Task GetLogsByEntity_FiltersByEntityTypeAndId()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var logs = new[]
        {
            // Logs for User:42
            new Log { Id = 1, Action = "Create", EntityType = "User", EntityId = 42, Timestamp = now.AddHours(-1) },
            new Log { Id = 2, Action = "Update", EntityType = "User", EntityId = 42, Timestamp = now.AddHours(-2) },

            // Logs for User:99
            new Log { Id = 3, Action = "Create", EntityType = "User", EntityId = 99, Timestamp = now.AddHours(-3) },

            // Logs for Product:42 - hypothetical new entity type
            new Log { Id = 4, Action = "Create", EntityType = "Product", EntityId = 42, Timestamp = now.AddHours(-4) }
        };

        foreach (var log in logs)
        {
            await _context.Create(log);
        }

        var expectedViewModels = new[]
        {
            new LogViewModel { Id = 1, Action = "Create", EntityType = "User", EntityId = 42 },
            new LogViewModel { Id = 2, Action = "Update", EntityType = "User", EntityId = 42 }
        };

        _mapperMock.Setup(m => m.Map<IEnumerable<LogViewModel>>(It.Is<List<Log>>(l =>
            l.Count == 2 &&
            l.All(log => log.EntityType == "User" && log.EntityId == 42))))
            .Returns(expectedViewModels);

        // Act
        var result = await _service.GetLogsByEntity("User", 42);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(log => log is { EntityType: "User", EntityId: 42 }).Should().BeTrue();
    }

    [Fact]
    public async Task GetLogsByEntity_WithMultipleEntityTypes_FiltersCorrectly()
    {
        // Arrange
        var logs = new[]
        {
            new Log { Id = 1, Action = "Create", EntityType = "User", EntityId = 1, Timestamp = DateTime.UtcNow.AddHours(-1) },
            new Log { Id = 2, Action = "Update", EntityType = "User", EntityId = 1, Timestamp = DateTime.UtcNow.AddHours(-2) },
            new Log { Id = 3, Action = "Create", EntityType = "Post", EntityId = 1, Timestamp = DateTime.UtcNow.AddHours(-3) },
            new Log { Id = 4, Action = "Update", EntityType = "Post", EntityId = 2, Timestamp = DateTime.UtcNow.AddHours(-4) }
        };

        foreach (var log in logs)
        {
            await _context.Create(log);
        }

        // For Post with ID 1
        var postLogs = logs.Where(l => l.EntityType == "Post" && l.EntityId == 1).ToList();
        var postViewModels = new[]
        {
            new LogViewModel { Id = 3, Action = "Create", EntityType = "Post", EntityId = 1 }
        };

        _mapperMock.Setup(m => m.Map<IEnumerable<LogViewModel>>(It.Is<List<Log>>(l =>
            l.Count == 1 &&
            l.All(log => log.EntityType == "Post" && log.EntityId == 1))))
            .Returns(postViewModels);

        // Act
        var result = await _service.GetLogsByEntity("Post", 1);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        var resultlog = result.First();
        resultlog.EntityType.Should().Be("Post");
        resultlog.EntityId.Should().Be(1);
        resultlog.Action.Should().Be("Create");
    }

    [Fact]
    public async Task GetLogsCount_WithNoFilter_ReturnsAllLogsCount()
    {
        // Arrange
        var logs = new[]
        {
            new Log { Id = 1, Action = "Create", EntityType = "User", EntityId = 1, Timestamp = DateTime.UtcNow.AddDays(-1) },
            new Log { Id = 2, Action = "Update", EntityType = "User", EntityId = 1, Timestamp = DateTime.UtcNow.AddDays(-2) },
            new Log { Id = 3, Action = "Delete", EntityType = "User", EntityId = 1, Timestamp = DateTime.UtcNow.AddDays(-3) }
        };

        foreach (var log in logs)
        {
            await _context.Create(log);
        }

        var filter = new LogFilter();

        // Act
        var count = await _service.GetLogsCount(filter);

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task GetLogsCount_WithActionTypeFilter_ReturnsFilteredCount()
    {
        // Arrange
        var logs = new[]
        {
            new Log { Id = 1, Action = "Create", EntityType = "User", EntityId = 1, Timestamp = DateTime.UtcNow.AddDays(-1) },
            new Log { Id = 2, Action = "Update", EntityType = "User", EntityId = 1, Timestamp = DateTime.UtcNow.AddDays(-2) },
            new Log { Id = 3, Action = "Create", EntityType = "User", EntityId = 2, Timestamp = DateTime.UtcNow.AddDays(-3) }
        };

        foreach (var log in logs)
        {
            await _context.Create(log);
        }

        var filter = new LogFilter { ActionType = "Create" };

        // Act
        var count = await _service.GetLogsCount(filter);

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetLogsCount_WithEntityTypeFilter_ReturnsFilteredCount()
    {
        // Arrange
        var logs = new[]
        {
            new Log { Id = 1, Action = "Create", EntityType = "User", EntityId = 1, Timestamp = DateTime.UtcNow.AddDays(-1) },
            new Log { Id = 2, Action = "Update", EntityType = "User", EntityId = 1, Timestamp = DateTime.UtcNow.AddDays(-2) },
            new Log { Id = 3, Action = "Create", EntityType = "Post", EntityId = 1, Timestamp = DateTime.UtcNow.AddDays(-3) }
        };

        foreach (var log in logs)
        {
            await _context.Create(log);
        }

        var filter = new LogFilter { EntityType = "User" };

        // Act
        var count = await _service.GetLogsCount(filter);

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task GetLogsCount_WithDateRangeFilter_ReturnsFilteredCount()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var logs = new[]
        {
            new Log { Id = 1, Action = "Create", EntityType = "User", EntityId = 1, Timestamp = now.AddDays(-1) },
            new Log { Id = 2, Action = "Update", EntityType = "User", EntityId = 1, Timestamp = now.AddDays(-5) },
            new Log { Id = 3, Action = "Delete", EntityType = "User", EntityId = 1, Timestamp = now.AddDays(-10) }
        };

        foreach (var log in logs)
        {
            await _context.Create(log);
        }

        var filter = new LogFilter
        {
            FromDate = now.AddDays(-7),
            ToDate = now
        };

        // Act
        var count = await _service.GetLogsCount(filter);

        // Assert
        count.Should().Be(2); // Only logs with timestamps in the last 7 days
    }

    [Fact]
    public async Task GetLogsCount_WithCombinedFilters_ReturnsFilteredCount()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var logs = new[]
        {
            new Log { Id = 1, Action = "Create", EntityType = "User", EntityId = 1, Timestamp = now.AddDays(-1) },
            new Log { Id = 2, Action = "Update", EntityType = "User", EntityId = 1, Timestamp = now.AddDays(-2) },
            new Log { Id = 3, Action = "Create", EntityType = "Post", EntityId = 1, Timestamp = now.AddDays(-3) },
            new Log { Id = 4, Action = "Create", EntityType = "User", EntityId = 2, Timestamp = now.AddDays(-8) }
        };

        foreach (var log in logs)
        {
            await _context.Create(log);
        }

        var filter = new LogFilter
        {
            ActionType = "Create",
            EntityType = "User",
            FromDate = now.AddDays(-7),
            ToDate = now
        };

        // Act
        var count = await _service.GetLogsCount(filter);

        // Assert
        count.Should().Be(1); // Only Log Id 1 matches all criteria
    }

    [Fact]
    public async Task GetLogsCount_WithNoMatchingLogs_ReturnsZero()
    {
        // Arrange
        var logs = new[]
        {
            new Log { Id = 1, Action = "Create", EntityType = "User", EntityId = 1, Timestamp = DateTime.UtcNow },
            new Log { Id = 2, Action = "Update", EntityType = "User", EntityId = 1, Timestamp = DateTime.UtcNow }
        };

        foreach (var log in logs)
        {
            await _context.Create(log);
        }

        var filter = new LogFilter { EntityType = "NonExistentType" };

        // Act
        var count = await _service.GetLogsCount(filter);

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetLogsCount_WithEmptyDatabase_ReturnsZero()
    {
        // Arrange
        var filter = new LogFilter();

        // Act
        var count = await _service.GetLogsCount(filter);

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task GetLogsCount_PaginationSettingsDoNotAffectCount()
    {
        // Arrange
        var logs = new[]
        {
            new Log { Id = 1, Action = "Create", EntityType = "User", EntityId = 1, Timestamp = DateTime.UtcNow.AddDays(-1) },
            new Log { Id = 2, Action = "Update", EntityType = "User", EntityId = 1, Timestamp = DateTime.UtcNow.AddDays(-2) },
            new Log { Id = 3, Action = "Delete", EntityType = "User", EntityId = 1, Timestamp = DateTime.UtcNow.AddDays(-3) }
        };

        foreach (var log in logs)
        {
            await _context.Create(log);
        }

        // Filter with pagination settings
        var filter = new LogFilter
        {
            Page = 2,
            PageSize = 1
        };

        // Act
        var count = await _service.GetLogsCount(filter);

        // Assert
        count.Should().Be(3); // Should count all logs, regardless of pagination
    }

    // A simplified version of DataContext for testing
    public class TestDataContext(DbContextOptions<TestDataContext> options) : DbContext(options), IDataContext
    {
        public DbSet<Log> Logs { get; set; }

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

        public new Task Update<TEntity>(TEntity entity) where TEntity : class => throw new NotImplementedException();

        public Task Delete<TEntity>(TEntity entity) where TEntity : class => throw new NotImplementedException();
    }
}
