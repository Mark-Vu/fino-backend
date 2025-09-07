using System.ComponentModel.DataAnnotations;
using FastEndpoints;
using FinoBackend.Common;
using FinoBackend.Services;
using FinoBackend.Models;

namespace FinoBackend.Endpoints.ConversionJob;

public class GetMultipleJobStatuses
    : Endpoint<GetMultipleJobStatusesRequest, GetMultipleJobStatusesResponse>
{
    private readonly ILogger<GetMultipleJobStatuses> _logger;
    private readonly ConversionJobService _conversionJobService;

    public GetMultipleJobStatuses(
        ILogger<GetMultipleJobStatuses> logger, 
        ConversionJobService conversionJobService)
    {
        _logger = logger;
        _conversionJobService = conversionJobService;
    }

    public override void Configure()
    {
        Post("/conversion-jobs/statuses");
        AllowAnonymous(); // or Roles("authenticated") if you want to restrict
    }

    public override async Task HandleAsync(GetMultipleJobStatusesRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Starting GetMultipleJobStatuses {@req}", req);

        if (req.JobIds is null || req.JobIds.Count == 0)
            throw new BadRequestException("No JobIds provided");

        var jobs = await _conversionJobService.GetJobsByIdsAsync(req.JobIds, ct);

        if (jobs == null || jobs.Count == 0)
            throw new NotFoundException();

        var response = new GetMultipleJobStatusesResponse(
            jobs.Select(j => new JobStatusDto(
                j.Id,
                j.Status,
                j.ErrorMessage,
                j.FinishedAt
            )).ToList()
        );

        _logger.LogInformation("Ending GetMultipleJobStatuses {@req}", req);
        await Send.OkAsync(response, ct);
    }
}

// --- Request / Response Models ---

public record GetMultipleJobStatusesRequest(
    [Required] List<Guid> JobIds
);

public record GetMultipleJobStatusesResponse(
    List<JobStatusDto> Jobs
);

public record JobStatusDto(
    Guid JobId,
    JobStatus Status,
    string? ErrorMessage,
    DateTime? FinishedAt
);
