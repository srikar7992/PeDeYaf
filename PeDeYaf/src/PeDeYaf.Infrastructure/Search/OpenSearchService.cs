using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenSearch.Client;
using PeDeYaf.Application.Common.Interfaces;
using PeDeYaf.Domain.Entities;

namespace PeDeYaf.Infrastructure.Search;

public class OpenSearchService(IOpenSearchClient client, ILogger<OpenSearchService> logger) : ISearchIndexService
{
    public async Task IndexDocumentAsync(Document document, CancellationToken ct = default)
    {
        var response = await client.IndexDocumentAsync(document, ct);
        if (!response.IsValid)
        {
            logger.LogWarning("Failed to index document {Id}: {Error}", document.Id, response.DebugInformation);
        }
        else
        {
            logger.LogInformation("Indexed document {Id} in OpenSearch", document.Id);
        }
    }

    public async Task UpdateDocumentAsync(Document document, CancellationToken ct = default)
    {
        var response = await client.UpdateAsync<Document>(document.Id, u => u.Doc(document), ct);
        if (!response.IsValid)
        {
            logger.LogWarning("Failed to update document {Id}: {Error}", document.Id, response.DebugInformation);
        }
    }
}
