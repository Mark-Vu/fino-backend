using FinoBackend.Commons.Enums;
using FinoBackend.Models;

namespace FinoBackend.Services;

public static class StorageKeyBuilder
{
    public static string GetPrivateUploadKey(Guid userId, Guid fileId, FileCategory category, FileExtension ext) =>
        $"private/{category.ToString().ToLowerInvariant()}/uploads/{userId}/{fileId}{NormalizeExt(ext)}";

    public static string GetPrivateResultKey(Guid userId, Guid jobId, FileCategory category) =>
        $"private/{category.ToString().ToLowerInvariant()}/results/{userId}/{jobId}.csv";

    public static string GetPublicUploadKey(Guid fileId, FileCategory category, FileExtension ext) =>
        $"public/{category.ToString().ToLowerInvariant()}/uploads/{fileId}{NormalizeExt(ext)}";

    public static string GetPublicResultKey(Guid jobId, FileCategory category) =>
        $"public/{category.ToString().ToLowerInvariant()}/results/{jobId}.csv";

    private static string NormalizeExt(FileExtension ext) =>
        "." + ext.ToString().ToLowerInvariant(); // always ".pdf", ".jpg", etc.
}