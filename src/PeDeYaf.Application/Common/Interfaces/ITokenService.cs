using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PeDeYaf.Domain.Entities;

namespace PeDeYaf.Application.Common.Interfaces;

public interface ITokenService
{
    Task<(string AccessToken, string RefreshToken)> GenerateTokenPairAsync(User user, CancellationToken ct = default);
}
