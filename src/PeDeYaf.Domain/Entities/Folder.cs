using System;

namespace PeDeYaf.Domain.Entities;

public sealed class Folder : AggregateRoot
{
    private Folder() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? ParentId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Color { get; private set; }
    public string? Icon { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
}
