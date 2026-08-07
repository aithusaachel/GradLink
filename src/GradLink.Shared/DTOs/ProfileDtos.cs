namespace GradLink.Shared.DTOs;

public class GraduateProfileDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? University { get; set; }
    public string? Degree { get; set; }
    public int? GraduationYear { get; set; }
    public string? Skills { get; set; }
    public string? Bio { get; set; }
    public bool HasCv { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateGraduateProfileDto
{
    public string? FullName { get; set; }
    public string? University { get; set; }
    public string? Degree { get; set; }
    public int? GraduationYear { get; set; }
    public string? Skills { get; set; }
    public string? Bio { get; set; }
}

public class EmployerProfileDto
{
    public string UserId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public int JobCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateEmployerProfileDto
{
    public string? CompanyName { get; set; }
    public string? Industry { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
}
