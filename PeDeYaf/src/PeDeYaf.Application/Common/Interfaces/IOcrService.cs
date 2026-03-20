using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PeDeYaf.Application.Common.Interfaces;

public record OcrLine(string Text, int Page, float Confidence);
public record OcrResult(string FullText, List<OcrLine> Lines, int MaxPage);

public interface IOcrService
{
    Task<OcrResult> ExtractTextAsync(string s3Key, CancellationToken ct = default);
}
