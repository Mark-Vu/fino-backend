using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FinoBackend.Common.Extensions;

public static class AuthExtension
{
    public static IServiceCollection AddSupabaseAuth(
        this IServiceCollection services,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        var projectId = config["SupabaseProjectId"]
                        ?? throw new InvalidOperationException("SupabaseProjectId missing from config");

        var issuer  = $"https://{projectId}.supabase.co/auth/v1";
        var jwksUri = $"{issuer}/.well-known/jwks.json";
        var jwksHandler = new HttpClientHandler();

        if (env.IsDevelopment())
        {
            jwksHandler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        var jwksHttpClient = new HttpClient(jwksHandler);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = "authenticated",
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    NameClaimType = "sub",
                    RoleClaimType = "role",

                    IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
                    {
                        var jwks = jwksHttpClient.GetStringAsync(jwksUri).GetAwaiter().GetResult();
                        var keys = new JsonWebKeySet(jwks);
                        return keys.Keys.Where(k => string.IsNullOrEmpty(kid) || k.Kid == kid);
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
