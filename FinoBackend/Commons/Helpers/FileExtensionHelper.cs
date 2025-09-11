using FinoBackend.Common;
using FinoBackend.Commons.Enums;

public static class FileExtensionHelper
{
    public static FileExtension Parse(string extString)
    {
        return extString.ToLowerInvariant() switch
        {
            "pdf"  => FileExtension.Pdf,
            "jpg"  => FileExtension.Jpg,
            "jpeg" => FileExtension.Jpg,
            "png"  => FileExtension.Png,
            "tiff" => FileExtension.Tiff,
            "tif"  => FileExtension.Tiff,
            "xlsx" => FileExtension.Xlsx,
            _ => throw new BadRequestException($"Unsupported file extension: {extString}")
        };
    }
    
    public static (string ext, string mime) ToContentType(FileExtension fileExt) =>
        fileExt switch
        {
            FileExtension.Pdf  => (".pdf", "application/pdf"),
            FileExtension.Jpg  => (".jpg", "image/jpeg"),
            FileExtension.Png  => (".png", "image/png"),
            FileExtension.Tiff => (".tiff", "image/tiff"),
            FileExtension.Xlsx => (".xlsx", "application/vnd.ms-excel"),
            _ => throw new BadRequestException($"Unsupported file extension: {fileExt}")
        };
}