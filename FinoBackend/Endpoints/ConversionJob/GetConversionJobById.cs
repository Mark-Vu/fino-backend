using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FastEndpoints;
using FinoBackend.Common;
using FinoBackend.Services;

namespace FinoBackend.Endpoints.ConversionJob;

public class GetConversionJobById
    : Endpoint<GetConversionJobByIdRequest>
{
    private readonly ILogger<GetConversionJobById> _logger;
    private readonly ConversionJobService _conversionJobService;

    public GetConversionJobById(ILogger<GetConversionJobById> logger, ConversionJobService conversionJobService)
    {
        _logger = logger;
        _conversionJobService = conversionJobService;
    }

    public override void Configure()
    {
        Get("/conversion-jobs/{JobId}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetConversionJobByIdRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Starting GetConversionJobById {@req}", req);
        var job = await _conversionJobService.GetJobByIdAsync(req.JobId, ct);
        if (job == null)
        {
            throw new NotFoundException();
        }
        _logger.LogInformation("Ending GetConversionJobById {@req}", req);
        await Send.OkAsync(job, ct);
    }
}

public record GetConversionJobByIdRequest([Required] Guid JobId);
