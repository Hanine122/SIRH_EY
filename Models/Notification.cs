using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

// Minimal internal notification — no external system exists yet (PowerAutomateService
// only triggers external flows). Recipient is always a Collaborateur.
public class Notification
{
    public int Id { get; set; }

    public int CollaborateurId { get; set; }
    public Collaborateur? Collaborateur { get; set; }

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    // Local URL the notification should deep-link to (e.g. Formations/Details?id=..).
    [MaxLength(300)]
    public string? Lien { get; set; }

    public bool Lu { get; set; } = false;

    public DateTime DateCreation { get; set; } = DateTime.Now;
}
