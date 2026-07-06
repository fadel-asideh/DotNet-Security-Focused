using System.ComponentModel.DataAnnotations;

namespace DotNetSecurityFocused.Models;

public class RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;

    [MinLength(1, ErrorMessage ="At least one role is required")]
    public string[] Roles { get; set; } = ["User"];
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;

}