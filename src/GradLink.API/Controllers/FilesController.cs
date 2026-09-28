using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GradLink.API.Data;

namespace GradLink.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly GradLinkDbContext _context;
    private readonly IWebHostEnvironment _env;

    public FilesController(GradLinkDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpPost("cv")]
    [Authorize(Roles = "Graduate")]
    public async Task<IActionResult> UploadCv(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (file.Length > 5 * 1024 * 1024) // 5MB limit
            return BadRequest("File size must be less than 5MB.");

        var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
        var extension = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExtensions.Contains(extension))
            return BadRequest("Only PDF and Word documents are allowed.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var uploadsDir = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "Uploads", "CVs"));
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Update user record
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            // Delete old CV if exists
            if (!string.IsNullOrEmpty(user.CvFilePath) && System.IO.File.Exists(user.CvFilePath))
            {
                var oldFullPath = Path.GetFullPath(user.CvFilePath);
                if (oldFullPath.StartsWith(uploadsDir, StringComparison.OrdinalIgnoreCase))
                {
                    System.IO.File.Delete(user.CvFilePath);
                }
            }
            user.CvFilePath = filePath;
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "CV uploaded successfully.", fileName });
    }

    [HttpGet("cv/{userId}")]
    public async Task<IActionResult> DownloadCv(string userId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        var isEmployer = User.IsInRole("Employer") || User.FindFirstValue(ClaimTypes.Role) == "Employer";
        var isOwner = string.Equals(currentUserId, userId, StringComparison.Ordinal);

        if (!isOwner)
        {
            if (!isEmployer)
            {
                return Forbid();
            }

            var hasApplied = await _context.JobApplications
                .AnyAsync(a => a.GraduateId == userId && a.JobListing.EmployerId == currentUserId);
            
            if (!hasApplied)
            {
                return Forbid();
            }
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.CvFilePath))
            return NotFound("CV not found.");

        var uploadsDir = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "Uploads", "CVs"));
        var fullCvPath = Path.GetFullPath(user.CvFilePath);

        if (!fullCvPath.StartsWith(uploadsDir, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(fullCvPath))
            return NotFound("CV file not found or inaccessible.");

        var fileBytes = await System.IO.File.ReadAllBytesAsync(fullCvPath);
        var extension = Path.GetExtension(fullCvPath);
        var contentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };

        return File(fileBytes, contentType, $"{user.FullName}_CV{extension}");
    }
}
