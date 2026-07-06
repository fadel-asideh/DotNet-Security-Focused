
using DotNetSecurityFocused.Models;
using FluentValidation;

namespace DotNetSecurityFocused.Validators;

public class RegisterRequestValidator: AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("ConfirmPassword must match Password");
        
         RuleFor(x => x.Roles)
            .NotEmpty()
            .WithMessage("At least one role is required");
    }
}