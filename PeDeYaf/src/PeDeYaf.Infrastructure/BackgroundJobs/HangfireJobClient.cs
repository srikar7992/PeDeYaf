using System;
using System.Collections.Generic;
using Hangfire;
using PeDeYaf.Application.Documents.Commands;

namespace PeDeYaf.Infrastructure.BackgroundJobs;

public class HangfireJobClient(IBackgroundJobClient jobClient) : IBackgroundJobClient, IPdfMergeJobClient
{
    public void Enqueue(string jobName, Guid documentId)
    {
        if (jobName == "PdfProcessingJob")
        {
            jobClient.Enqueue<PdfProcessingJob>(job => job.ProcessAsync(documentId, JobCancellationToken.Null));
        }
    }

    public void EnqueueMerge(Guid outputDocId, List<Guid> sourceDocIds, string outputKey)
    {
        jobClient.Enqueue<PdfMergeJob>(job => job.MergeAsync(outputDocId, sourceDocIds, outputKey, JobCancellationToken.Null));
    }
}
