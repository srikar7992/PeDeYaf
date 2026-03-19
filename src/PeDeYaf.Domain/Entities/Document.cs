using System;
using System.Collections.Generic;
using PeDeYaf.Domain.Enums;
using PeDeYaf.Domain.Events;
using System.Linq;

namespace PeDeYaf.Domain.Entities;

public sealed class Document : AggregateRoot
{
    private Document() { } // EF Core ctor

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? FolderId { get; private set; }
    public string Name { get; private set; } = default!;
    public string S3Key { get; private set; } = default!;
    public string? ThumbnailS3Key { get; private set; }
    public long FileSizeBytes { get; private set; }
    public int PageCount { get; private set; }
    public DocumentStatus Status { get; private set; }
    public bool IsPasswordProtected { get; private set; }
    public string? ExtractedText { get; private set; }
    public List<string> Tags { get; private set; } = [];
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = default!;
    public Folder? Folder { get; private set; }
    public IReadOnlyList<Annotation> Annotations { get; private set; } = [];
    public IReadOnlyList<DocumentVersion> Versions { get; private set; } = [];

    public static Document Create(
        Guid userId, string name, string s3Key, long fileSizeBytes)
    {
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            S3Key = s3Key,
            FileSizeBytes = fileSizeBytes,
            Status = DocumentStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        // Raise domain event
        doc.AddDomainEvent(new DocumentUploadedEvent(doc.Id, doc.UserId, doc.S3Key));

        return doc;
    }

    public void MarkProcessed(int pageCount, string? thumbnailKey, string? extractedText)
    {
        PageCount = pageCount;
        ThumbnailS3Key = thumbnailKey;
        ExtractedText = extractedText;
        Status = DocumentStatus.Ready;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new DocumentProcessedEvent(Id, UserId));
    }

    public void MarkFailed()
    {
        Status = DocumentStatus.Failed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ApplyTags(IEnumerable<string> tags)
    {
        Tags = [..tags.Select(t => t.Trim().ToLower()).Distinct()];
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkOcrComplete(string extractedText)
    {
        ExtractedText = extractedText;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new OcrCompletedEvent(Id, UserId));
    }

    public void MoveToFolder(Guid? newFolderId)
    {
        FolderId = newFolderId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
