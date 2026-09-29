using System.ComponentModel.DataAnnotations;
using GradLink.Shared.Enums;

namespace GradLink.Shared.DTOs;

public class JobListingDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public ExperienceLevel ExperienceLevel { get; set; }
    public string? SalaryRange { get; set; }
    public DateTime PostedDate { get; set; }
    public DateTime? Deadline { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string EmployerId { get; set; } = string.Empty;
    public int ApplicationCount { get; set; }
    public bool IsActive { get; set; }
}

public class CreateJobDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Location { get; set; } = string.Empty;

    [Required]
    public string Industry { get; set; } = string.Empty;

    [EnumDataType(typeof(ExperienceLevel))]
    public ExperienceLevel ExperienceLevel { get; set; }

    public string? SalaryRange { get; set; }

    public DateTime? Deadline { get; set; }
}
