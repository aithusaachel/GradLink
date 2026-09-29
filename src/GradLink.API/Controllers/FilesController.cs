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
    private const long MaxCvBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> CvContentTypes = new()
    {
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private readonly GradLinkDbContext _context;
    private readonly IWebHostEnvironment _env;

    public FilesController(GradLinkDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    private string CvDirectory => Path.Combine(_env.ContentRootPath, "Uploads", "CVs");

    // Only the stored file name is used, so a value can never point outside CvDirectory.
    // Older rows stored an absolute path; those still resolve to the same file.
    private string? ResolveCvPath(string? stored) =>
        string.IsNullOrEmpty(stored) ? null : Path.Combine(CvDirectory, Path.GetFileName(stored));

    [HttpPost("cv")]
    [Authorize(Roles = "Graduate")]
    public async Task<IActionResult> UploadCv(IFormFile file)
    {
        if (file.Length == 0)
            return BadRequest("No file uploaded.");

        if (file.Length > MaxCvBytes)
            return BadRequest("File size must be less than 5MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!CvContentTypes.ContainsKey(extension))
            return BadRequest("Only PDF and Word documents are allowed.");

        var user = await _context.Users.FindAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
        if (user == null)
            return Unauthorized();

        Directory.CreateDirectory(CvDirectory);
        var fileName = $"{user.Id}_{Guid.NewGuid():N}{extension}";
        await using (var stream = System.IO.File.Create(Path.Combine(CvDirectory, fileName)))
        {
            await file.CopyToAsync(stream);
        }

        // Only remove the old CV once the database points at the new one.
        var previousPath = ResolveCvPath(user.CvFilePath);
        user.CvFilePath = fileName;
        await _context.SaveChangesAsync();

        if (previousPath != null)
            System.IO.File.Delete(previousPath);

        return Ok(new { message = "CV uploaded successfully.", fileName });
    }

    [HttpGet("cv/{userId}")]
    public async Task<IActionResult> DownloadCv(string userId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Graduates can fetch their own CV; employers only those of graduates who applied to them.
        if (currentUserId != userId)
        {
            var isApplicantsEmployer = User.IsInRole("Employer") && await _context.JobApplications
                .AnyAsync(a => a.GraduateId == userId && a.JobListing.EmployerId == currentUserId);

            if (!isApplicantsEmployer)
                return Forbid();
        }

        var user = await _context.Users.FindAsync(userId);
        var path = ResolveCvPath(user?.CvFilePath);
        if (user == null || path == null || !System.IO.File.Exists(path))
            return NotFound("CV not found.");

        var extension = Path.GetExtension(path);
        var contentType = CvContentTypes.GetValueOrDefault(extension, "application/octet-stream");
        return PhysicalFile(path, contentType, $"{user.FullName}_CV{extension}");
    }
}
