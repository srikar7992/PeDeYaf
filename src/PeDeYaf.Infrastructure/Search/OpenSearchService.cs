using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PeDeYaf.Application.Common.Interfaces;
using PeDeYaf.Domain.Entities;

namespace PeDeYaf.Infrastructure.Search;

// Mock implementation of OpenSearch for this exercise since we are focusing on clean architecture
// In real world, use NEST or OpenSearch.Client
public class OpenSearchService(ILogger<OpenSearchService> logger) : ISearchIndexService
{
    public Task IndexDocumentAsync(Document document, CancellationToken ct = default)
    {
        logger.LogInformation("Indexed document {Id} in OpenSearch", document.Id);
        return Task.CompletedTask;
    }

    public Task UpdateDocumentAsync(Document document, CancellationToken ct = default)
    {
        logger.LogInformation("Updated document {Id} in OpenSearch", document.Id);
        return Task.CompletedTask;
    }
}
