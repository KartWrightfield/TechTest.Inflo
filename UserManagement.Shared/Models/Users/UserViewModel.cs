namespace UserManagement.Shared.Models.Users;

public class UserViewModel
{
    public long Id { get; set; }
    public string? Forename { get; set; }
    public string? Surname { get; set; }
    public string? Email { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public bool IsActive { get; set; }

    public string FullName => $"{Forename} {Surname}";
}
