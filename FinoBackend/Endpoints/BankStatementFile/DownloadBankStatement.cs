using System.Security.Claims;
using FastEndpoints;
using FinoBackend.Common;
using FinoBackend.Services;

namespace FinoBackend.Endpoints.BankStatementFile;
public class DownloadBankStatementCsv 
    : Endpoint<DownloadBankStatementCsvRequest, DownloadBankStatementCsvResponse>
{
    private readonly StorageService _storage;
    private readonly ILogger<DownloadBankStatementCsv> _logger;
    private readonly BankStatementService _bankStatementService;

    public DownloadBankStatementCsv(StorageService storage,  ILogger<DownloadBankStatementCsv> logger, BankStatementService bankStatementService)
    {
        _storage = storage;
        _logger = logger;
        _bankStatementService = bankStatementService;
    }

    public override void Configure()
    {
        Get("/bank-statement-files/{FileId:guid}/download");
        Roles("authenticated");
    }

    public override async Task HandleAsync(DownloadBankStatementCsvRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Starting DownloadBankStatementCsv file {FileId}", req.FileId);
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = Guid.Parse(sub);

        // look up file from DB (ensure it belongs to user)
        var file = await _bankStatementService.GetBankStatementFileByIdAsync(req.FileId, ct);
        if (file is null)
        {
            throw new NotFoundException("File not found");
        }

        if (file.UserId != userId)
        {
            throw new ForbiddenException();
        }

        var url = _storage.GetPresignedGetUrl(file.CsvFileKey, TimeSpan.FromMinutes(5));

        await Send.OkAsync(new DownloadBankStatementCsvResponse(url), ct);
    }
}

public record DownloadBankStatementCsvRequest(Guid FileId);
public record DownloadBankStatementCsvResponse(string DownloadUrl);