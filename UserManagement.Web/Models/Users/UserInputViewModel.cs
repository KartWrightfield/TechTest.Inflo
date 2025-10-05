using System;
using System.ComponentModel.DataAnnotations;

namespace UserManagement.Web.Models.Users;

public class UserInputViewModel
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Forename is required")]
    [StringLength(50, ErrorMessage = "Forename cannot exceed 50 characters")]
    public string? Forename { get; set; }

    [Required(ErrorMessage = "Surname is required")]
    [StringLength(50, ErrorMessage = "Surname cannot exceed 50 characters")]
    public string? Surname { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Date of Birth is required")]
    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; }
}
