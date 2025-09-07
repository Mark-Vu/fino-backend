using System.Reflection;
using System.Runtime.Serialization;

namespace FinoBackend.Commons;

public static class EnumExtensions
{
    public static string ToNormalizedString(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var attr = member?.GetCustomAttribute<EnumMemberAttribute>();
        return attr?.Value ?? value.ToString().ToLowerInvariant();
    }
}