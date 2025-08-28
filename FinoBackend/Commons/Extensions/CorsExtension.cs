using Microsoft.Extensions.DependencyInjection;

namespace FinoBackend.Common.Extensions;

public static class CorsPolicies
{
    public const string Default = "_defaultCors";
}

public static class CorsExtension
{
    public static IServiceCollection AddAppCors(this IServiceCollection services)
    {
        var origins = new[]
        {
            "http://localhost:3000",
            "https://www.finotools.app"
        };

        services.AddCors(opt =>
        {
            opt.AddPolicy(CorsPolicies.Default, builder =>
            {
                builder.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}