namespace SIRH.EY.Models;

/// <summary>
/// Classement figé d'un candidat successeur à un instant T. Une nouvelle exécution du
/// scoring (SuccessionEngine) crée de nouveaux snapshots plutôt que d'écraser les anciens :
/// c'est ce qui donne l'historique demandé (évolution du classement dans le temps),
/// sans dupliquer le mécanisme d'audit générique (AuditLog reste pour les transitions
/// de statut du SuccessionPlan lui-même).
/// </summary>
public class SuccessorRankingSnapshot
{
    public int Id { get; set; }

    public int SuccessionPlanId { get; set; }
    public SuccessionPlan? SuccessionPlan { get; set; }

    public int CandidatId { get; set; }
    public Collaborateur? Candidat { get; set; }

    /// <summary>1 = premier choix, 2 = deuxième choix, etc.</summary>
    public int Rang { get; set; }

    // Scores figés au moment du snapshot — recalculés par SuccessionEngine.Score(),
    // jamais réécrits après coup (un nouveau run = un nouveau snapshot).
    public int ScoreSuccession { get; set; }
    public int ScoreCouverture { get; set; }

    public ReadinessHorizon Horizon { get; set; }

    public DateTime DateSnapshot { get; set; } = DateTime.Now;
}

public enum ReadinessHorizon
{
    ReadyNow = 0,           // 0-6 mois
    Ready6To12Months = 1,
    Ready12To24Months = 2,
    NotReady = 3
}
