using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GradLink.API.Data;
using GradLink.Shared.Enums;

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

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest("File size must be less than 5MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
        if (!allowedExtensions.Contains(extension))
            return BadRequest("Only PDF and Word documents are allowed.");

        if (!IsLikelyValidCv(file.OpenReadStream(), extension))
            return BadRequest("The uploaded file is not a valid CV document.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var uploadsDir = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "Uploads", "CVs"));
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream);
        }

        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            if (!string.IsNullOrEmpty(user.CvFilePath))
            {
                var oldFullPath = Path.GetFullPath(user.CvFilePath);
                if (oldFullPath.StartsWith(uploadsDir, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(oldFullPath))
                {
                    System.IO.File.Delete(oldFullPath);
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

        var isEmployer = User.IsInRole(UserRole.Employer.ToString()) || string.Equals(User.FindFirstValue(ClaimTypes.Role), UserRole.Employer.ToString(), StringComparison.OrdinalIgnoreCase);
        var isOwner = string.Equals(currentUserId, userId, StringComparison.Ordinal);

        if (!isOwner)
        {
            if (!isEmployer)
                return Forbid();

            var hasApplied = await _context.JobApplications
                .AnyAsync(a => a.GraduateId == userId && a.JobListing.EmployerId == currentUserId);

            if (!hasApplied)
                return Forbid();
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

    private static bool IsLikelyValidCv(Stream stream, string extension)
    {
        try
        {
            stream.Position = 0;
            var header = new byte[8];
            var bytesRead = stream.Read(header, 0, header.Length);
            if (bytesRead < 4)
                return false;

            if (extension == ".pdf")
                return header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46;

            if (extension == ".doc")
                return header[0] == 0xD0 && header[1] == 0xCF && header[2] == 0x11 && header[3] == 0xE0;

            if (extension == ".docx")
            {
                stream.Position = 0;
                var zipHeader = new byte[4];
                stream.Read(zipHeader, 0, zipHeader.Length);
                return zipHeader[0] == 0x50 && zipHeader[1] == 0x4B && zipHeader[2] == 0x03 && zipHeader[3] == 0x04;
            }

            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            stream.Position = 0;
        }
    }
}
