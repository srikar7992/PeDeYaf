using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using PeDeYaf.Application.Common.Interfaces;

namespace PeDeYaf.Infrastructure.FileStorage;

public class S3FileStorage(IAmazonS3 s3Client, IConfiguration configuration) : IFileStorage
{
    private readonly string _bucket = configuration["AWS:S3:BucketName"] ?? "pedeyaf-storage";
    private readonly string _cdnDomain = configuration["AWS:S3:CdnDomain"] ?? "cdn.pedeyaf.com";

    public async Task<string> GeneratePresignedUploadUrlAsync(
        string key,
        string contentType,
        int expiryMinutes,
        long maxSizeBytes,
        CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            ContentType = contentType,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
        };

        return await s3Client.GetPreSignedURLAsync(request);
    }

    public async Task<string> GeneratePresignedDownloadUrlAsync(
        string key,
        int expiryMinutes = 60,
        CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            ResponseHeaderOverrides = new ResponseHeaderOverrides
            {
                ContentDisposition = "inline",
                ContentType = "application/pdf",
            },
        };

        return await s3Client.GetPreSignedURLAsync(request);
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
    {
        var response = await s3Client.GetObjectAsync(_bucket, key, ct);
        return response.ResponseStream;
    }

    public async Task CopyAsync(string sourceKey, string destKey, CancellationToken ct = default)
    {
        await s3Client.CopyObjectAsync(_bucket, sourceKey, _bucket, destKey, ct);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await s3Client.DeleteObjectAsync(_bucket, key, ct);
    }

    public async Task UploadAsync(string key, Stream fileStream, string contentType, CancellationToken ct = default)
    {
        var putRequest = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType
        };
        await s3Client.PutObjectAsync(putRequest, ct);
    }

    public string GetPublicCdnUrl(string key)
    {
        return $"https://{_cdnDomain}/{key}";
    }
}
