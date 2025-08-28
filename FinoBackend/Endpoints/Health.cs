using FastEndpoints;

public class MyEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/health-check");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync("Hello World!");
    }
}