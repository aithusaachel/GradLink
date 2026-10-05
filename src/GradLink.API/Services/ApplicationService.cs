using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using GradLink.API.Data;
using GradLink.Shared.DTOs;
using GradLink.Shared.Enums;

namespace GradLink.API.Services;

public class ApplicationService
{
    private readonly GradLinkDbContext _context;
    private readonly NotificationService _notifications;

    public ApplicationService(GradLinkDbContext context, NotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    public async Task<List<ApplicationDto>> GetGraduateApplicationsAsync(string graduateId)
    {
        return await _context.JobApplications
            .Where(a => a.GraduateId == graduateId)
            .OrderByDescending(a => a.AppliedDate)
            .Select(ToDto)
            .ToListAsync();
    }

    public async Task<List<ApplicationDto>> GetJobApplicantsAsync(int jobId, string employerId)
    {
        return await _context.JobApplications
            .Where(a => a.JobListingId == jobId && a.JobListing.EmployerId == employerId)
            .OrderByDescending(a => a.AppliedDate)
            .Select(ToDto)
            .ToListAsync();
    }

    /// <summary>Returns null when the job doesn't exist or is closed, or the graduate has already applied.</summary>
    public async Task<ApplicationDto?> ApplyAsync(string graduateId, CreateApplicationDto dto)
    {
        // Check if already applied
        var existing = await _context.JobApplications
            .AnyAsync(a => a.JobListingId == dto.JobListingId && a.GraduateId == graduateId);

        if (existing) return null;

        var job = await _context.JobListings.FindAsync(dto.JobListingId);
        if (job == null || !job.IsActive || job.Deadline?.Date < DateTime.UtcNow.Date) return null;

        var application = new JobApplication
        {
            JobListingId = dto.JobListingId,
            GraduateId = graduateId,
            CoverLetter = dto.CoverLetter,
            Status = ApplicationStatus.Pending
        };

        _context.JobApplications.Add(application);

        // Notify employer
        var graduate = await _context.Users.FindAsync(graduateId);
        var notification = _notifications.Add(job.EmployerId,
            NotificationMessages.NewApplication(graduate?.FullName ?? "a graduate", job.Title),
            NotificationType.ApplicationUpdate);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // A concurrent duplicate can pass the existing-application check; the unique index rejects it here.
            return null;
        }

        // Send real-time notification
        await _notifications.PublishAsync(notification);

        return await GetByIdAsync(application.Id);
    }

    public async Task<ApplicationDto?> UpdateStatusAsync(string employerId, UpdateApplicationStatusDto dto)
    {
        var application = await _context.JobApplications
            .Include(a => a.JobListing)
            .FirstOrDefaultAsync(a => a.Id == dto.ApplicationId && a.JobListing.EmployerId == employerId);

        if (application == null) return null;
        if (application.Status == dto.NewStatus) return await GetByIdAsync(application.Id);

        application.Status = dto.NewStatus;

        // Notify graduate
        var notification = _notifications.Add(application.GraduateId,
            NotificationMessages.StatusChanged(application.JobListing.Title, dto.NewStatus),
            NotificationType.ApplicationUpdate);

        await _context.SaveChangesAsync();

        // Send real-time notification
        await _notifications.PublishAsync(notification);

        return await GetByIdAsync(application.Id);
    }

    public async Task<DashboardStatsDto> GetGraduateStatsAsync(string graduateId)
    {
        var apps = await _context.JobApplications
            .Where(a => a.GraduateId == graduateId)
            .ToListAsync();

        return new DashboardStatsDto
        {
            TotalApplications = apps.Count,
            PendingApplications = apps.Count(a => a.Status == ApplicationStatus.Pending),
            ShortlistedApplications = apps.Count(a => a.Status == ApplicationStatus.Shortlisted),
            RejectedApplications = apps.Count(a => a.Status == ApplicationStatus.Rejected),
            AcceptedApplications = apps.Count(a => a.Status == ApplicationStatus.Accepted)
        };
    }

    public async Task<DashboardStatsDto> GetEmployerStatsAsync(string employerId)
    {
        var jobs = _context.JobListings.Where(j => j.EmployerId == employerId);
        var applications = _context.JobApplications.Where(a => a.JobListing.EmployerId == employerId);
        var today = DateTime.UtcNow.Date;

        return new DashboardStatsDto
        {
            TotalJobsPosted = await jobs.CountAsync(),
            ActiveJobs = await jobs.CountAsync(j => j.IsActive),
            TotalApplicants = await applications.CountAsync(),
            NewApplicantsToday = await applications.CountAsync(a => a.AppliedDate >= today)
        };
    }

    private Task<ApplicationDto> GetByIdAsync(int id) =>
        _context.JobApplications.Where(a => a.Id == id).Select(ToDto).SingleAsync();

    private static readonly Expression<Func<JobApplication, ApplicationDto>> ToDto = a => new ApplicationDto
    {
        Id = a.Id,
        JobListingId = a.JobListingId,
        JobTitle = a.JobListing.Title,
        CompanyName = a.JobListing.Employer.CompanyName ?? "",
        GraduateId = a.GraduateId,
        GraduateName = a.Graduate.FullName,
        GraduateEmail = a.Graduate.Email,
        GraduateUniversity = a.Graduate.University,
        GraduateDegree = a.Graduate.Degree,
        Status = a.Status,
        AppliedDate = a.AppliedDate,
        CoverLetter = a.CoverLetter,
        HasCv = !string.IsNullOrEmpty(a.Graduate.CvFilePath)
    };
}
