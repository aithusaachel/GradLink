using Microsoft.AspNetCore.Identity;
using GradLink.Shared.Enums;

namespace GradLink.API.Data;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Graduate profile fields
    public string? University { get; set; }
    public string? Degree { get; set; }
    public int? GraduationYear { get; set; }
    public string? Skills { get; set; }
    public string? Bio { get; set; }
    public string? CvFilePath { get; set; }

    // Employer profile fields
    public string? CompanyName { get; set; }
    public string? Industry { get; set; }
    public string? CompanyDescription { get; set; }
    public string? Website { get; set; }
    public string? CompanySize { get; set; }

    // Navigation properties
    public ICollection<JobListing> JobListings { get; set; } = new List<JobListing>();
    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

public class JobListing
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public ExperienceLevel ExperienceLevel { get; set; }
    public string? SalaryRange { get; set; }
    public DateTime PostedDate { get; set; } = DateTime.UtcNow;
    public DateTime? Deadline { get; set; }
    public bool IsActive { get; set; } = true;

    // Foreign key
    public string EmployerId { get; set; } = string.Empty;
    public ApplicationUser Employer { get; set; } = null!;

    // Navigation
    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
}

public class JobApplication
{
    public int Id { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public DateTime AppliedDate { get; set; } = DateTime.UtcNow;
    public string? CoverLetter { get; set; }

    // Foreign keys
    public int JobListingId { get; set; }
    public JobListing JobListing { get; set; } = null!;

    public string GraduateId { get; set; } = string.Empty;
    public ApplicationUser Graduate { get; set; } = null!;
}

public class Notification
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public NotificationType Type { get; set; }

    // Foreign key
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
}
