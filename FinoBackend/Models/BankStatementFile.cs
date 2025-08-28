using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FinoBackend.Models;

[Table("bank_statement_files")]
public class BankStatementFile : BaseModel
{
    public string PdfFileKey {get; set; } = string.Empty;
    public string CsvFileKey { get; set; } = string.Empty;

    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }
    public User User { get; set; }
    public ICollection<ConversionJob>  ConversionJobs { get; set; } = new List<ConversionJob>();
}