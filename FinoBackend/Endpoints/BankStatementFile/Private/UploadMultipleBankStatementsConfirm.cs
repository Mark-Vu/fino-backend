using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FastEndpoints;
using FinoBackend.Common;
using FinoBackend.Services;
using FinoBackend.Models;

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
        Post("/private/bank-statement-files/confirm-multiple");
        Roles("authenticated");
    }

    public override async Task HandleAsync(UploadMultipleBankStatementsConfirmRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Confirming batch upload for MultipleBankStatements");

        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var authUserId) || authUserId != req.UserId)
            throw new UnauthorizedException();

        if (req.Files is null || req.Files.Count == 0)
            throw new BadRequestException("No files provided.");

        // Collect all file keys for batch validation
        var keys = req.Files.Select(f => f.FileKey).ToList();
        var (allValid, results) = await _storage.ValidateMultipleFilesAsync(keys, ct);
        if (!allValid)
        {
            _logger.LogWarning("Batch validation failed for MultipleBankStatements. Details: {@results}", results);
            throw new BadRequestException("One or more files are missing or exceed the size limit.");
        }

        var createdJobs = new List<Models.ConversionJob>();

        foreach (var spec in req.Files)
        {
            var fileExt = FileExtensionHelper.Parse(spec.FileExtension);

            // Insert/confirm BankStatementFile row
            var file = await _bankStatementService.CreateBankStatementFileAsync(
                userId: req.UserId,
                fileKey: spec.FileKey,
                fileExtension: fileExt,
                ct: ct,
                OriginalFileName: spec.FileName,
                bankStatementFileId: spec.FileId);

            var jobId = Guid.NewGuid();
            var csvKey = _storage.GetPrivateCsvResultKey(req.UserId, jobId);

            var message = new ConversionJobMessage(jobId, file.Id, file.UserId);

            var job = await _conversionJobService.CreateAsync(file.Id, jobId, ct);
            var queueUrl = await _messageQueueService.EnqueueJobAsync(message, isPublic: false, ct);

            _logger.LogInformation("Enqueued file {FileId}: {@message} to {QueueUrl}", file.Id, message, queueUrl);
            createdJobs.Add(job);
        }

        var response = new UploadMultipleBankStatementsConfirmResponse(createdJobs);
        await Send.OkAsync(response, ct);
    }
}

// --- Request/Response Models ---

public record UploadMultipleBankStatementsConfirmRequest(
    [Required] Guid UserId,
    [Required] List<FileConfirmSpec> Files
);

public record FileConfirmSpec(
    [Required] Guid FileId,
    [Required] string FileName,
    [Required] string FileKey,
    [Required] string FileExtension
);

public record UploadMultipleBankStatementsConfirmResponse(
    List<Models.ConversionJob> Jobs
);
