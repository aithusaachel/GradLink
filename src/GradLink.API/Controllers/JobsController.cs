using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GradLink.API.Services;
using GradLink.Shared.DTOs;
using GradLink.Shared.Enums;

namespace GradLink.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly JobService _jobService;

    public JobsController(JobService jobService)
    {
        _jobService = jobService;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobs(
        [FromQuery] string? search,
        [FromQuery] string? industry,
        [FromQuery] string? location,
        [FromQuery] ExperienceLevel? experienceLevel)
    {
        var jobs = await _jobService.GetJobsAsync(search, industry, location, experienceLevel);
        return Ok(jobs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetJob(int id)
    {
        var job = await _jobService.GetJobByIdAsync(id);
        if (job == null) return NotFound();
        return Ok(job);
    }

    [Authorize]
    [HttpGet("employer")]
    public async Task<IActionResult> GetEmployerJobs()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var jobs = await _jobService.GetEmployerJobsAsync(userId);
        return Ok(jobs);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var job = await _jobService.CreateJobAsync(userId, dto);
        return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateJob(int id, [FromBody] CreateJobDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var job = await _jobService.UpdateJobAsync(id, userId, dto);
        if (job == null) return NotFound();
        return Ok(job);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJob(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _jobService.DeleteJobAsync(id, userId);
        if (!result) return NotFound();
        return NoContent();
    }
}
