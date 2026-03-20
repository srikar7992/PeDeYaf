using System;

namespace PeDeYaf.Domain.Entities;

public enum AnnotationType
{
    Highlight,
    Underline,
    Strikethrough,
    Freehand,
    TextNote,
    VoiceNote,
    Stamp
}

public sealed class Annotation : AggregateRoot
{
    private Annotation() { }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid UserId { get; private set; }
    public int PageNumber { get; private set; }
    public AnnotationType Type { get; private set; }
    public string Data { get; private set; } = default!; // JSON payload
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
}
