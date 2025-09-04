using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FastEndpoints;
using FinoBackend.Common;
using FinoBackend.Services;

namespace FinoBackend.Endpoints.BankStatementFile;

public class UploadMultipleBankStatementsConfirm 
    : Endpoint<UploadMultipleBankStatementsConfirmRequest, UploadMultipleBankStatementsConfirmResponse>
{
    private readonly StorageService _storage;
    private readonly BankStatementService _bankStatementService;
    private readonly ConversionJobService _conversionJobService;
    private readonly MessageQueueService _messageQueueService;
    private readonly ILogger<UploadMultipleBankStatementsConfirm> _logger;

    public UploadMultipleBankStatementsConfirm(
        ConversionJobService conversionJobService, 
        StorageService storage, 
        BankStatementService bankStatementService,
        MessageQueueService messageQueueService,
        ILogger<UploadMultipleBankStatementsConfirm> logger)
    {
        _conversionJobService = conversionJobService;
        _storage = storage;
        _bankStatementService = bankStatementService;
        _messageQueueService = messageQueueService;
        _logger = logger;
    }

    public override void Configure()
    {
        // Batch confirm (no per-id in the route)
        Post("/private/bank-statement-files/confirm-multiple");
        Roles("authenticated");
    }

    public override async Task HandleAsync(UploadMultipleBankStatementsConfirmRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Confirming batch upload for MultipleBankStatements");
        _logger.LogInformation("Batch upload for MultipleBankStatements");
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var authUserId) || authUserId != req.UserId)
            throw new UnauthorizedException();

        if (req.FileIds is null || req.FileIds.Count == 0)
            throw new BadRequestException("No FileIds provided.");

        // Build S3 keys from the provided FileIds
        var keys = req.FileIds
            .Select(fileId => _storage.GetPrivatePdfUploadKey(authUserId, fileId))
            .ToList();

        // Validate all files in S3 (exists + size <= MAX)
        var (allValid, results) = await _storage.ValidateMultipleFilesAsync(keys, ct);
        if (!allValid)
        {
            _logger.LogWarning("Batch validation failed for MultipleBankStatements. Details: {@results}", results);
            throw new BadRequestException("One or more files are missing or exceed the size limit.");
        }

        var createdJobs = new List<Models.ConversionJob>();

        // Create DB rows + jobs and enqueue each
        for (int i = 0; i < req.FileIds.Count; i++)
        {
            var fileId = req.FileIds[i];
            var originalFileName = req.FileNames[i];
            var key = keys[i];

            // Insert/confirm BankStatementFile row
            var file = await _bankStatementService.CreateBankStatementFileAsync(
                userId: req.UserId,
                pdfFileKey: key,
                ct: ct,
                OriginalFileName: originalFileName,
                bankStatementFileId: fileId);

            // Create Job (explicit jobId to keep things deterministic if you like)
            var jobId = Guid.NewGuid();

            // Compute CSV result key if needed by the job/queue
            var csvKey = _storage.GetPrivateCsvResultKey(authUserId, jobId);

            // Enqueue message to start conversion
            var message = new ConversionJobMessage(jobId, file.Id, file.UserId);
            
            var job = await _conversionJobService.CreateAsync(
                file.Id,
                jobId,
                ct);
            var queueUrl = await _messageQueueService.EnqueueJobAsync(message,isPublic: false, ct);

            _logger.LogInformation("Enqueued file {FileId}: {@message} to {QueueUrl}", file.Id, message, queueUrl);
            createdJobs.Add(job);
        }

        var response = new UploadMultipleBankStatementsConfirmResponse(createdJobs);
        await Send.OkAsync(response, ct);
    }
}

public record UploadMultipleBankStatementsConfirmRequest(
    [Required] Guid UserId,
    [Required] List<Guid> FileIds,
    [Required] List<string> FileNames
);

public record UploadMultipleBankStatementsConfirmResponse(
    List<Models.ConversionJob> Jobs
);
