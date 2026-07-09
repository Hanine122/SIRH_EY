using SIRH.EY.Models;

namespace SIRH.EY.Services;

public static class CompetenceChangeReason
{
    public const string ValidationManager = "ValidationManager";
    public const string CorrectionManager = "CorrectionManager";
    public const string Manuel = "Manuel";
    public const string Formation = "Formation";
}

public interface ICompetenceLifecycleService
{
    // Point d'entree unique quand Competence.NiveauActuel change reellement (validation manager,
    // override RH, ou completion de formation). Ecrit l'historique et referme les plans de
    // developpement resolus. N'appelle pas SaveChangesAsync : la persistance reste portee par
    // l'appelant pour garder tout dans la meme transaction EF.
    Task RecordLevelChangeAsync(Competence competence, int ancienNiveau, int nouveauNiveau, string raison);
}
