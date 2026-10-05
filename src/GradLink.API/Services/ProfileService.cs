using Microsoft.EntityFrameworkCore;
using GradLink.API.Data;
using GradLink.Shared.DTOs;

namespace GradLink.API.Services;

public class ProfileService
{
    private readonly GradLinkDbContext _context;

    public ProfileService(GradLinkDbContext context)
    {
        _context = context;
    }

    public async Task<GraduateProfileDto?> GetGraduateProfileAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        return new GraduateProfileDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? "",
            University = user.University,
            Degree = user.Degree,
            GraduationYear = user.GraduationYear,
            Skills = user.Skills,
            Bio = user.Bio,
            HasCv = !string.IsNullOrEmpty(user.CvFilePath),
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<GraduateProfileDto?> UpdateGraduateProfileAsync(string userId, UpdateGraduateProfileDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        if (dto.FullName != null) user.FullName = dto.FullName;
        if (dto.University != null) user.University = dto.University;
        if (dto.Degree != null) user.Degree = dto.Degree;
        if (dto.GraduationYear.HasValue) user.GraduationYear = dto.GraduationYear;
        if (dto.Skills != null) user.Skills = dto.Skills;
        if (dto.Bio != null) user.Bio = dto.Bio;

        await _context.SaveChangesAsync();
        return await GetGraduateProfileAsync(userId);
    }

    public async Task<EmployerProfileDto?> GetEmployerProfileAsync(string userId)
    {
        return await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new EmployerProfileDto
            {
                UserId = u.Id,
                CompanyName = u.CompanyName ?? "",
                Email = u.Email ?? "",
                Industry = u.Industry,
                Description = u.CompanyDescription,
                Website = u.Website,
                CompanySize = u.CompanySize,
                JobCount = u.JobListings.Count,
                CreatedAt = u.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<EmployerProfileDto?> UpdateEmployerProfileAsync(string userId, UpdateEmployerProfileDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        if (dto.CompanyName != null) user.CompanyName = dto.CompanyName;
        if (dto.Industry != null) user.Industry = dto.Industry;
        if (dto.Description != null) user.CompanyDescription = dto.Description;
        if (dto.Website != null) user.Website = dto.Website;
        if (dto.CompanySize != null) user.CompanySize = dto.CompanySize;

        await _context.SaveChangesAsync();
        return await GetEmployerProfileAsync(userId);
    }
}
