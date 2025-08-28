using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace FinoBackend.Services;

public class StorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    // optional prefixes you can tweak via config
    private readonly string _basePrivate = "private";
    private readonly string _basePublic  = "public";

    public StorageService(IAmazonS3 s3, IConfiguration config)
    {
        _s3 = s3;
        _bucket = config["S3:Bucket"]
                  ?? throw new InvalidOperationException("Missing S3:Bucket");
        _basePrivate = "private";
        _basePublic  = "public";
    }

    // --- Keys (single bucket; namespacing via prefixes) ---
    public string GetPrivatePdfUploadKey(Guid userId, Guid fileId)
        => $"{_basePrivate}/uploads/{userId}/{fileId}.pdf";

    public string GetPrivateCsvResultKey(Guid userId, Guid jobId)
        => $"{_basePrivate}/results/{userId}/{jobId}.csv";

    public string GetPublicPdfUploadKey(Guid fileId)
        => $"{_basePublic}/uploads/{fileId}.pdf";

    public string GetPublicCsvResultKey(Guid jobId)
        => $"{_basePublic}/results/{jobId}.csv";
    
    
    // === Upload / Download ===

    public async Task UploadAsync(string key, Stream stream, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = stream,
            ContentType = contentType
        };
        await _s3.PutObjectAsync(request, ct);
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
    {
        var response = await _s3.GetObjectAsync(_bucket, key, ct);
        return response.ResponseStream;
    }
    
    // --- Presigned URLs ---
    public string GetPresignedPutUrl(string key, string contentType, TimeSpan ttl)
        => _s3.GetPreSignedURL(new GetPreSignedUrlRequest {
               BucketName = _bucket,
               Key = key,
               Verb = HttpVerb.PUT,
               Expires = DateTime.UtcNow.Add(ttl),
               ContentType = contentType
           });

    public string GetPresignedGetUrl(string key, TimeSpan ttl)
        => _s3.GetPreSignedURL(new GetPreSignedUrlRequest {
               BucketName = _bucket,
               Key = key,
               Verb = HttpVerb.GET,
               Expires = DateTime.UtcNow.Add(ttl)
           });

    // --- Metadata / Ops ---
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

    public Task<DeleteObjectResponse> DeleteAsync(string key, CancellationToken ct = default)
        => _s3.DeleteObjectAsync(_bucket, key, ct);

    // --- Convenience: create fileId + key + presigned PUT in one call ---
    public (Guid FileId, string Key, string UploadUrl) CreatePdfUpload(Guid userId, TimeSpan ttl)
    {
        var fileId = Guid.NewGuid();
        var key = GetPrivatePdfUploadKey(userId, fileId);
        var url = GetPresignedPutUrl(key, "application/pdf", ttl);
        return (fileId, key, url);
    }
}
