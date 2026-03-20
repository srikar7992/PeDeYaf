using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PeDeYaf.Domain.Entities;
using PeDeYaf.Domain.Repositories;

namespace PeDeYaf.Infrastructure.Persistence.Repositories;

public class FolderRepository(AppDbContext dbContext) : IFolderRepository
{
    public async Task<Folder?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.Folders.FirstOrDefaultAsync(f => f.Id == id, ct);
    }

    public async Task<IReadOnlyList<Folder>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await dbContext.Folders.Where(f => f.UserId == userId).ToListAsync(ct);
    }

    public async Task AddAsync(Folder folder, CancellationToken ct = default)
    {
        await dbContext.Folders.AddAsync(folder, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Folder folder, CancellationToken ct = default)
    {
        dbContext.Folders.Update(folder);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Folder folder, CancellationToken ct = default)
    {
        dbContext.Folders.Remove(folder);
        await dbContext.SaveChangesAsync(ct);
    }
}
