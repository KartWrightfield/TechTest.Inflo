namespace UserManagement.Shared.Models.Logs;

public class LogViewModel
{
    public long Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public long EntityId { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int ActionById { get; set; }
    public string? ActionByUser { get; set; }
}

public class LogDetailViewModel : LogViewModel
{
    public string FormattedTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
}
