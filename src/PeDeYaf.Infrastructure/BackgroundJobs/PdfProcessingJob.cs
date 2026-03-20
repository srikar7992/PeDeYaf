using System;
using System.IO;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PeDeYaf.Application.Common.Interfaces;
using PeDeYaf.Domain.Repositories;
using PeDeYaf.Infrastructure.Persistence;

namespace PeDeYaf.Infrastructure.BackgroundJobs;

public class PdfProcessingJob(
    IDocumentRepository documents,
    IFileStorage fileStorage,
    ISearchIndexService searchIndex,
    ISyncNotifier syncNotifier,
    AppDbContext db,
    ILogger<PdfProcessingJob> logger)
{
    [JobDisplayName("Process PDF: {0}")]
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [30, 120, 300])]
    public async Task ProcessAsync(Guid documentId, IJobCancellationToken ct)
    {
        var document = await documents.GetByIdAsync(documentId, ct.ShutdownToken)
                       ?? throw new InvalidOperationException($"Document {documentId} not found");

        logger.LogInformation("Processing document {Id}: {Name}", documentId, document.Name);

        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
        
        try
        {
            await using var stream = await fileStorage.DownloadAsync(document.S3Key, ct.ShutdownToken);
            await using var file = File.Create(tempPath);
            await stream.CopyToAsync(file, ct.ShutdownToken);
            await file.DisposeAsync();

            int pageCount;
            using (var pdfDoc = PdfReader.Open(tempPath, PdfDocumentOpenMode.InformationOnly))
            {
                pageCount = pdfDoc.PageCount;
            }

            // Thumbnail generation omitted for brevity or replaced by ghostscript call
            var thumbnailKey = document.S3Key.Replace(".pdf", "_thumb.jpg");

            if (pageCount <= 100)
            {
                BackgroundJob.Enqueue<OcrJob>(
                    job => job.ExtractTextAsync(documentId, tempPath, JobCancellationToken.Null));
            }

            document.MarkProcessed(pageCount, thumbnailKey, null);
            await db.SaveChangesAsync(ct.ShutdownToken);

            await searchIndex.IndexDocumentAsync(document, ct.ShutdownToken);

            await syncNotifier.NotifyDocumentReadyAsync(document.UserId, new DocumentReadyPayload(document.Id, thumbnailKey, pageCount), ct.ShutdownToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process document {Id}", documentId);
            document.MarkFailed();
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}
