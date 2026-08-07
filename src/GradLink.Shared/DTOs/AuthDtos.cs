using System.ComponentModel.DataAnnotations;
using GradLink.Shared.Enums;

namespace GradLink.Shared.DTOs;

public class RegisterDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    // Graduate-specific
    public string? University { get; set; }
    public string? Degree { get; set; }
    public int? GraduationYear { get; set; }

    // Employer-specific
    public string? CompanyName { get; set; }
    public string? Industry { get; set; }
}

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public bool Success { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
