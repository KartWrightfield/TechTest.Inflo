namespace UserManagement.Shared.Filters;

public class UserFilter
{
    public bool? ActiveStatus { get; set; }

    public static UserFilter FromString(string filter)
    {
        return filter.ToLower() switch
        {
            "active" => new UserFilter { ActiveStatus = true },
            "inactive" => new UserFilter { ActiveStatus = false },
            _ => new UserFilter()
        };
    }
}
