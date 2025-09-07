using System.Runtime.Serialization;

public enum OwnerType
{
    [EnumMember(Value = "auth_user")]
    AuthenticatedUser,
    [EnumMember(Value = "anonymous")]
    Anonymous
}