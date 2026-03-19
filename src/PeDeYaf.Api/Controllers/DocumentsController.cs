using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeDeYaf.Application.Documents.Commands;

namespace PeDeYaf.Api.Controllers;

[Authorize]
[ApiController]
[Route("v1/[controller]")]
public class DocumentsController(IMediator mediator) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("upload-url")]
    public async Task<IActionResult> GenerateUploadUrl([FromBody] GenerateUploadUrlRequest request, CancellationToken ct)
    {
        var command = new GenerateUploadUrlCommand(UserId, request.FileName, request.ContentType, request.FolderId);
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/confirm-upload")]
    public async Task<IActionResult> ConfirmUpload(Guid id, CancellationToken ct)
    {
        var command = new ConfirmUploadCommand(id, UserId);
        await mediator.Send(command, ct);
        return Ok(new { documentId = id, status = "processing" });
    }

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
