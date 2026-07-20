namespace SIRH.EY.Models;

/// <summary>
/// Données minimales pour le partial _EvaluationStatusBadge — le badge de statut
/// (À évaluer / En attente / Validé), les notes /5 et le bouton de validation manager
/// réutilisés sur les 3 sections de cartes de Competences/Index.cshtml.
/// </summary>
public class CompetenceEvaluationStatusViewModel
{
    public Competence Competence { get; set; } = null!;
    public bool IsManager { get; set; }
}
