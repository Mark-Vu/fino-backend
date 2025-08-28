using System.ComponentModel.DataAnnotations.Schema;

namespace FinoBackend.Models;

[Table("users")]
public class User : BaseModel
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; }
}