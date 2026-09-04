using SIRH.EY.Data;
using SIRH.EY.Models;

namespace SIRH.EY.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task NotifyAsync(int collaborateurId, string message, string? lien = null)
    {
        _context.Notifications.Add(new Notification
        {
            CollaborateurId = collaborateurId,
            Message = message,
            Lien = lien
        });
        return Task.CompletedTask;
    }
}
