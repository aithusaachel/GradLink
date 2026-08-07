using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GradLink.API.Services;
using GradLink.Shared.DTOs;

namespace GradLink.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly ApplicationService _applicationService;

    public ApplicationsController(ApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyApplications()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var apps = await _applicationService.GetGraduateApplicationsAsync(userId);
        return Ok(apps);
    }

    [HttpGet("job/{jobId}")]
    public async Task<IActionResult> GetJobApplicants(int jobId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var apps = await _applicationService.GetJobApplicantsAsync(jobId, userId);
        return Ok(apps);
    }

    [HttpPost]
    public async Task<IActionResult> Apply([FromBody] CreateApplicationDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _applicationService.ApplyAsync(userId, dto);
        if (result == null) return BadRequest("Unable to apply. You may have already applied to this job.");
        return Ok(result);
    }

    [HttpPut("status")]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateApplicationStatusDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _applicationService.UpdateStatusAsync(userId, dto);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("stats/graduate")]
    public async Task<IActionResult> GetGraduateStats()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var stats = await _applicationService.GetGraduateStatsAsync(userId);
        return Ok(stats);
    }

    [HttpGet("stats/employer")]
    public async Task<IActionResult> GetEmployerStats()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var stats = await _applicationService.GetEmployerStatsAsync(userId);
        return Ok(stats);
    }
}
