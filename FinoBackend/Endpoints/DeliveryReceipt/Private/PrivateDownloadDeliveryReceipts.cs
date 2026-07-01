using FastEndpoints;
using FinoBackend.Common;
using FinoBackend.Services;

namespace FinoBackend.Endpoints.DeliveryReceipt.Private;
public class PrivateDownloadDeliveryReceiptCsv
    : Endpoint<PrivateDownloadDeliveryReceiptCsvRequest, PrivateDownloadDeliveryReceiptCsvResponse>
{
    private readonly StorageService _storage;
    private readonly ILogger<PrivateDownloadDeliveryReceiptCsv> _logger;
    private readonly UploadedFileService _uploadedFileService;

    public PrivateDownloadDeliveryReceiptCsv(
        StorageService storage,
        ILogger<PrivateDownloadDeliveryReceiptCsv> logger,
        UploadedFileService uploadedFileService)
    {
        _storage = storage;
        _logger = logger;
        _uploadedFileService = uploadedFileService;
    }

    public override void Configure()
    {
        Get("/private/delivery-receipts/{UserId:guid}/{FileId:guid}/download");
        Roles("authenticated");
    }

    public override async Task HandleAsync(PrivateDownloadDeliveryReceiptCsvRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Starting DownloadDeliveryReceiptCsv file {FileId}", req.FileId);
        var sub = User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(sub, out var userId))
            throw new UnauthorizedException();

        var file = await _uploadedFileService.GetUploadedFileByIdAsync(req.FileId, ct);
        if (file is null)
            throw new NotFoundException("File not found");
        if (userId != file.UserId)
            throw new UnauthorizedException();

        var fileName = Path.ChangeExtension(file.OriginalFileName, ".csv");

        var url = _storage.GetPresignedGetUrl(
            file.OutputFileKey,
            TimeSpan.FromMinutes(5),
            fileName: fileName);

        await Send.OkAsync(new PrivateDownloadDeliveryReceiptCsvResponse(url), ct);
    }
}

public record PrivateDownloadDeliveryReceiptCsvRequest(Guid FileId, Guid UserId);
public record PrivateDownloadDeliveryReceiptCsvResponse(string DownloadUrl);
