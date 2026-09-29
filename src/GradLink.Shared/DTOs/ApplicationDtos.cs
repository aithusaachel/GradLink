using System.ComponentModel.DataAnnotations;
using GradLink.Shared.Enums;

namespace GradLink.Shared.DTOs;

public class ApplicationDto
{
    public int Id { get; set; }
    public int JobListingId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string GraduateId { get; set; } = string.Empty;
    public string GraduateName { get; set; } = string.Empty;
    public string? GraduateEmail { get; set; }
    public string? GraduateUniversity { get; set; }
    public string? GraduateDegree { get; set; }
    public ApplicationStatus Status { get; set; }
    public DateTime AppliedDate { get; set; }
    public string? CoverLetter { get; set; }
    public bool HasCv { get; set; }
}

public class CreateApplicationDto
{
    public int JobListingId { get; set; }
    public string? CoverLetter { get; set; }
}

public class UpdateApplicationStatusDto
{
    public int ApplicationId { get; set; }

    [EnumDataType(typeof(ApplicationStatus))]
    public ApplicationStatus NewStatus { get; set; }
}
