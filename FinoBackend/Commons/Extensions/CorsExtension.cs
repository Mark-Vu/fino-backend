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
        services.AddCors(opt =>
        {
            opt.AddPolicy(CorsPolicies.Default, builder =>
            {
                builder
                    .SetIsOriginAllowed(origin =>
                    {
                        if (string.IsNullOrEmpty(origin))
                            return false;

                        try
                        {
                            var uri = new Uri(origin);
                            var host = uri.Host;
                            var port = uri.Port;

                            // Allow localhost:3000 and any subdomain of localhost:3000
                            if (port == 3000 && (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                                || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)))
                            {
                                return true;
                            }

                            // Allow root domain
                            if (host.Equals("finotools.app", StringComparison.OrdinalIgnoreCase))
                                return true;

                            // Allow any subdomain like abc.finotools.app
                            if (host.EndsWith(".finotools.app", StringComparison.OrdinalIgnoreCase))
                                return true;
                        }
                        catch
                        {
                            return false; // invalid origin format
                        }

                        return false;
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
