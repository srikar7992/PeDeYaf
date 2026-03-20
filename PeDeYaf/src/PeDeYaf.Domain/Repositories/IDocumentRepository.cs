using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PeDeYaf.Domain.Entities;

namespace PeDeYaf.Domain.Repositories;

/// <summary>
/// Defines repository operations for managing Document entities.
/// </summary>
public interface IDocumentRepository
{
    /// <summary>
    /// Retrieves a document by its unique identifier.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The <see cref="Document"/> if found; otherwise, null.</returns>
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves multiple documents by their identifiers.
    /// </summary>
    Task<IReadOnlyList<Document>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);

    /// <summary>
    /// Adds a new document to the repository.
    /// </summary>
    Task AddAsync(Document document, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing document in the repository.
    /// </summary>
    Task UpdateAsync(Document document, CancellationToken ct = default);

    /// <summary>
    /// Deletes a document from the repository.
    /// </summary>
    Task DeleteAsync(Document document, CancellationToken ct = default);
}
