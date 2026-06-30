using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FastEndpoints;
using FinoBackend.Services;

namespace FinoBackend.Endpoints.User;

using Microsoft.Extensions.Logging;

public class GetUserById : Endpoint<GetUserByIdRequest, GetUserByIdResponse>
{
    private readonly UserService _userService;
    private readonly ILogger<GetUserById> _logger;

    public GetUserById(UserService userService, ILogger<GetUserById> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/users/{id}");
        Roles("authenticated");
    }

    public override async Task HandleAsync(GetUserByIdRequest req, CancellationToken ct)
    {
        _logger.LogInformation("GetUserById request started {req}", req);
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.Parse(sub) != req.Id)
        {
            await Send.ForbiddenAsync(ct); // 403 Forbidden
            return;
        }
        
        var user = await _userService.GetUserByIdAsync(req.Id);

        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        _logger.LogInformation("GetUserById request finished {req}", req);

        await Send.OkAsync(new GetUserByIdResponse(
            Id: user.Id,
            Name: user.Name,
            Email: user.Email
        ), ct);
    }
}



public record GetUserByIdRequest(
    [Required] Guid Id
);

public record GetUserByIdResponse(
    Guid Id,
    string Name,
    string Email
);