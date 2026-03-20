using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Textract;
using Amazon.Textract.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PeDeYaf.Application.Common.Interfaces;

namespace PeDeYaf.Infrastructure.Ocr;

public class TextractOcrService(
    IAmazonTextract textract,
    IConfiguration configuration,
    ILogger<TextractOcrService> logger) : IOcrService
{
    private readonly string _bucket = configuration["AWS:S3:BucketName"] ?? "pedeyaf-storage";
    private readonly string _roleArn = configuration["AWS:Textract:RoleArn"] ?? "";
    private readonly string _snsTopicArn = configuration["AWS:Textract:SnsTopicArn"] ?? "";

    public async Task<OcrResult> ExtractTextAsync(string s3Key, CancellationToken ct = default)
    {
        var startRequest = new StartDocumentTextDetectionRequest
        {
            DocumentLocation = new DocumentLocation
            {
                S3Object = new S3Object { Bucket = _bucket, Name = s3Key },
            },
            NotificationChannel = string.IsNullOrEmpty(_roleArn) ? null : new NotificationChannel
            {
                RoleArn = _roleArn,
                SNSTopicArn = _snsTopicArn,
            },
        };

        var startResponse = await textract.StartDocumentTextDetectionAsync(startRequest, ct);
        var jobId = startResponse.JobId;

        // Simple polling instead of SNS callback for simplicity in prod-ready snippet
        return await PollForResultAsync(jobId, ct);
    }

    private async Task<OcrResult> PollForResultAsync(string jobId, CancellationToken ct)
    {
        var allBlocks = new List<Block>();
        string? nextToken = null;

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(3), ct);

            var resultRequest = new GetDocumentTextDetectionRequest
            {
                JobId = jobId,
                NextToken = nextToken,
            };

            var result = await textract.GetDocumentTextDetectionAsync(resultRequest, ct);

            if (result.JobStatus == JobStatus.FAILED)
                throw new Exception($"Textract job {jobId} failed");

            if (result.JobStatus == JobStatus.IN_PROGRESS)
                continue;

            allBlocks.AddRange(result.Blocks);
            nextToken = result.NextToken;

            if (nextToken is null) break;
        }

        var lineBlocks = allBlocks.Where(b => b.BlockType == BlockType.LINE).ToList();
        var lines = lineBlocks
            .OrderBy(b => b.Page)
            .ThenBy(b => b.Geometry.BoundingBox.Top)
            .Select(b => new OcrLine(b.Text, b.Page, b.Confidence))
            .ToList();

        var fullText = string.Join("\n", lines.Select(l => l.Text));
        var maxPage = allBlocks.Count > 0 ? allBlocks.Max(b => b.Page) : 0;

        return new OcrResult(fullText, lines, maxPage);
    }
}
