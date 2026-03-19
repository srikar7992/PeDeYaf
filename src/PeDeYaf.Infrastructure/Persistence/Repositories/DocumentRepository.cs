using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PeDeYaf.Domain.Entities;
using PeDeYaf.Domain.Repositories;

namespace PeDeYaf.Infrastructure.Persistence.Repositories;

public class DocumentRepository(AppDbContext dbContext) : IDocumentRepository
{
    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await dbContext.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<IReadOnlyList<Document>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        return await dbContext.Documents.Where(d => ids.Contains(d.Id)).ToListAsync(ct);
    }

    public async Task AddAsync(Document document, CancellationToken ct = default)
    {
        await dbContext.Documents.AddAsync(document, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Document document, CancellationToken ct = default)
    {
        dbContext.Documents.Update(document);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Document document, CancellationToken ct = default)
    {
        dbContext.Documents.Remove(document);
        await dbContext.SaveChangesAsync(ct);
    }
}
