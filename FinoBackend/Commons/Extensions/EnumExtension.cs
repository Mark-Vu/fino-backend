using System.Reflection;
using System.Runtime.Serialization;

namespace FinoBackend.Commons;

public static class EnumExtensions
{
    /// <summary>
    /// Converts an enum value into a normalized string representation.
    /// 
    /// - If the enum field has an [EnumMember(Value = "...")] attribute,
    ///   that value is returned.
    /// - Otherwise, falls back to the enum name lowercased (e.g. "Developer" → "developer").
    /// 
    /// Useful for storing enums as predictable strings in the database or serializing
    /// them consistently in APIs.
    /// </summary>

    public static string ToNormalizedString(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var attr = member?.GetCustomAttribute<EnumMemberAttribute>();
        return attr?.Value ?? value.ToString().ToLowerInvariant();
    }
}