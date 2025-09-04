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
        => $"{_basePrivate}/bank_statement_converter/uploads/{userId}/{fileId}.pdf";

    public string GetPrivateCsvResultKey(Guid userId, Guid jobId)
        => $"{_basePrivate}/bank_statement_converter/results/{userId}/{jobId}.csv";

    public string GetPublicPdfUploadKey(Guid fileId)
        => $"{_basePublic}/bank_statement_converter/uploads/{fileId}.pdf";

    public string GetPublicCsvResultKey(Guid jobId)
        => $"{_basePublic}/bank_statement_converter/results/{jobId}.csv";
    
    // === Upload / Download ===

    public async Task UploadAsync(string key, Stream stream, string contentType, CancellationToken ct = default)
    {
        if (stream.CanSeek) stream.Position = 0; // reset before upload

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
    public Task<string> GetPresignedPutUrlAsync(string key, string contentType, TimeSpan ttl)
    {
        var url = _s3.GetPreSignedURL(new GetPreSignedUrlRequest {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(ttl),
            ContentType = contentType
        });
        return Task.FromResult(url);
    }

    public Task<string> GetPresignedGetUrlAsync(
        string key,
        TimeSpan ttl,
        string? fileName = null,
        CancellationToken ct = default)
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

        var url = _s3.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

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
    public async Task<(Guid FileId, string Key, string UploadUrl)> CreatePdfUploadPublicAsync(TimeSpan ttl)
    {
        var fileId = Guid.NewGuid();
        var key = GetPublicPdfUploadKey(fileId);
        var url = await GetPresignedPutUrlAsync(key, "application/pdf", ttl);
        return (fileId, key, url);
    }
    
    public async Task<(Guid FileId, string Key, string UploadUrl)> CreatePdfUploadPrivateAsync(Guid userId, TimeSpan ttl)
    {
        var fileId = Guid.NewGuid();
        var key = GetPrivatePdfUploadKey(userId, fileId);
        var url = await GetPresignedPutUrlAsync(key, "application/pdf", ttl);
        return (fileId, key, url);
    }
    
    public async Task<(bool Exists, bool ValidFileSize, long? size)> ValidateFileAsync(string key,
        CancellationToken ct = default)
    {
        try
        {
            var meta = await _s3.GetObjectMetadataAsync(_bucket, key, ct);

            var size = meta.ContentLength;
            if (size > Constants.MAX_FILE_SIZE_BYTES)
            {
                await _s3.DeleteObjectAsync(_bucket, key, ct); 
                return (true, true, size);   
            }

            return (true, false, size);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return (false, false, null); // file not found
        }   
    }
    
    // public async Task<IReadOnlyList<(string Key, bool Exists, bool TooLarge, long? Size)>> 
    //     ValidateMultipleFilesAsync(IEnumerable<string> keys, CancellationToken ct = default)
    // {
    //     var tasks = keys.Select(async key =>
    //     {
    //         try
    //         {
    //             var meta = await _s3.GetObjectMetadataAsync(_bucket, key, ct);
    //             var size = meta.ContentLength;
    //             return (key, true, size > Constants.MAX_FILE_SIZE_BYTES, size);
    //         }
    //         catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    //         {
    //             return (key, false, false, (long?)null);
    //         }
    //     });
    //
    //     return await Task.WhenAll(tasks);
    // }
    public async Task<(bool AllValid, List<(string Key, long? Size)> Results)>
        ValidateMultipleFilesAsync(List<string> keys, CancellationToken ct = default)
    {
        var results = new List<(string Key, long? Size)>();
        bool allValid = true;

        foreach (var key in keys)
        {
            try
            {
                var meta = await _s3.GetObjectMetadataAsync(_bucket, key, ct);
                results.Add((key, meta.ContentLength));

                if (meta.ContentLength > Constants.MAX_FILE_SIZE_BYTES)
                {
                    allValid = false;
                    break; // stop immediately if one is too large
                }
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // file missing → invalid
                results.Add((key, null));
                allValid = false;
                break;
            }
        }

        if (!allValid)
        {
            // delete all collected files
            foreach (var (key, _) in results)
            {
                try { await _s3.DeleteObjectAsync(_bucket, key, ct); }
                catch { /* ignore delete errors */ }
            }
        }

        return (allValid, results);
    }

}
