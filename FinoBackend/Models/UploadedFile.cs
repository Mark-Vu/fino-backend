// Models/BankStatementFile.cs
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using FinoBackend.Commons.Enums;

namespace FinoBackend.Models;

[Table("uploaded_files")]
public class UploadedFile : BaseModel
{
    public Guid? UserId { get; set; }
    public OwnerType OwnerType { get; set; } = OwnerType.AuthenticatedUser;

    public string UploadedFileKey { get; set; } = string.Empty; 
    public FileExtension FileExtension { get; set; } = FileExtension.Pdf;
    
    public FileCategory Category { get; set; } = FileCategory.Bank_Statement;
    public string OriginalFileName { get; set; } = string.Empty;
    public string? OutputFileKey { get; set; }             

    [JsonIgnore]
    public ICollection<ConversionJob> ConversionJobs { get; set; } = new List<ConversionJob>();
}
