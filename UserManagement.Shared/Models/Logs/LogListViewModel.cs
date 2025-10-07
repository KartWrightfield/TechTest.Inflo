using UserManagement.Shared.Filters;

namespace UserManagement.Shared.Models.Logs;

public class LogListViewModel
{
    public IEnumerable<LogViewModel> Logs { get; set; } = new List<LogViewModel>();
    public LogFilter Filter { get; set; } = new LogFilter();
    public int TotalPages { get; set; }
    public int CurrentPage => Filter.Page;
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}
