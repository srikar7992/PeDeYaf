using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using PeDeYaf.Application.Auth.Commands;
using PeDeYaf.Application.Common.Interfaces;
using PeDeYaf.Domain.Entities;
using PeDeYaf.Domain.Exceptions;
using PeDeYaf.Domain.Repositories;
using Xunit;

namespace PeDeYaf.Application.Tests.Auth;

// Mock ICacheService to wrap MemoryDistributedCache for tests
public class MemoryCacheService : ICacheService
{
    private readonly MemoryDistributedCache _cache = new(Options.Create(new MemoryDistributedCacheOptions()));

    public async Task<string?> GetStringAsync(string key, CancellationToken ct = default)
    {
        return await _cache.GetStringAsync(key, ct);
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? absoluteExpirationRelativeToNow = null, CancellationToken ct = default)
    {
        var options = new DistributedCacheEntryOptions();
        if (absoluteExpirationRelativeToNow.HasValue)
            options.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;
        
        await _cache.SetStringAsync(key, value, options, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await _cache.RemoveAsync(key, ct);
    }
}

public class VerifyOtpCommandTests
{
    [Fact]
    public async Task Handle_ValidOtp_ReturnsTokens()
    {
        // Arrange
        var cache = new MemoryCacheService();
        var otp = "123456";
        var hash = BCrypt.Net.BCrypt.HashPassword(otp);
        var userId = Guid.NewGuid();
        var phone = "+1234567890";
        var cacheEntry = new OtpCacheEntry(hash, userId, DateTimeOffset.UtcNow);

        await cache.SetStringAsync($"otp:{phone}", JsonSerializer.Serialize(cacheEntry));

        var mockUserRepo = new Mock<IUserRepository>();
        var user = User.Create(phone);
        // reflection or trick to set id, here we just return it, verification doesn't strict check ID in fake user, but repo receives queried ID
        mockUserRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var mockTokenService = new Mock<ITokenService>();
        mockTokenService.Setup(t => t.GenerateTokenPairAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("access.token.jwt", "refresh_token_opaque"));

        var handler = new VerifyOtpCommandHandler(mockUserRepo.Object, mockTokenService.Object, cache);

        // Act
        var result = await handler.Handle(new VerifyOtpCommand(phone, otp), CancellationToken.None);

        // Assert
        result.AccessToken.Should().NotBeEmpty();
        result.RefreshToken.Should().NotBeEmpty();
        result.User.Phone.Should().Be(phone);
    }

    [Fact]
    public async Task Handle_ExpiredOtp_ThrowsInvalidOtpException()
    {
        // No OTP in cache = expired
        var cache = new MemoryCacheService();
        var handler = new VerifyOtpCommandHandler(
            Mock.Of<IUserRepository>(), Mock.Of<ITokenService>(), cache);

        await Assert.ThrowsAsync<InvalidOtpException>(
            () => handler.Handle(new VerifyOtpCommand("+1234567890", "999999"), CancellationToken.None));
    }
}
