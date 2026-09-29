using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using GradLink.API.Data;
using GradLink.Shared.DTOs;
using GradLink.Shared.Enums;

namespace GradLink.API.Services;

public class JobService
{
    private readonly GradLinkDbContext _context;

    public JobService(GradLinkDbContext context)
    {
        _context = context;
    }

    public async Task<List<JobListingDto>> GetJobsAsync(
        string? search = null,
        string? industry = null,
        string? location = null,
        ExperienceLevel? experienceLevel = null)
    {
        var query = _context.JobListings.Where(j => j.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(j =>
                j.Title.ToLower().Contains(lower) ||
                j.Description.ToLower().Contains(lower) ||
                j.Employer.CompanyName!.ToLower().Contains(lower));
        }

        if (!string.IsNullOrWhiteSpace(industry))
            query = query.Where(j => j.Industry.ToLower() == industry.ToLower());

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(j => j.Location.ToLower().Contains(location.ToLower()));

        if (experienceLevel.HasValue)
            query = query.Where(j => j.ExperienceLevel == experienceLevel.Value);

        return await query
            .OrderByDescending(j => j.PostedDate)
            .Select(ToDto)
            .ToListAsync();
    }

    public async Task<JobListingDto?> GetJobByIdAsync(int id)
    {
        return await _context.JobListings
            .Where(j => j.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync();
    }

    public async Task<List<JobListingDto>> GetEmployerJobsAsync(string employerId)
    {
        return await _context.JobListings
            .Where(j => j.EmployerId == employerId)
            .OrderByDescending(j => j.PostedDate)
            .Select(ToDto)
            .ToListAsync();
    }

    public async Task<JobListingDto> CreateJobAsync(string employerId, CreateJobDto dto)
    {
        var job = new JobListing { EmployerId = employerId };
        CopyFields(dto, job);

        _context.JobListings.Add(job);
        await _context.SaveChangesAsync();

        return (await GetJobByIdAsync(job.Id))!;
    }

    public async Task<JobListingDto?> UpdateJobAsync(int id, string employerId, CreateJobDto dto)
    {
        var job = await _context.JobListings
            .FirstOrDefaultAsync(j => j.Id == id && j.EmployerId == employerId);

        if (job == null) return null;

        CopyFields(dto, job);
        await _context.SaveChangesAsync();

        return await GetJobByIdAsync(job.Id);
    }

    public async Task<bool> DeleteJobAsync(int id, string employerId)
    {
        var job = await _context.JobListings
            .FirstOrDefaultAsync(j => j.Id == id && j.EmployerId == employerId);

        if (job == null) return false;

        _context.JobListings.Remove(job);
        await _context.SaveChangesAsync();
        return true;
    }

    private static void CopyFields(CreateJobDto dto, JobListing job)
    {
        job.Title = dto.Title;
        job.Description = dto.Description;
        job.Location = dto.Location;
        job.Industry = dto.Industry;
        job.ExperienceLevel = dto.ExperienceLevel;
        job.SalaryRange = dto.SalaryRange;
        job.Deadline = dto.Deadline;
    }

    private static readonly Expression<Func<JobListing, JobListingDto>> ToDto = j => new JobListingDto
    {
        Id = j.Id,
        Title = j.Title,
        Description = j.Description,
        Location = j.Location,
        Industry = j.Industry,
        ExperienceLevel = j.ExperienceLevel,
        SalaryRange = j.SalaryRange,
        PostedDate = j.PostedDate,
        Deadline = j.Deadline,
        CompanyName = j.Employer.CompanyName ?? "Unknown",
        EmployerId = j.EmployerId,
        ApplicationCount = j.Applications.Count,
        IsActive = j.IsActive
    };
}
