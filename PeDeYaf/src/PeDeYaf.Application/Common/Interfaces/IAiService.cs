using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PeDeYaf.Application.Common.Interfaces;

public interface IAiService
{
    Task<List<string>> GenerateTagsAsync(string fileName, string textSample, CancellationToken ct = default);
    Task<string> SummarizePdfAsync(string extractedText, int maxWords, CancellationToken ct = default);
}
