using System;
using System.Threading.Tasks;
using UserManagement.Services.Interfaces;
using UserManagement.Shared.Filters;
using UserManagement.Shared.Models.Logs;

namespace UserManagement.Web.Controllers;

public class LogsController(ILoggingService loggingService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(LogFilter? filter)
    {
        // If no filter is provided, create a default one
        filter ??= new LogFilter();

        var logs = await loggingService.GetAllLogs(filter);

        var viewModel = new LogListViewModel
        {
            Logs = logs,
            Filter = filter,
            TotalPages = await GetTotalPagesAsync(filter)
        };

        return View(viewModel);
    }

    [HttpGet("view")]
    public async Task<IActionResult> View(long id)
    {
        var log = await loggingService.GetLogById(id);

        if (log == null)
            return RedirectToAction("Index");

        return View(log);
    }

    private async Task<int> GetTotalPagesAsync(LogFilter filter)
    {
        var totalCount = await loggingService.GetLogsCount(filter);
        return (int)Math.Ceiling(totalCount / (double)filter.PageSize);
    }
}
