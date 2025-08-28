using FinoBackend.Data;
using FinoBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace FinoBackend.Services;

public class BankStatementService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<BankStatementService> _logger;
    
    public BankStatementService(ApplicationDbContext db, ILogger<BankStatementService> logger)
    {
        _db = db;
        _logger = logger;
    }
    
    public async Task<BankStatementFile> CreateBankStatementFileAsync(
        Guid id,
        Guid userId,
        string pdf_file_key,
        string csv_file_key,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Creating bank statement file");
        var file = new BankStatementFile
        {
            Id = id,
            UserId = userId,
            PdfFileKey = pdf_file_key,
            CsvFileKey = csv_file_key,
        };

        await _db.BankStatementFiles.AddAsync(file);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Ending creating bank statement file");
        return file;
    }
    

    public async Task<BankStatementFile?> GetBankStatementFileByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.BankStatementFiles
            .FirstOrDefaultAsync(f => f.Id == id, ct);
    }
}