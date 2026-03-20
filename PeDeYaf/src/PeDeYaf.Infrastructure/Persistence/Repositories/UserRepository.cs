using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PeDeYaf.Domain.Entities;
using PeDeYaf.Domain.Repositories;

namespace PeDeYaf.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> FindByPhoneAsync(string phone, CancellationToken ct = default)
    {
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Phone == phone, ct);
    }

    public async Task<User> CreateAsync(string phone, CancellationToken ct = default)
    {
        var user = User.Create(phone);
        await dbContext.Users.AddAsync(user, ct);
        await dbContext.SaveChangesAsync(ct);
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(ct);
    }
}
