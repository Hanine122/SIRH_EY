namespace SIRH.EY.Services;

public interface INotificationService
{
    // Does not call SaveChangesAsync — mirrors ICompetenceLifecycleService's convention
    // so the notification is persisted in the same transaction as the caller's changes.
    Task NotifyAsync(int collaborateurId, string message, string? lien = null);
}
