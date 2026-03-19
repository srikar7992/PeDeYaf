using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PeDeYaf.Application.Common.Interfaces;
using PeDeYaf.Domain.Exceptions;
using PeDeYaf.Domain.Repositories;
using System.Text.Json;

namespace PeDeYaf.Application.Auth.Commands;

public record RequestOtpCommand(string Phone) : IRequest<Unit>;

public class RequestOtpCommandValidator : AbstractValidator<RequestOtpCommand>
{
    public RequestOtpCommandValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .Matches(@"^\+[1-9]\d{7,14}$")
            .WithMessage("Phone must be in E.164 format (+919876543210)");
    }
}

public class RequestOtpCommandHandler(
    IUserRepository userRepository,
    ISmsService smsService,
    ICacheService cache,
    ILogger<RequestOtpCommandHandler> logger)
    : IRequestHandler<RequestOtpCommand, Unit>
{
    private const int OtpExpiryMinutes = 10;
    private const int MaxAttemptsPerHour = 5;

    public async Task<Unit> Handle(RequestOtpCommand request, CancellationToken ct)
    {
        // Simple rate limiting placeholder
        var rateLimitKey = $"otp_rate:{request.Phone}";
        var attemptsStr = await cache.GetStringAsync(rateLimitKey, ct);
        var attempts = int.TryParse(attemptsStr, out var a) ? a : 0;

        if (attempts >= MaxAttemptsPerHour)
            throw new TooManyRequestsException("OTP rate limit exceeded");

        // Upsert user (phone is identity)
        var user = await userRepository.FindByPhoneAsync(request.Phone, ct)
                   ?? await userRepository.CreateAsync(request.Phone, ct);

        // Generate secure 6-digit OTP
        var otp = GenerateOtp();

        // Store OTP hash in cache (never store plain OTP)
        var otpData = new OtpCacheEntry(BCrypt.Net.BCrypt.HashPassword(otp), user.Id, DateTimeOffset.UtcNow);
        await cache.SetStringAsync(
            $"otp:{request.Phone}",
            JsonSerializer.Serialize(otpData),
            TimeSpan.FromMinutes(OtpExpiryMinutes),
            ct);

        // Increment rate limit counter
        await cache.SetStringAsync(rateLimitKey, (attempts + 1).ToString(), TimeSpan.FromHours(1), ct);

        await smsService.SendOtpAsync(request.Phone, otp, ct);

        logger.LogInformation("OTP sent to {Phone}", request.Phone);
        return Unit.Value;
    }

    private static string GenerateOtp()
    {
        var bytes = new byte[4];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToUInt32(bytes, 0) % 1_000_000;
        return value.ToString("D6");
    }
}

public record OtpCacheEntry(string OtpHash, Guid UserId, DateTimeOffset CreatedAt);
