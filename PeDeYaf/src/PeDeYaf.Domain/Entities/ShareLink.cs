using System;

namespace PeDeYaf.Domain.Entities;

public sealed class ShareLink : AggregateRoot
{
    private ShareLink() { }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public string Token { get; private set; } = default!;
    public string? PasswordHash { get; private set; }
    public string Permission { get; private set; } = "view";
    public int? MaxViews { get; private set; }
    public int ViewCount { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
