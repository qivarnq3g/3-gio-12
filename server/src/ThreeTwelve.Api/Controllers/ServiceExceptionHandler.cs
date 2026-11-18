using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ThreeTwelve.Api.Services;

namespace ThreeTwelve.Api.Controllers;

public sealed class ServiceExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Không tìm thấy"),
            ConflictException => (StatusCodes.Status409Conflict, "Trái với trạng thái hiện tại"),
            _ => (0, ""),
        };
        if (status == 0)
            return false;

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = exception.Message },
        });
    }
}
