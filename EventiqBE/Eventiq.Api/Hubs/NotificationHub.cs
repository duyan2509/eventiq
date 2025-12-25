using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Eventiq.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    public async Task LeaveUserGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    public async Task JoinEventGroup(string eventId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"event_{eventId}");
    }

    public async Task LeaveEventGroup(string eventId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"event_{eventId}");
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await JoinUserGroup(userId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst("userId")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await LeaveUserGroup(userId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}

