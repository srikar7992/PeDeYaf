using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PeDeYaf.Application.Common.Interfaces;
using PeDeYaf.Domain.Entities;
using PeDeYaf.Domain.Repositories;

namespace PeDeYaf.Application.Documents.Commands;

public record GenerateUploadUrlCommand(
    Guid UserId,
    string FileName,
    string ContentType,
    Guid? FolderId
) : IRequest<GenerateUploadUrlResult>;

public record GenerateUploadUrlResult(
    string UploadUrl,
    Guid DocumentId,
    string S3Key
);

public class GenerateUploadUrlHandler(
    IFileStorage fileStorage,
    IDocumentRepository documents)
    : IRequestHandler<GenerateUploadUrlCommand, GenerateUploadUrlResult>
{
    private const long MaxFileSizeBytes = 500 * 1024 * 1024; // 500MB

    public async Task<GenerateUploadUrlResult> Handle(
        GenerateUploadUrlCommand request, CancellationToken ct)
    {
        var sanitizedName = SanitizeFileName(request.FileName);
        var s3Key = $"documents/{request.UserId}/{Guid.NewGuid()}/{sanitizedName}";

        // Create document record in PENDING state before upload
        var document = Document.Create(request.UserId, sanitizedName, s3Key, 0);
        if (request.FolderId.HasValue)
            document.MoveToFolder(request.FolderId.Value);

        await documents.AddAsync(document, ct);

        var uploadUrl = await fileStorage.GeneratePresignedUploadUrlAsync(
            s3Key,
            request.ContentType,
            expiryMinutes: 15,
            maxSizeBytes: MaxFileSizeBytes,
            ct);

        return new GenerateUploadUrlResult(uploadUrl, document.Id, s3Key);
    }

    private static string SanitizeFileName(string name)
    {
        var clean = Path.GetInvalidFileNameChars()
            .Aggregate(name, (s, c) => s.Replace(c.ToString(), ""));
        return clean.Length > 200 ? clean[..200] : clean;
    }
}
