using System.Runtime.Serialization;

namespace FinoBackend.Models;

public enum FileExtension
{
    [EnumMember(Value = "pdf")]
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