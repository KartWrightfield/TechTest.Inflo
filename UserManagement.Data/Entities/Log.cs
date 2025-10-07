using System;

namespace UserManagement.Data.Entities;

public class Log
{
    public long Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public long EntityId { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Related user who performed the action (in case I have time to have a go at task 5.3)
    public int? ActionById { get; set; }
}
