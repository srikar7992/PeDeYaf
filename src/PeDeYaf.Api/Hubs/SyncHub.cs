using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PeDeYaf.Application.Common.Interfaces;

namespace PeDeYaf.Api.Hubs;

[Authorize]
public class SyncHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Group by userId - allows targeting all devices of a user
        var userId = Context.UserIdentifier!;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        await base.OnConnectedAsync();
    }
}

public class SyncNotifier(IHubContext<SyncHub> hubContext) : ISyncNotifier
{
    public async Task NotifyDocumentReadyAsync(Guid userId, DocumentReadyPayload payload, CancellationToken ct = default)
    {
        await hubContext.Clients.Group($"user:{userId}").SendAsync("DocumentReady", payload, ct);
    }

    public async Task NotifyOcrCompleteAsync(Guid userId, OcrCompletePayload payload, CancellationToken ct = default)
    {
        await hubContext.Clients.Group($"user:{userId}").SendAsync("OcrComplete", payload, ct);
    }

    public async Task NotifyDocumentDeletedAsync(Guid userId, Guid documentId, CancellationToken ct = default)
    {
        await hubContext.Clients.Group($"user:{userId}").SendAsync("DocumentDeleted", documentId, ct);
    }
}
