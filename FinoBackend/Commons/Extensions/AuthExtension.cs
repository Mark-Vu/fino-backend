using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FinoBackend.Common.Extensions;

public static class AuthExtension
{
    public static IServiceCollection AddSupabaseAuth(this IServiceCollection services, IConfiguration config)
    {
        var projectId = config["SupabaseProjectId"]
                        ?? throw new InvalidOperationException("SupabaseProjectId missing from config");

        var issuer  = $"https://{projectId}.supabase.co/auth/v1";
        var jwksUri = $"{issuer}/.well-known/jwks.json";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = "authenticated",
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
                    {
                        using var http = new HttpClient();
                        var jwks = http.GetStringAsync(jwksUri).Result;
                        var keys = new JsonWebKeySet(jwks);
                        return keys.Keys.Where(k => string.IsNullOrEmpty(kid) || k.Kid == kid);
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
