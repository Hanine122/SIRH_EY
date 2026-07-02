namespace SIRH.EY.Services;

/// <summary>
/// Decision Layer unique — seul endroit du code où un score numérique est converti en
/// étiquette/décision RH (sévérité, priorité, niveau de risque, type de successeur).
/// Avant extraction, chacune de ces classifications était dupliquée sous forme de ternaires
/// en ligne dans PromotionReadinessService et WorkforceImpactService. Les Engines de calcul
/// (PromotionReadinessEngine, WorkforceImpactEngine, SuccessionEngine, TalentScoringEngine)
/// produisent des nombres ; DecisionEngine est la seule couche qui les transforme en décision.
/// </summary>
public static class DecisionEngine
{
    // ── Écarts de compétence (PromotionReadinessService) ──────────────────────

    /// <summary>Sévérité d'un écart de compétence. Source : PromotionReadinessService (champ Severity).</summary>
    public static string ClassifyGapSeverity(int gap) =>
        gap >= 3 ? "High" : gap == 2 ? "Medium" : gap == 1 ? "Low" : "Ready";

    /// <summary>Étiquette de priorité (FR) d'un écart. Source : PromotionReadinessService (champ PriorityLabel).</summary>
    public static string ClassifyGapPriorityLabel(int gap) =>
        gap >= 3 ? "Critique" : gap == 2 ? "Prioritaire" : gap == 1 ? "A renforcer" : "Couvert";

    /// <summary>Texte d'impact d'une formation recommandée selon l'écart qu'elle comble.</summary>
    public static string ClassifyGapProgressionImpact(int gap) =>
        gap >= 3 ? "Impact critique sur la readiness"
        : gap == 2 ? "Impact eleve sur la compatibilite"
        : "Impact rapide de consolidation";

    // ── Succession / continuité (WorkforceImpactService) ──────────────────────

    /// <summary>Type de successeur selon son score de readiness (0-100).</summary>
    public static string ClassifySuccessorType(double readinessScore) =>
        readinessScore >= 75 ? "Immediate successor" : readinessScore >= 45 ? "Partial successor" : "High potential";

    /// <summary>Niveau de risque de dépendance stratégique (0-100).</summary>
    public static string ClassifyRiskLevel(double strategicDependencyScore) =>
        strategicDependencyScore >= 75 ? "Critical" : strategicDependencyScore >= 55 ? "Elevated" : "Controlled";

    /// <summary>Signal d'exposition d'un département (0-100).</summary>
    public static string ClassifyExposureSignal(double exposureScore) =>
        exposureScore >= 70 ? "Forte dependance" : exposureScore >= 45 ? "Fragilite a surveiller" : "Exposition controlee";

    /// <summary>Priorité d'action RH dérivée du niveau de risque.</summary>
    public static string ClassifyActionPriority(string riskLevel) =>
        riskLevel == "Critical" ? "High" : riskLevel == "Elevated" ? "Medium" : "Low";
}
