using System.Security.Claims;
using FastEndpoints;
using FinoBackend.Common;
using FinoBackend.Services;

namespace FinoBackend.Endpoints.BankStatementFile;
public class PrivateDownloadBankStatementCsv 
    : Endpoint<PrivateDownloadBankStatementCsvRequest, PrivateDownloadBankStatementCsvResponse>
{
    private readonly StorageService _storage;
    private readonly ILogger<PublicDownloadBankStatementCsv> _logger;
    private readonly BankStatementService _bankStatementService;

    public PrivateDownloadBankStatementCsv(StorageService storage,  ILogger<PublicDownloadBankStatementCsv> logger, BankStatementService bankStatementService)
    {
        _storage = storage;
        _logger = logger;
        _bankStatementService = bankStatementService;
    }

    public override void Configure()
    {
        Get("/private/bank-statement-files/{UserId:guid}/{FileId:guid}/download");
        Roles("authenticated");
    }

    public override async Task HandleAsync(PrivateDownloadBankStatementCsvRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Starting DownloadBankStatementCsv file {FileId}", req.FileId);
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // look up file from DB (ensure it belongs to user)
        var file = await _bankStatementService.GetBankStatementFileByIdAsync(req.FileId, ct);
        if (file is null)
        {
            throw new NotFoundException("File not found");
        }
        if (userId != file.UserId)
        {
            throw new UnauthorizedException();
        }

        // 👇 choose original name if available, else fall back to FileId
        var fileName = Path.ChangeExtension(file.OriginalFileName, ".csv");

        var url = await _storage.GetPresignedGetUrlAsync(
            file.CsvFileKey,
            TimeSpan.FromMinutes(5),
            fileName: fileName,
            ct: ct);

        await Send.OkAsync(new PrivateDownloadBankStatementCsvResponse(url), ct);
    }
}

public record PrivateDownloadBankStatementCsvRequest(Guid FileId, Guid UserId);
public record PrivateDownloadBankStatementCsvResponse(string DownloadUrl);