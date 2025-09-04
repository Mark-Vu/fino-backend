// Models/BankStatementFile.cs
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FinoBackend.Models;

[Table("bank_statement_files")]
public class BankStatementFile : BaseModel
{
    public Guid? UserId { get; set; }
    public OwnerType OwnerType { get; set; } = OwnerType.AuthenticatedUser;

    public string PdfFileKey { get; set; } = string.Empty; 
    public string OriginalFileName { get; set; } = string.Empty;
    public string? CsvFileKey { get; set; }             

    [JsonIgnore]
    public ICollection<ConversionJob> ConversionJobs { get; set; } = new List<ConversionJob>();
}
