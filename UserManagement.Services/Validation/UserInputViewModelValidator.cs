using System;
using FluentValidation;
using UserManagement.Shared.Models.Users;

namespace UserManagement.Services.Validation;

public class UserInputViewModelValidator : AbstractValidator<UserInputViewModel>
{
    public UserInputViewModelValidator()
    {
        RuleFor(x => x.Forename).NotEmpty().WithMessage("Forename is required");
        RuleFor(x => x.Surname).NotEmpty().WithMessage("Surname is required");
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
        RuleFor(x => x.DateOfBirth).NotNull().WithMessage("Date of birth is required")
            .Must(BeAValidBirthDate).WithMessage("Date of birth cannot be in the future");
    }

    private static bool BeAValidBirthDate(DateOnly? date)
    {
        return date.HasValue && date.Value <= DateOnly.FromDateTime(DateTime.Today);
    }
}
