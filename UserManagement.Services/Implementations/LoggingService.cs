using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Data.Entities;
using UserManagement.Services.Interfaces;
using UserManagement.Shared.Filters;
using UserManagement.Shared.Models.Logs;

namespace UserManagement.Services.Implementations;

public class LoggingService(IDataContext dataContext, IMapper mapper) : ILoggingService
{
    public async Task LogAction(string action, string entityType, long entityId, string details, int? actionById = null)
    {
        var log = new Log
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            Timestamp = DateTime.UtcNow,
            ActionById = actionById
        };

        await dataContext.Create(log);
    }

    public async Task<IEnumerable<LogViewModel>> GetAllLogs(LogFilter filter)
    {
        var query = dataContext.Logs.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.ActionType))
            query = query.Where(l => l.Action == filter.ActionType);

        if (!string.IsNullOrEmpty(filter.EntityType))
            query = query.Where(l => l.EntityType == filter.EntityType);

        if (filter.FromDate.HasValue)
            query = query.Where(l => l.Timestamp >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(l => l.Timestamp <= filter.ToDate.Value);

        // Order by the newest first
        query = query.OrderByDescending(l => l.Timestamp);

        // Pagination
        query = query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize);

        var logs = await query.ToListAsync();
        var viewModels = mapper.Map<IEnumerable<LogViewModel>>(logs);

        // Enrich with user info for logs that have ActionById (in case I have time to have a go at task 5.3)
        /*foreach (var log in viewModels.Where(l => l.ActionByUser == null))
        {
            if (log is LogViewModel vm && vm.ActionByUser == null)
            {
                var user = await _dataContext.GetById<User>(log.ActionById);
                if (user != null)
                {
                    vm.ActionByUser = $"{user.Forename} {user.Surname}";
                }
            }
        }*/

        return viewModels;
    }

    public async Task<LogDetailViewModel?> GetLogById(long id)
    {
        var log = await dataContext.GetById<Log>(id);
        if (log == null)
            return null;

        var viewModel = mapper.Map<LogDetailViewModel>(log);

        // Enrich with user info if ActionById is present (in case I have time to have a go at task 5.3)
        /*if (log.ActionById.HasValue)
        {
            var user = await _dataContext.GetById<User>((long)log.ActionById);
            if (user != null)
            {
                viewModel.ActionByUser = $"{user.Forename} {user.Surname}";
            }
        }*/

        return viewModel;
    }

    public async Task<IEnumerable<LogViewModel>> GetLogsByEntity(string entityType, long entityId)
    {
        var logs = await dataContext.Logs
            .Where(l => l.EntityType == entityType && l.EntityId == entityId)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync();

        return mapper.Map<IEnumerable<LogViewModel>>(logs);
    }

    public async Task<int> GetLogsCount(LogFilter filter)
    {
        var query = dataContext.Logs.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.ActionType))
            query = query.Where(l => l.Action == filter.ActionType);

        if (!string.IsNullOrEmpty(filter.EntityType))
            query = query.Where(l => l.EntityType == filter.EntityType);

        if (filter.FromDate.HasValue)
            query = query.Where(l => l.Timestamp >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(l => l.Timestamp <= filter.ToDate.Value);

        return await query.CountAsync();
    }
}
