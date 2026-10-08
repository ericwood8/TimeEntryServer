using Microsoft.AspNetCore.Diagnostics;

namespace TimeEntry.ApiService.Apis;

/// <summary>
/// A body that is not valid JSON (or is missing) is the caller's mistake: answer 400 with a problem document, not 500.
/// Every other exception is left to the default handler, which answers 500 and logs it.
/// </summary>
public sealed class ApiExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException bad)
            return false;

        httpContext.Response.StatusCode = bad.StatusCode;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = bad.StatusCode, Title = "Invalid request", Detail = "The request body is missing or is not valid JSON for this endpoint." },
        });
    }
}
