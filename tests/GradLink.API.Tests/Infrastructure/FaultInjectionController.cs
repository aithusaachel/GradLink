using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GradLink.API.Tests.Infrastructure;

[ApiController]
[Route("test/faults")]
public sealed class FaultInjectionController : ControllerBase
{
    [HttpGet("unhandled")]
    public IActionResult Unhandled() => throw new InvalidOperationException("Simulated failure for error-handling tests.");

    [HttpGet("unique-violation")]
    public IActionResult UniqueViolation() =>
        throw new DbUpdateException("Simulated duplicate row.", new SqliteException("UNIQUE constraint failed", 19, 2067));
}
