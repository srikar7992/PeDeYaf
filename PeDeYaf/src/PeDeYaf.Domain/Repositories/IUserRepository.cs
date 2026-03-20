using System;
using System.Threading;
using System.Threading.Tasks;
using PeDeYaf.Domain.Entities;

namespace PeDeYaf.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> FindByPhoneAsync(string phone, CancellationToken ct = default);
    Task<User> CreateAsync(string phone, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}
