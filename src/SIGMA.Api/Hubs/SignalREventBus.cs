using Microsoft.AspNetCore.SignalR;
using SIGMA.Application.Abstractions;

namespace SIGMA.Api.Hubs;

internal sealed class SignalREventBus(IHubContext<SigmaHub> hubContext) : IEventBus
{
    public async Task PublishAsync<T>(string eventType, T data, CancellationToken ct = default) where T : class
    {
        await hubContext.Clients.Group("all").SendAsync("entityUpdated", new
        {
            type = eventType,
            data,
            timestamp = DateTimeOffset.UtcNow
        }, ct);
    }

    public async Task PublishToGroupAsync<T>(string groupName, string eventType, T data, CancellationToken ct = default) where T : class
    {
        await hubContext.Clients.Group(groupName).SendAsync("entityUpdated", new
        {
            type = eventType,
            data,
            timestamp = DateTimeOffset.UtcNow
        }, ct);
    }
}
