using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using PeDeYaf.Domain.Entities;
using PeDeYaf.Domain.Exceptions;
using PeDeYaf.Domain.Repositories;

namespace PeDeYaf.Application.Documents.Commands;

public record DocumentDto(Guid Id, string Name, string Status, int PageCount, DateTimeOffset CreatedAt);

public record MergeDocumentsCommand(
    Guid UserId,
    IReadOnlyList<Guid> DocumentIds,
    string OutputName
) : IRequest<DocumentDto>;

public interface IPdfMergeJobClient
{
    void EnqueueMerge(Guid outputDocId, List<Guid> sourceDocIds, string outputKey);
}

public class MergeDocumentsHandler(
    IDocumentRepository documents,
    IPdfMergeJobClient jobs)
    : IRequestHandler<MergeDocumentsCommand, DocumentDto>
{
    public async Task<DocumentDto> Handle(MergeDocumentsCommand request, CancellationToken ct)
    {
        if (request.DocumentIds.Count < 2)
            throw new ValidationException("Need at least 2 documents to merge");
        if (request.DocumentIds.Count > 20)
            throw new ValidationException("Cannot merge more than 20 documents at once");

        var docs = await documents.GetByIdsAsync(request.DocumentIds, ct);

        // Validate ownership of all documents
        if (docs.Any(d => d.UserId != request.UserId))
            throw new ForbiddenException("One or more documents do not belong to you");

        // Create output document in PROCESSING state
        var outputKey = $"documents/{request.UserId}/{Guid.NewGuid()}/{request.OutputName}.pdf";
        var outputDoc = Document.Create(request.UserId, request.OutputName + ".pdf", outputKey, 0);
        await documents.AddAsync(outputDoc, ct);

        // Enqueue actual merge job
        jobs.EnqueueMerge(
            outputDoc.Id,
            request.DocumentIds.ToList(),
            outputKey);

        return new DocumentDto(outputDoc.Id, outputDoc.Name, outputDoc.Status.ToString(), outputDoc.PageCount, outputDoc.CreatedAt);
    }
}
