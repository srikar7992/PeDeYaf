using System;

namespace PeDeYaf.Domain.Entities;

/// <summary>
/// Represents a secure refresh token used to obtain new JWT access tokens without requiring the user to re-authenticate.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Gets or sets the unique identifier for the refresh token record.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets or sets the cryptographically secure token string.
    /// </summary>
    public string Token { get; private set; }

    /// <summary>
    /// Gets or sets the identifier of the user who owns this refresh token.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets or sets the date and time when this token expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// Gets or sets the date and time when this token was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets or sets the date and time if this token was revoked. A non-null value indicates the token is no longer valid.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the token has expired.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    /// <summary>
    /// Gets a value indicating whether the token is currently active and usable.
    /// </summary>
    public bool IsActive => RevokedAt == null && !IsExpired;

    private RefreshToken() { Token = string.Empty; } // EF Core

    /// <summary>
    /// Creates a new RefreshToken instance.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="token">The secure random token string.</param>
    /// <param name="expiresInDays">The number of days until the token expires.</param>
    /// <returns>A new <see cref="RefreshToken"/>.</returns>
    public static RefreshToken Create(Guid userId, string token, int expiresInDays = 30)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(expiresInDays)
        };
    }

    /// <summary>
    /// Marks the refresh token as revoked, effectively invalidating it.
    /// </summary>
    public void Revoke()
    {
        RevokedAt = DateTimeOffset.UtcNow;
    }
}
