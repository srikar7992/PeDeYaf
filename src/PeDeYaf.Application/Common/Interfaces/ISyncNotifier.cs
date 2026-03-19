using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PeDeYaf.Application.Common.Interfaces;

public record DocumentReadyPayload(Guid DocumentId, string? ThumbnailUrl, int PageCount);
public record OcrCompletePayload(Guid DocumentId, List<string> Tags);

public interface ISyncNotifier
{
    Task NotifyDocumentReadyAsync(Guid userId, DocumentReadyPayload payload, CancellationToken ct = default);
    Task NotifyOcrCompleteAsync(Guid userId, OcrCompletePayload payload, CancellationToken ct = default);
    Task NotifyDocumentDeletedAsync(Guid userId, Guid documentId, CancellationToken ct = default);
}
