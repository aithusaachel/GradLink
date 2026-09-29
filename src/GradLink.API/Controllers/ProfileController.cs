using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GradLink.API.Services;
using GradLink.Shared.DTOs;

namespace GradLink.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly ProfileService _profileService;
    private readonly ApplicationService _applicationService;

    public ProfileController(ProfileService profileService, ApplicationService applicationService)
    {
        _profileService = profileService;
        _applicationService = applicationService;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var stats = User.IsInRole("Employer")
            ? await _applicationService.GetEmployerStatsAsync(userId)
            : await _applicationService.GetGraduateStatsAsync(userId);
        return Ok(stats);
    }

    [Authorize(Roles = "Graduate")]
    [HttpGet("graduate")]
    public async Task<IActionResult> GetGraduateProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await _profileService.GetGraduateProfileAsync(userId);
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    [Authorize(Roles = "Graduate")]
    [HttpPut("graduate")]
    public async Task<IActionResult> UpdateGraduateProfile([FromBody] UpdateGraduateProfileDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await _profileService.UpdateGraduateProfileAsync(userId, dto);
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    [Authorize(Roles = "Employer")]
    [HttpGet("employer")]
    public async Task<IActionResult> GetEmployerProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await _profileService.GetEmployerProfileAsync(userId);
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    [Authorize(Roles = "Employer")]
    [HttpPut("employer")]
    public async Task<IActionResult> UpdateEmployerProfile([FromBody] UpdateEmployerProfileDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await _profileService.UpdateEmployerProfileAsync(userId, dto);
        if (profile == null) return NotFound();
        return Ok(profile);
    }
}
