using System;

namespace PeDeYaf.Domain.Entities;

public sealed class User : AggregateRoot
{
    private User() { }

    public Guid Id { get; private set; }
    public string Phone { get; private set; } = default!;
    public string? Name { get; private set; }
    public string? AvatarS3Key { get; private set; }
    public Enums.UserPlan Plan { get; private set; }
    public long StorageLimit { get; private set; }
    public long StorageUsed { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static User Create(string phone)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Phone = phone,
            Plan = Enums.UserPlan.Free,
            StorageLimit = 104857600, // 100MB free
            StorageUsed = 0,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}
