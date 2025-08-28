using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FinoBackend.Common.Extensions;

public static class ExceptionHandlerExtension
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(appErr => appErr.Run(async context =>
        {
            var feature = context.Features.Get<IExceptionHandlerFeature>();
            var ex = feature?.Error;

            var problem = ex switch
            {
                ForbiddenException fex => New(403, "Forbidden", fex.Message),
                NotFoundException nex  => New(404, "Not Found", nex.Message),
                BadRequestException bex=> New(400, "Bad Request", bex.Message),
                UnauthorizedException _=> New(401, "Unauthorized", ex.Message),
                _ => New(500, "Internal Server Error", "An unexpected error occurred.")
            };

            problem.Extensions["traceId"] = context.TraceIdentifier;
            context.Response.StatusCode  = problem.Status ?? 500;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem);
        }));

        return app;
    }

    private static ProblemDetails New(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail, Type = $"https://finotools.app/errors/{title.Replace(" ", "").ToLower()}" };
}