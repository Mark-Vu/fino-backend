using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FastEndpoints;
using FinoBackend.Common;
using FinoBackend.Commons.Enums;
using FinoBackend.Services;

namespace FinoBackend.Endpoints.DeliveryReceipt.Private;

public class UploadMultipleDeliveryReceipts
    : Endpoint<UploadMultipleDeliveryReceiptsRequest, UploadMultipleDeliveryReceiptsResponse>
{
    private readonly ILogger<UploadMultipleDeliveryReceipts> _logger;
    private readonly StorageService _storage;

    public UploadMultipleDeliveryReceipts(ILogger<UploadMultipleDeliveryReceipts> logger, StorageService storage)
    {
        _logger = logger;
        _storage = storage;
    }

    public override void Configure()
    {
        Post("/private/delivery-receipts/upload-multiple");
        Roles("authenticated");
    }

    public override async Task HandleAsync(UploadMultipleDeliveryReceiptsRequest req, CancellationToken ct)
    {
        _logger.LogInformation("UploadMultipleDeliveryReceipts request started {@Req}", req);

        if (req.Files.Count == 0 || req.Files.Count > 10)
            throw new BadRequestException("Must request between 1 and 10 files");

        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.Parse(sub) != req.UserId)
            throw new UnauthorizedException();

        var uploads = new List<FileUploadDto>();

        foreach (var spec in req.Files)
        {
            var fileExt = FileExtensionHelper.Parse(spec.FileType);
            var (_, mime) = FileExtensionHelper.ToContentType(fileExt);

            var fileId = Guid.NewGuid();
            var key = StorageKeyBuilder.GetPrivateUploadKey(req.UserId, fileId, FileCategory.Delivery_Receipt, fileExt);
            var url = _storage.GetPresignedPutUrl(key, mime, TimeSpan.FromMinutes(5));
            uploads.Add(new FileUploadDto(fileId, key, url));

            _logger.LogInformation("Generated presigned URL for {UserId}, key={Key}", req.UserId, key);
        }

        await Send.OkAsync(new UploadMultipleDeliveryReceiptsResponse(uploads), ct);
    }
}

// --- Request/Response Models ---

public record UploadFileSpec(
    [Required] string FileType // "pdf", "jpeg", "png", "tiff"
);

public record UploadMultipleDeliveryReceiptsRequest(
    [Required] Guid UserId,
    [Required] List<UploadFileSpec> Files
);

public record UploadMultipleDeliveryReceiptsResponse(List<FileUploadDto> Files);

public record FileUploadDto(Guid FileId, string FileKey, string UploadUrl);
