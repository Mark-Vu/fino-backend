using System.Runtime.Serialization;

namespace FinoBackend.Commons.Enums;

public enum FileExtension
{
    Pdf,
    [EnumMember(Value = "jpg")]
    Jpg,

    [EnumMember(Value = "png")]
    Png,

    [EnumMember(Value = "tiff")]
    Tiff,
    
    [EnumMember(Value = "xlsx")]
    Xlsx
}