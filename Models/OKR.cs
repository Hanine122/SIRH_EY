using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

/// <summary>
/// Objectives and Key Results (OKR) pour les collaborateurs
/// </summary>
public class OKR
{
    public int Id { get; set; }
    
    public int CollaborateurId { get; set; }
    public Collaborateur? Collaborateur { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Objectif { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    // Période
    public int Annee { get; set; } = DateTime.Now.Year;
    public Trimestre Trimestre { get; set; } = Trimestre.Q1;
    
    // Statut
    public OKRStatut Statut { get; set; } = OKRStatut.Draft;
    
    // Progression globale (calculée)
    [Range(0, 100)]
    public int ProgressionGlobale { get; set; } = 0;
    
    // Dates
    public DateTime DateDebut { get; set; } = DateTime.Now;
    public DateTime DateFinCible { get; set; } = DateTime.Now.AddMonths(3);
    public DateTime? DateRealisation { get; set; }
    
    // Validation
    public string? ManagerId { get; set; }
    public ApplicationUser? Manager { get; set; }
    public bool ValideParManager { get; set; } = false;
    public DateTime? DateValidation { get; set; }
    
    // Navigation
    public ICollection<KeyResult> KeyResults { get; set; } = new List<KeyResult>();
}

public class KeyResult
{
    public int Id { get; set; }
    
    public int OKRId { get; set; }
    public OKR? OKR { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Description { get; set; } = string.Empty;
    
    // Cible et actuel
    public double ValeurCible { get; set; } = 100;
    public double ValeurActuelle { get; set; } = 0;
    public string? Unite { get; set; } // %, nombre, etc.
    
    // Progression calculée
    [Range(0, 100)]
    public int Progression => ValeurCible > 0 
        ? (int)Math.Min(100, (ValeurActuelle / ValeurCible) * 100) 
        : 0;
    
    // Difficulté
    public KeyResultDifficulty Difficulte { get; set; } = KeyResultDifficulty.Medium;
    
    // Statut
    public KeyResultStatut Statut { get; set; } = KeyResultStatut.NotStarted;
    
    public int Ordre { get; set; } = 0;
}

public enum Trimestre
{
    Q1 = 1,
    Q2 = 2,
    Q3 = 3,
    Q4 = 4
}

public enum OKRStatut
{
    Draft = 0,
    Active = 1,
    OnTrack = 2,
    AtRisk = 3,
    Completed = 4,
    Cancelled = 5
}

public enum KeyResultStatut
{
    NotStarted = 0,
    InProgress = 1,
    AtRisk = 2,
    Completed = 3,
    Cancelled = 4
}

public enum KeyResultDifficulty
{
    Easy = 1,
    Medium = 2,
    Hard = 3,
    Stretch = 4
}

public static class OKRStatutExtensions
{
    public static string GetDisplayName(this OKRStatut statut)
    {
        return statut switch
        {
            OKRStatut.Draft => "📝 Brouillon",
            OKRStatut.Active => "🟢 Actif",
            OKRStatut.OnTrack => "✅ On Track",
            OKRStatut.AtRisk => "⚠️ At Risk",
            OKRStatut.Completed => "🎉 Complété",
            OKRStatut.Cancelled => "❌ Annulé",
            _ => "Unknown"
        };
    }
    
    public static string GetColorClass(this OKRStatut statut)
    {
        return statut switch
        {
            OKRStatut.Draft => "status-draft",
            OKRStatut.Active => "status-active",
            OKRStatut.OnTrack => "status-ontrack",
            OKRStatut.AtRisk => "status-atrisk",
            OKRStatut.Completed => "status-completed",
            OKRStatut.Cancelled => "status-cancelled",
            _ => "status-default"
        };
    }
}
