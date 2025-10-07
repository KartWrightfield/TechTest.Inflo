using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using UserManagement.Services.Interfaces;
using UserManagement.Shared.Filters;
using UserManagement.Shared.Models.Logs;
using UserManagement.Web.Controllers;

namespace UserManagement.Web.Tests;

public class LogsControllerTests
{
    private readonly Mock<ILoggingService> _loggingService = new();

    [Fact]
    public async Task Index_WithNoFilter_CreatesDefaultFilterAndReturnsView()
    {
        // Arrange
        var controller = CreateController();
        var logs = new List<LogViewModel> { new() { Id = 1 } };
        var totalPages = 2;

        _loggingService.Setup(s => s.GetAllLogs(It.IsAny<LogFilter>()))
            .ReturnsAsync(logs);

        _loggingService.Setup(s => s.GetLogsCount(It.IsAny<LogFilter>()))
            .ReturnsAsync(10);

        // Act
        var result = await controller.Index(null);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var viewModel = viewResult.Model.Should().BeOfType<LogListViewModel>().Subject;

        viewModel.Logs.Should().BeEquivalentTo(logs);
        viewModel.Filter.Should().NotBeNull();
        viewModel.TotalPages.Should().Be(totalPages);

        _loggingService.Verify(s => s.GetAllLogs(It.IsAny<LogFilter>()), Times.Once);
        _loggingService.Verify(s => s.GetLogsCount(It.IsAny<LogFilter>()), Times.Once);
    }

    [Fact]
    public async Task Index_WithFilter_UsesProvidedFilterAndReturnsView()
    {
        // Arrange
        var controller = CreateController();
        var filter = new LogFilter { PageSize = 5, Page = 2 };
        var logs = new List<LogViewModel> { new() { Id = 5 } };
        var totalPages = 3;

        _loggingService.Setup(s => s.GetAllLogs(filter))
            .ReturnsAsync(logs);

        _loggingService.Setup(s => s.GetLogsCount(filter))
            .ReturnsAsync(11); // 11 logs with page size of 5 = 3 pages

        // Act
        var result = await controller.Index(filter);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var viewModel = viewResult.Model.Should().BeOfType<LogListViewModel>().Subject;

        viewModel.Logs.Should().BeEquivalentTo(logs);
        viewModel.Filter.Should().Be(filter);
        viewModel.TotalPages.Should().Be(totalPages);

        _loggingService.Verify(s => s.GetAllLogs(filter), Times.Once);
        _loggingService.Verify(s => s.GetLogsCount(filter), Times.Once);
    }

    [Fact]
    public async Task View_WhenLogExists_ReturnsViewWithLogViewModel()
    {
        // Arrange
        var controller = CreateController();
        var logViewModel = new LogDetailViewModel
        {
            Id = 1,
            Timestamp = System.DateTime.UtcNow
        };

        _loggingService.Setup(s => s.GetLogById(1))
            .ReturnsAsync(logViewModel);

        // Act
        var result = await controller.View(1);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.Model.Should().BeEquivalentTo(logViewModel);

        _loggingService.Verify(s => s.GetLogById(1), Times.Once);
    }

    [Fact]
    public async Task View_WhenLogDoesNotExist_RedirectsToIndex()
    {
        // Arrange
        var controller = CreateController();

        _loggingService.Setup(s => s.GetLogById(1))
            .ReturnsAsync((LogDetailViewModel)null!);

        // Act
        var result = await controller.View(1);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be(nameof(LogsController.Index));

        _loggingService.Verify(s => s.GetLogById(1), Times.Once);
    }

    private LogsController CreateController()
    {
        var controller = new LogsController(_loggingService.Object);
        controller.TempData = new TempDataDictionary(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());
        return controller;
    }
}
