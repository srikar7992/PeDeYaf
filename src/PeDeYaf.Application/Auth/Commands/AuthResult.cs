using System;

namespace PeDeYaf.Application.Auth.Commands;

public record AuthResult(string AccessToken, string RefreshToken, UserDto User);

public record UserDto(Guid Id, string Phone, string? Name, string? AvatarUrl, string Plan, long StorageUsed, long StorageLimit);
