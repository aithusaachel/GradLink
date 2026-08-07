namespace GradLink.Shared.DTOs;

public class DashboardStatsDto
{
    // Graduate stats
    public int TotalApplications { get; set; }
    public int PendingApplications { get; set; }
    public int ShortlistedApplications { get; set; }
    public int RejectedApplications { get; set; }
    public int AcceptedApplications { get; set; }

    // Employer stats
    public int TotalJobsPosted { get; set; }
    public int ActiveJobs { get; set; }
    public int TotalApplicants { get; set; }
    public int NewApplicantsToday { get; set; }
}
