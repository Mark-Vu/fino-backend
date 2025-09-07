using Amazon.S3;
using Amazon.S3.Model;
using FinoBackend.Common;
using Microsoft.Extensions.Configuration;

namespace FinoBackend.Services;

public class StorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    public StorageService(IAmazonS3 s3, IConfiguration config)
    {
        _s3 = s3;
        _bucket = config["S3:Bucket"] ?? throw new InvalidOperationException("Missing S3:Bucket");
    }

    // === Upload / Download ===
    public async Task UploadAsync(string key, Stream stream, string contentType, CancellationToken ct = default)
    {
        if (stream.CanSeek) stream.Position = 0;
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = stream,
            ContentType = contentType
        }, ct);
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
    {
        var response = await _s3.GetObjectAsync(_bucket, key, ct);
        return response.ResponseStream;
    }

    // === Presigned URLs ===
    public string GetPresignedPutUrl(string key, string contentType, TimeSpan ttl) =>
        _s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(ttl),
            ContentType = contentType
        });

    public string GetPresignedGetUrl(string key, TimeSpan ttl, string? fileName = null)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(ttl),
        };

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            request.ResponseHeaderOverrides = new ResponseHeaderOverrides
            {
                ContentDisposition = $"attachment; filename=\"{fileName}\""
            };
        }

        return _s3.GetPreSignedURL(request);
    }

    // === Metadata / Ops ===
    public async Task<(long Size, string? ETag, string? ContentType)?> TryHeadAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var meta = await _s3.GetObjectMetadataAsync(_bucket, key, ct);
            return (meta.ContentLength, meta.ETag, meta.Headers.ContentType);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task<DeleteObjectResponse> DeleteAsync(string key, CancellationToken ct = default) =>
        _s3.DeleteObjectAsync(_bucket, key, ct);

    // === Validation ===
    public async Task<(bool Exists, bool ValidSize, long? Size)> ValidateFileAsync(string key, CancellationToken ct = default)
    {
        var meta = await TryHeadAsync(key, ct);
        if (meta == null) return (false, false, null);

        if (meta.Value.Size > Constants.MAX_FILE_SIZE_BYTES)
        {
            await DeleteAsync(key, ct);
            return (true, false, meta.Value.Size);
        }

        return (true, true, meta.Value.Size);
    }

    public async Task<(bool AllValid, List<(string Key, long? Size)> Results)> ValidateMultipleFilesAsync(IEnumerable<string> keys, CancellationToken ct = default)
    {
        var results = new List<(string Key, long? Size)>();
        bool allValid = true;

        foreach (var key in keys)
        {
            var meta = await TryHeadAsync(key, ct);
            if (meta == null || meta.Value.Size > Constants.MAX_FILE_SIZE_BYTES)
            {
                allValid = false;
                break;
            }
            results.Add((key, meta.Value.Size));
        }

        if (!allValid)
        {
            foreach (var (key, _) in results)
            {
                try { await DeleteAsync(key, ct); } catch { /* ignore */ }
            }
        }

        return (allValid, results);
    }
}
