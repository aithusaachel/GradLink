using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using GradLink.API.Data;
using GradLink.API.Hubs;
using GradLink.Shared.DTOs;
using GradLink.Shared.Enums;

namespace GradLink.API.Services;

public class ApplicationService
{
    private readonly GradLinkDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;

    public ApplicationService(GradLinkDbContext context, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<List<ApplicationDto>> GetGraduateApplicationsAsync(string graduateId)
    {
        return await _context.JobApplications
            .Include(a => a.JobListing).ThenInclude(j => j.Employer)
            .Include(a => a.Graduate)
            .Where(a => a.GraduateId == graduateId)
            .OrderByDescending(a => a.AppliedDate)
            .Select(a => MapToDto(a))
            .ToListAsync();
    }

    public async Task<List<ApplicationDto>> GetJobApplicantsAsync(int jobId, string employerId)
    {
        return await _context.JobApplications
            .Include(a => a.JobListing)
            .Include(a => a.Graduate)
            .Where(a => a.JobListingId == jobId && a.JobListing.EmployerId == employerId)
            .OrderByDescending(a => a.AppliedDate)
            .Select(a => MapToDto(a))
            .ToListAsync();
    }

    public async Task<ApplicationDto?> ApplyAsync(string graduateId, CreateApplicationDto dto)
    {
        // Check if already applied
        var existing = await _context.JobApplications
            .AnyAsync(a => a.JobListingId == dto.JobListingId && a.GraduateId == graduateId);

        if (existing) return null;

        var job = await _context.JobListings.Include(j => j.Employer).FirstOrDefaultAsync(j => j.Id == dto.JobListingId);
        if (job == null) return null;

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
        var notification = new Notification
        {
            UserId = job.EmployerId,
            Message = $"New application from {graduate?.FullName ?? "a graduate"} for \"{job.Title}\"",
            Type = NotificationType.ApplicationUpdate
        };
        _context.Notifications.Add(notification);

        await _context.SaveChangesAsync();

        // Send real-time notification
        await _hubContext.Clients.Group(job.EmployerId)
            .SendAsync("ReceiveNotification", new NotificationDto
            {
                Id = notification.Id,
                Message = notification.Message,
                IsRead = false,
                CreatedAt = notification.CreatedAt,
                Type = notification.Type
            });

        application.JobListing = job;
        application.Graduate = graduate!;
        return MapToDto(application);
    }

    public async Task<ApplicationDto?> UpdateStatusAsync(string employerId, UpdateApplicationStatusDto dto)
    {
        var application = await _context.JobApplications
            .Include(a => a.JobListing).ThenInclude(j => j.Employer)
            .Include(a => a.Graduate)
            .FirstOrDefaultAsync(a => a.Id == dto.ApplicationId && a.JobListing.EmployerId == employerId);

        if (application == null) return null;

        application.Status = dto.NewStatus;

        // Notify graduate
        var notification = new Notification
        {
            UserId = application.GraduateId,
            Message = $"Your application for \"{application.JobListing.Title}\" has been updated to: {dto.NewStatus}",
            Type = NotificationType.ApplicationUpdate
        };
        _context.Notifications.Add(notification);

        await _context.SaveChangesAsync();

        // Send real-time notification
        await _hubContext.Clients.Group(application.GraduateId)
            .SendAsync("ReceiveNotification", new NotificationDto
            {
                Id = notification.Id,
                Message = notification.Message,
                IsRead = false,
                CreatedAt = notification.CreatedAt,
                Type = notification.Type
            });

        return MapToDto(application);
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
        var jobs = await _context.JobListings
            .Include(j => j.Applications)
            .Where(j => j.EmployerId == employerId)
            .ToListAsync();

        var allApps = jobs.SelectMany(j => j.Applications).ToList();

        return new DashboardStatsDto
        {
            TotalJobsPosted = jobs.Count,
            ActiveJobs = jobs.Count(j => j.IsActive),
            TotalApplicants = allApps.Count,
            NewApplicantsToday = allApps.Count(a => a.AppliedDate.Date == DateTime.UtcNow.Date)
        };
    }

    private static ApplicationDto MapToDto(JobApplication a) => new()
    {
        Id = a.Id,
        JobListingId = a.JobListingId,
        JobTitle = a.JobListing?.Title ?? "",
        CompanyName = a.JobListing?.Employer?.CompanyName ?? "",
        GraduateId = a.GraduateId,
        GraduateName = a.Graduate?.FullName ?? "",
        GraduateEmail = a.Graduate?.Email,
        GraduateUniversity = a.Graduate?.University,
        GraduateDegree = a.Graduate?.Degree,
        Status = a.Status,
        AppliedDate = a.AppliedDate,
        CoverLetter = a.CoverLetter,
        HasCv = !string.IsNullOrEmpty(a.Graduate?.CvFilePath)
    };
}
