using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PeDeYaf.Domain.Entities;

namespace PeDeYaf.Domain.Repositories;

public interface IFolderRepository
{
    Task<Folder?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Folder>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Folder folder, CancellationToken ct = default);
    Task UpdateAsync(Folder folder, CancellationToken ct = default);
    Task DeleteAsync(Folder folder, CancellationToken ct = default);
}
