using System.Security.Claims;
using FastEndpoints;
using FinoBackend.Common;
using FinoBackend.Services;

namespace FinoBackend.Endpoints.BankStatementFile;
public class PublicDownloadBankStatementCsv 
    : Endpoint<PublicDownloadBankStatementCsvRequest, PublicDownloadBankStatementCsvResponse>
{
    private readonly StorageService _storage;
    private readonly ILogger<PublicDownloadBankStatementCsv> _logger;
    private readonly BankStatementService _bankStatementService;

    public PublicDownloadBankStatementCsv(StorageService storage,  ILogger<PublicDownloadBankStatementCsv> logger, BankStatementService bankStatementService)
    {
        _storage = storage;
        _logger = logger;
        _bankStatementService = bankStatementService;
    }

    public override void Configure()
    {
        Get("/public/bank-statement-files/{FileId:guid}/download");
        AllowAnonymous();
    }

    public override async Task HandleAsync(PublicDownloadBankStatementCsvRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Starting DownloadBankStatementCsv file {FileId}", req.FileId);

        // look up file from DB (ensure it belongs to user)
        var file = await _bankStatementService.GetBankStatementFileByIdAsync(req.FileId, ct);
        if (file is null)
        {
            throw new NotFoundException("File not found");
        }
        var fileName = Path.ChangeExtension(file.OriginalFileName, ".csv");

        var url = await _storage.GetPresignedGetUrlAsync(
            file.CsvFileKey,
            TimeSpan.FromMinutes(5),
            fileName: fileName,
            ct: ct);

        await Send.OkAsync(new PublicDownloadBankStatementCsvResponse(url), ct);
    }
}

public record PublicDownloadBankStatementCsvRequest(Guid FileId, string FileName);
public record PublicDownloadBankStatementCsvResponse(string DownloadUrl);