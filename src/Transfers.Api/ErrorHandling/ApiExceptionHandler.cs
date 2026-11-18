using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Transfers.Application.Common;
using Transfers.Domain.Common;

namespace Transfers.Api.ErrorHandling
{
    public sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
        {
            var problem = exception switch
            {
                DomainException domain => new ProblemDetails
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "Business rule violated",
                    Detail = domain.Message,
                    Extensions = { ["reason"] = domain.Reason.ToString() }
                },
                NotFoundException notFound => new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource not found",
                    Detail = notFound.Message
                },
                ConcurrencyConflictException conflict => new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Concurrent update",
                    Detail = conflict.Message
                },
                _ => null
            };

            if (problem is null)
            {
                return false;
            }

            httpContext.Response.StatusCode = problem.Status!.Value;

            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem,
                Exception = exception
            });
        }
    }
}
