using System;

namespace PeDeYaf.Domain.Entities;

public sealed class DocumentVersion : AggregateRoot
{
    private DocumentVersion() { }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public int VersionNumber { get; private set; }
    public string S3Key { get; private set; } = default!;
    public long FileSizeBytes { get; private set; }
    public Guid ChangedBy { get; private set; }
    public string? ChangeNote { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
