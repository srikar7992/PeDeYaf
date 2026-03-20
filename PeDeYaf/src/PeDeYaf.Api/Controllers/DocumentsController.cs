using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeDeYaf.Application.Documents.Commands;

namespace PeDeYaf.Api.Controllers;

/// <summary>
/// Handles HTTP requests for document management, including uploads, confirmation, and merging.
/// </summary>
[Authorize]
[ApiController]
[Route("v1/[controller]")]
public class DocumentsController(IMediator mediator) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Generates a presigned S3 upload URL for a new document.
    /// </summary>
    /// <param name="request">The upload parameters.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A presigned URL to securely upload the PDF.</returns>
    [HttpPost("upload-url")]
    public async Task<IActionResult> GenerateUploadUrl([FromBody] GenerateUploadUrlRequest request, CancellationToken ct)
    {
        var command = new GenerateUploadUrlCommand(UserId, request.FileName, request.ContentType, request.FolderId);
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Confirms that a document upload to S3 has completed successfully, triggering background processing.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A processing acknowledgment.</returns>
    [HttpPost("{id:guid}/confirm-upload")]
    public async Task<IActionResult> ConfirmUpload(Guid id, CancellationToken ct)
    {
        var command = new ConfirmUploadCommand(id, UserId);
        await mediator.Send(command, ct);
        return Ok(new { documentId = id, status = "processing" });
    }

    /// <summary>
    /// Merges multiple uploaded PDF documents into a single new document.
    /// </summary>
    /// <param name="command">The merge command containing source IDs and output name.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The newly created merged document metadata.</returns>
    [HttpPost("merge")]
    public async Task<IActionResult> Merge([FromBody] MergeDocumentsCommand command, CancellationToken ct)
    {
        // Force correct user ID
        var safeCommand = command with { UserId = UserId };
        var result = await mediator.Send(safeCommand, ct);
        return Ok(result);
    }
}

public record GenerateUploadUrlRequest(string FileName, string ContentType, Guid? FolderId);
