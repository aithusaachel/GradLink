using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GradLink.API.Data;

namespace GradLink.API.Infrastructure;

public class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            BadHttpRequestException badRequest => (badRequest.StatusCode, "The request could not be processed."),
            DbUpdateException dbUpdate when dbUpdate.IsUniqueConstraintViolation() =>
                (StatusCodes.Status409Conflict, "The request conflicts with data that already exists."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogWarning(exception, "{Method} {Path} failed with {StatusCode}.", httpContext.Request.Method, httpContext.Request.Path, status);

        httpContext.Response.StatusCode = status;

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = null,
            Type = $"https://httpstatuses.com/{status}"
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });
    }
}
