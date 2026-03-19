using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PeDeYaf.Domain.Entities;

namespace PeDeYaf.Domain.Repositories;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Document>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task AddAsync(Document document, CancellationToken ct = default);
    Task UpdateAsync(Document document, CancellationToken ct = default);
    Task DeleteAsync(Document document, CancellationToken ct = default);
}
