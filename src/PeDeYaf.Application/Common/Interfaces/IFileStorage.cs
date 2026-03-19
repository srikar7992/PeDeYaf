using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PeDeYaf.Application.Common.Interfaces;

public interface IFileStorage
{
    Task<string> GeneratePresignedUploadUrlAsync(string key, string contentType, int expiryMinutes, long maxSizeBytes, CancellationToken ct = default);
    Task<string> GeneratePresignedDownloadUrlAsync(string key, int expiryMinutes = 60, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string key, CancellationToken ct = default);
    Task CopyAsync(string sourceKey, string destKey, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task UploadAsync(string key, Stream fileStream, string contentType, CancellationToken ct = default);
    string GetPublicCdnUrl(string key);
}
