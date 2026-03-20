using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PeDeYaf.Application.Common.Interfaces;
using PeDeYaf.Domain.Repositories;
using PeDeYaf.Infrastructure.Persistence;

namespace PeDeYaf.Infrastructure.BackgroundJobs;

public class PdfMergeJob(
    IDocumentRepository documents,
    IFileStorage fileStorage,
    ISyncNotifier syncNotifier,
    AppDbContext db)
{
    [JobDisplayName("Merge PDFs into: {0}")]
    [AutomaticRetry(Attempts = 2)]
    public async Task MergeAsync(
        Guid outputDocId,
        List<Guid> sourceDocIds,
        string outputKey,
        IJobCancellationToken ct)
    {
        var sourceDocs = await documents.GetByIdsAsync(sourceDocIds, ct.ShutdownToken);
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var localPaths = new List<string>();
        try
        {
            foreach (var docId in sourceDocIds)
            {
                var doc = sourceDocs.First(d => d.Id == docId);
                var localPath = Path.Combine(tempDir, $"{doc.Id}.pdf");
                await using var stream = await fileStorage.DownloadAsync(doc.S3Key, ct.ShutdownToken);
                await using var dest = File.Create(localPath);
                await stream.CopyToAsync(dest, ct.ShutdownToken);
                localPaths.Add(localPath);
            }

            var outputPath = Path.Combine(tempDir, "merged.pdf");
            using (var outputDoc = new PdfDocument())
            {
                foreach (var path in localPaths)
                {
                    using var inputDoc = PdfReader.Open(path, PdfDocumentOpenMode.Import);
                    for (int i = 0; i < inputDoc.PageCount; i++)
                        outputDoc.AddPage(inputDoc.Pages[i]);
                }
                outputDoc.Save(outputPath);
            }

            await using var mergedStream = File.OpenRead(outputPath);
            await fileStorage.UploadAsync(outputKey, mergedStream, "application/pdf", ct.ShutdownToken);

            var outputDocument = await documents.GetByIdAsync(outputDocId, ct.ShutdownToken);
            using (var merged = PdfReader.Open(outputPath, PdfDocumentOpenMode.InformationOnly))
            {
                outputDocument!.MarkProcessed(merged.PageCount, null, null);
            }

            await db.SaveChangesAsync(ct.ShutdownToken);
            await syncNotifier.NotifyDocumentReadyAsync(outputDocument!.UserId, new DocumentReadyPayload(outputDocId, null, outputDocument.PageCount), ct.ShutdownToken);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}
