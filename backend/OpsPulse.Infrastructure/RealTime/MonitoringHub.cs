using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpsPulse.Domain.Common;

namespace OpsPulse.Infrastructure.RealTime;

/// <summary>
/// Organization-scoped real-time hub. Connections authenticate with JWT
/// (access_token via query string for WebSockets) and join their organization
/// group automatically; SuperAdmins also join the platform group.
/// </summary>
[Authorize]
public sealed class MonitoringHub : Hub
{
    public const string PlatformGroup = "platform";

    public static string GroupName(Guid organizationId) => $"org:{organizationId}";

    public override async Task OnConnectedAsync()
    {
        if (Context.User?.IsInRole(RoleNames.SuperAdmin) == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, PlatformGroup);
        }

        if (Context.User?.FindFirst("org")?.Value is { Length: > 0 } organizationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(Guid.Parse(organizationId)));
        }

        await base.OnConnectedAsync();
    }
}
