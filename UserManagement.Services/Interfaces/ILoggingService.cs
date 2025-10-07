using System.Collections.Generic;
using System.Threading.Tasks;
using UserManagement.Shared.Filters;
using UserManagement.Shared.Models.Logs;

namespace UserManagement.Services.Interfaces;

public interface ILoggingService
{
    Task LogAction(string action, string entityType, long entityId, string details, int? actionById = null);
    Task<IEnumerable<LogViewModel>> GetAllLogs(LogFilter filter);
    Task<LogDetailViewModel?> GetLogById(long id);
    Task<IEnumerable<LogViewModel>> GetLogsByEntity(string entityType, long entityId);
    Task<int> GetLogsCount(LogFilter filter);
}
