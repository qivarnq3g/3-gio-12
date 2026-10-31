using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ThreeTwelve.Api.Controllers;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RejectLargeBodyAttribute(long maxBytes) : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        if (context.HttpContext.Request.ContentLength > maxBytes)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status413PayloadTooLarge,
                Title = "Lô quá lớn",
                Detail = $"Tối đa {maxBytes} byte; chia lô nhỏ hơn rồi gửi lại.",
            })
            {
                StatusCode = StatusCodes.Status413PayloadTooLarge,
                ContentTypes = { "application/problem+json" },
            };
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
