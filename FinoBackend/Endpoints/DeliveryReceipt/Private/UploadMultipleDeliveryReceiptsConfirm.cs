using System.ComponentModel.DataAnnotations;
using FastEndpoints;
using FinoBackend.Common;
using FinoBackend.Commons.Enums;
using FinoBackend.Endpoints.BankStatementFile;
using FinoBackend.Services;
namespace FinoBackend.Endpoints.DeliveryReceipt.Private;

public class UploadMultipleDeliveryReceiptsConfirm 
    : Endpoint<UploadMultipleDeliveryReceiptsConfirmRequest, UploadMultipleDeliveryReceiptsConfirmResponse>
{
    private readonly StorageService _storage;
    private readonly UploadedFileService _uploadedFileService;
    private readonly ConversionJobService _conversionJobService;
    private readonly MessageQueueService _messageQueueService;
    private readonly ILogger<UploadMultipleDeliveryReceiptsConfirm> _logger;

    public UploadMultipleDeliveryReceiptsConfirm(
        ConversionJobService conversionJobService, 
        StorageService storage, 
        UploadedFileService uploadedFileService,
        MessageQueueService messageQueueService,
        ILogger<UploadMultipleDeliveryReceiptsConfirm> logger)
    {
        _conversionJobService = conversionJobService;
        _storage = storage;
        _uploadedFileService = uploadedFileService;
        _messageQueueService = messageQueueService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/private/delivery-receipts/confirm-multiple");
        Roles("authenticated");
    }

    public override async Task HandleAsync(UploadMultipleDeliveryReceiptsConfirmRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Confirming batch upload for DeliveryReceipts");

        var sub = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(sub, out var authUserId) || authUserId != req.UserId)
            throw new UnauthorizedException();

        if (req.Files is null || req.Files.Count == 0)
            throw new BadRequestException("No files provided.");

        // Validate uploaded files exist in S3
        var keys = req.Files.Select(f => f.FileKey).ToList();
        var (allValid, results) = await _storage.ValidateMultipleFilesAsync(keys, ct);
        if (!allValid)
        {
            _logger.LogWarning("Batch validation failed for DeliveryReceipts. Details: {@results}", results);
            throw new BadRequestException("One or more files are missing or exceed the size limit.");
        }

        var createdJobs = new List<Models.ConversionJob>();

        foreach (var spec in req.Files)
        {
            var fileExt = FileExtensionHelper.Parse(spec.FileExtension);

            // Insert UploadedFile row
            var file = await _uploadedFileService.CreateUploadedFileAsync(
                userId: req.UserId,
                fileKey: spec.FileKey,
                fileCategory: FileCategory.DeliveryReceipt,
                fileExtension: fileExt,
                ct: ct,
                originalFileName: spec.FileName,
                bankStatementFileId: spec.FileId); // ← consider renaming param to just `uploadedFileId`

            var jobId = Guid.NewGuid();
            var message = new ConversionJobMessage(jobId, file.Id, file.UserId);

            var job = await _conversionJobService.CreateAsync(file.Id, jobId, ct);
            var queueUrl = await _messageQueueService.EnqueueAsync(
                queueType: QueueType.DeliveryReceiptConversion,message: message, ct);

            _logger.LogInformation("Enqueued delivery receipt {FileId}: {@message} to {QueueUrl}", file.Id, message, queueUrl);
            createdJobs.Add(job);
        }

        var response = new UploadMultipleDeliveryReceiptsConfirmResponse(createdJobs);
        await Send.OkAsync(response, ct);
    }
}

// --- Request/Response Models ---

public record UploadMultipleDeliveryReceiptsConfirmRequest(
    [Required] Guid UserId,
    [Required] List<FileConfirmSpec> Files
);

public record UploadMultipleDeliveryReceiptsConfirmResponse(
    List<Models.ConversionJob> Jobs
);
