using FinoBackend.Commons.Enums;

namespace FinoBackend.Models;

public class User : BaseModel
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; }

    public GlobalRole GlobalRole { get; set; } = GlobalRole.User;
}
