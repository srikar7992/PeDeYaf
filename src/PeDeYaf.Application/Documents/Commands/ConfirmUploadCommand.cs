using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PeDeYaf.Domain.Exceptions;
using PeDeYaf.Domain.Repositories;

namespace PeDeYaf.Application.Documents.Commands;

public interface IBackgroundJobClient
{
    void Enqueue(string jobName, Guid documentId);
}

public record ConfirmUploadCommand(Guid DocumentId, Guid UserId) : IRequest<Unit>;

public class ConfirmUploadHandler(
    IDocumentRepository documents,
    IBackgroundJobClient jobClient)
    : IRequestHandler<ConfirmUploadCommand, Unit>
{
    public async Task<Unit> Handle(ConfirmUploadCommand request, CancellationToken ct)
    {
        var doc = await documents.GetByIdAsync(request.DocumentId, ct)
                  ?? throw new DocumentNotFoundException(request.DocumentId);

        if (doc.UserId != request.UserId)
            throw new ForbiddenException();

        // Queue background processing - fire and forget
        jobClient.Enqueue("PdfProcessingJob", request.DocumentId);

        return Unit.Value;
    }
}
