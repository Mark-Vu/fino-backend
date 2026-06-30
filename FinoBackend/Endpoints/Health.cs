using FastEndpoints;
namespace FinoBackend.Endpoints.Health;
public class HealthCheck : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/health-check");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync("Hello World!", ct);
    }
}