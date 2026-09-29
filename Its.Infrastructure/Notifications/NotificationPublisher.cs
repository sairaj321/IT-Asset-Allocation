using Its.Application.Interfaces;
using Its.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Its.Infrastructure.Notifications;

public interface INotificationClient
{
    Task ReceiveNotification(object notification);
    Task RequestStatusUpdated(object requestUpdate);
    Task AssetStatusUpdated(object assetUpdate);
}

[Authorize]
public class NotificationHub : Hub<INotificationClient>
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? Context.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        var roles = Context.User?.FindAll(ClaimTypes.Role).Select(c => c.Value);
        if (roles != null)
        {
            foreach (var role in roles)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"role-{role}");
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                     ?? Context.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}

public class NotificationPublisher : INotificationPublisher
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly IUnitOfWork _uow;

    public NotificationPublisher(IHubContext<NotificationHub, INotificationClient> hubContext, IUnitOfWork uow)
    {
        _hubContext = hubContext;
        _uow = uow;
    }

    public async Task PublishNotificationAsync(long userId, string title, string message, string type, string? relatedEntity = null, long? relatedId = null)
    {
        try
        {
            var notif = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                RelatedEntity = relatedEntity,
                RelatedId = relatedId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Notifications.AddAsync(notif);
            await _uow.SaveChangesAsync();

            var payload = new
            {
                notif.NotificationId,
                notif.Title,
                notif.Message,
                notif.Type,
                notif.RelatedEntity,
                notif.RelatedId,
                notif.IsRead,
                notif.CreatedAt
            };

            await _hubContext.Clients.Group($"user-{userId}").ReceiveNotification(payload);
        }
        catch
        {
            // Do not fail main transaction if websocket push fails
        }
    }

    public async Task PublishNotificationToRoleAsync(string roleName, string title, string message, string type, string? relatedEntity = null, long? relatedId = null)
    {
        try
        {
            var role = await _uow.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role != null)
            {
                var userRoles = await _uow.UserRoles.FindAsync(ur => ur.RoleId == role.RoleId);
                foreach (var ur in userRoles)
                {
                    var notif = new Notification
                    {
                        UserId = ur.UserId,
                        Title = title,
                        Message = message,
                        Type = type,
                        RelatedEntity = relatedEntity,
                        RelatedId = relatedId,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _uow.Notifications.AddAsync(notif);
                }
                await _uow.SaveChangesAsync();
            }

            var payload = new
            {
                Title = title,
                Message = message,
                Type = type,
                RelatedEntity = relatedEntity,
                RelatedId = relatedId,
                CreatedAt = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"role-{roleName}").ReceiveNotification(payload);
        }
        catch
        {
            // Fallback gracefully
        }
    }
}
