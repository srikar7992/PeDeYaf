using System;
using System.Threading.Tasks;
using Hangfire;
using PeDeYaf.Application.Common.Interfaces;
using PeDeYaf.Domain.Repositories;
using PeDeYaf.Infrastructure.Persistence;

namespace PeDeYaf.Infrastructure.BackgroundJobs;

public class OcrJob(
    IDocumentRepository documents,
    IOcrService ocrService,
    IAiService aiService,
    ISearchIndexService searchIndex,
    ISyncNotifier syncNotifier,
    AppDbContext db)
{
    [JobDisplayName("OCR + AI Tagging: {0}")]
    [AutomaticRetry(Attempts = 2)]
    [Queue("ocr")]
    public async Task ExtractTextAsync(Guid documentId, string localTempPath, IJobCancellationToken ct)
    {
        var document = await documents.GetByIdAsync(documentId, ct.ShutdownToken)
                       ?? throw new InvalidOperationException();

        var ocrResult = await ocrService.ExtractTextAsync(document.S3Key, ct.ShutdownToken);

        var aiTags = await aiService.GenerateTagsAsync(
            document.Name,
            ocrResult.FullText[..Math.Min(4000, ocrResult.FullText.Length)],
            ct.ShutdownToken);

        document.MarkOcrComplete(ocrResult.FullText);
        document.ApplyTags(aiTags);

        await db.SaveChangesAsync(ct.ShutdownToken);
        await searchIndex.UpdateDocumentAsync(document, ct.ShutdownToken);

        await syncNotifier.NotifyOcrCompleteAsync(document.UserId, new OcrCompletePayload(documentId, aiTags), ct.ShutdownToken);
    }
}
