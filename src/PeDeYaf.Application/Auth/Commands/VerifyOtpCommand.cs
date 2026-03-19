using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PeDeYaf.Application.Common.Interfaces;
using PeDeYaf.Domain.Exceptions;
using PeDeYaf.Domain.Repositories;
using System.Text.Json;

namespace PeDeYaf.Application.Auth.Commands;

public record VerifyOtpCommand(string Phone, string Otp) : IRequest<AuthResult>;

public class VerifyOtpCommandHandler(
    IUserRepository userRepository,
    ITokenService tokenService,
    ICacheService cache)
    : IRequestHandler<VerifyOtpCommand, AuthResult>
{
    public async Task<AuthResult> Handle(VerifyOtpCommand request, CancellationToken ct)
    {
        var cached = await cache.GetStringAsync($"otp:{request.Phone}", ct);
        if (cached is null)
            throw new InvalidOtpException("OTP expired or not found");

        var otpData = JsonSerializer.Deserialize<OtpCacheEntry>(cached)!;

        // Verify hash
        if (!BCrypt.Net.BCrypt.Verify(request.Otp, otpData.OtpHash))
            throw new InvalidOtpException("Invalid OTP");

        // Invalidate OTP immediately after successful use
        await cache.RemoveAsync($"otp:{request.Phone}", ct);

        var user = await userRepository.GetByIdAsync(otpData.UserId, ct)
                   ?? throw new UserNotFoundException();

        // Generate JWT pair
        var (accessToken, refreshToken) = await tokenService.GenerateTokenPairAsync(user, ct);

        // Returning the data, infrastructure layer handles refresh token persistence in API/Middleware or generic behavior
        var userDto = new UserDto(user.Id, user.Phone, user.Name, user.AvatarS3Key, user.Plan.ToString(), user.StorageUsed, user.StorageLimit);

        return new AuthResult(accessToken, refreshToken, userDto);
    }
}
