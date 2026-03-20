using System;
using System.Threading;
using System.Threading.Tasks;

namespace PeDeYaf.Application.Common.Interfaces;

public interface ICacheService
{
    Task<string?> GetStringAsync(string key, CancellationToken ct = default);
    Task SetStringAsync(string key, string value, TimeSpan? absoluteExpirationRelativeToNow = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}
