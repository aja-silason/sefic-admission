using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Partners.Domain.Common;
using Partners.Domain.Partners;

namespace Partners.Api.Errors;

/// <summary>
/// Maps every unhandled exception to an RFC 9457 problem response. Messages are meant for the client;
/// stack traces and internal details are only logged.
/// </summary>
internal sealed class DomainExceptionHandler(IProblemDetailsService problemDetails, ILogger<DomainExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            DomainValidationException ex => new ValidationProblemDetails(ex.Errors.ToDictionary())
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Detail = "See the errors property for details."
            },
            PartnerNotFoundException ex => Problem(StatusCodes.Status404NotFound, "Partner not found.", ex.Message),
            DuplicateTaxNumberException ex => Problem(StatusCodes.Status409Conflict, "Duplicate tax number.", ex.Message),
            PartnerConcurrencyException ex => Problem(StatusCodes.Status409Conflict, "Concurrency conflict.", ex.Message),
            BadHttpRequestException ex => Problem(ex.StatusCode, "Bad request.", "The request could not be read."),
            _ => null
        };

        if (problem is null)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
            problem = Problem(StatusCodes.Status500InternalServerError, "An unexpected error occurred.",
                "The request could not be completed. Please try again later.");
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails Problem(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };
}
