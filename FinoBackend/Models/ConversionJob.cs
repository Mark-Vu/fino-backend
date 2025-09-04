// Models/ConversionJob.cs
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FinoBackend.Models;

[Table("conversion_jobs")]
public class ConversionJob : BaseModel
{
    public JobStatus Status { get; set; } = JobStatus.Pending;

    public string? ErrorMessage { get; set; }

    [ForeignKey(nameof(BankStatementFile))]
    public Guid BankStatementFileId { get; set; }

    [JsonIgnore]
    public BankStatementFile BankStatementFile { get; set; } = null!;

    // Set these in code (worker/service), not with property initializers.
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}