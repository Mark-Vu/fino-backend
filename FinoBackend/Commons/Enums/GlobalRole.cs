using System.Runtime.Serialization;

namespace FinoBackend.Commons.Enums;
public enum GlobalRole
{
    User,
    Developer,
    [EnumMember(Value = "super_admin")]
    SuperAdmin
}