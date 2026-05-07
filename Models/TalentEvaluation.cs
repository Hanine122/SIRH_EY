using System.ComponentModel.DataAnnotations;

namespace SIRH.EY.Models;

/// <summary>
/// Évaluation talent pour la matrice 9-box
/// </summary>
public class TalentEvaluation
{
    public int Id { get; set; }
    
    public int CollaborateurId { get; set; }
    public Collaborateur? Collaborateur { get; set; }
    
    // Performance (1-5)
    [Range(1, 5)]
    public int PerformanceScore { get; set; } = 3;
    
    // Potentiel (1-5)
    [Range(1, 5)]
    public int PotentielScore { get; set; } = 3;
    
    // Catégorie 9-box calculée
    public NineBoxCategory Category { get; set; } = NineBoxCategory.SolidProfessional;
    
    // Détails
    public string? CommentairesPerformance { get; set; }
    public string? CommentairesPotentiel { get; set; }
    
    // Évaluateur
    public string? EvaluateurId { get; set; }
    public ApplicationUser? Evaluateur { get; set; }
    
    public DateTime DateEvaluation { get; set; } = DateTime.Now;
    public bool Actif { get; set; } = true;
}

public enum NineBoxCategory
{
    Star = 1,                    // High Perf / High Pot
    FutureLeader = 2,           // High Perf / Medium Pot
    HighProfessional = 3,       // High Perf / Low Pot
    EmergingTalent = 4,       // Medium Perf / High Pot
    SolidProfessional = 5,      // Medium Perf / Medium Pot
    InPlace = 6,               // Medium Perf / Low Pot
    RisingStar = 7,           // Low Perf / High Pot
    NeedDevelopment = 8,       // Low Perf / Medium Pot
    Underperformer = 9         // Low Perf / Low Pot
}

public static class NineBoxExtensions
{
    public static string GetDisplayName(this NineBoxCategory category)
    {
        return category switch
        {
            NineBoxCategory.Star => "⭐ Star (Leader)",
            NineBoxCategory.FutureLeader => "🚀 Future Leader",
            NineBoxCategory.HighProfessional => "💎 High Professional",
            NineBoxCategory.EmergingTalent => "🌱 Emerging Talent",
            NineBoxCategory.SolidProfessional => "✅ Solid Professional",
            NineBoxCategory.InPlace => "📍 In Place",
            NineBoxCategory.RisingStar => "⭐ Rising Star",
            NineBoxCategory.NeedDevelopment => "📈 Needs Development",
            NineBoxCategory.Underperformer => "⚠️ Underperformer",
            _ => "Unknown"
        };
    }
    
    public static string GetColorClass(this NineBoxCategory category)
    {
        return category switch
        {
            NineBoxCategory.Star => "box-star",
            NineBoxCategory.FutureLeader => "box-future",
            NineBoxCategory.HighProfessional => "box-pro",
            NineBoxCategory.EmergingTalent => "box-emerging",
            NineBoxCategory.SolidProfessional => "box-solid",
            NineBoxCategory.InPlace => "box-inplace",
            NineBoxCategory.RisingStar => "box-rising",
            NineBoxCategory.NeedDevelopment => "box-development",
            NineBoxCategory.Underperformer => "box-risk",
            _ => "box-default"
        };
    }
}
