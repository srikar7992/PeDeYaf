using System.Threading;
using System.Threading.Tasks;
using PeDeYaf.Domain.Entities;

namespace PeDeYaf.Application.Common.Interfaces;

public interface ISearchIndexService
{
    Task IndexDocumentAsync(Document document, CancellationToken ct = default);
    Task UpdateDocumentAsync(Document document, CancellationToken ct = default);
}
