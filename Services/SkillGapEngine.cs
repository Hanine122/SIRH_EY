using SIRH.EY.Models;

namespace SIRH.EY.Services;

public record SkillGapMatch(Competence Competence, int Ecart);

// Static helper, same convention as CompetenceRules/FormationCompletionEngine.
// Matches a collaborateur's skill gaps (NiveauActuel < NiveauCible, target already
// set per grade via CompetenceRules.GetNiveauCibleParGrade when the Competence was
// created) against the catalogue of Formations, reusing the same "SkillId bridge
// first, CompetenceVisee text fallback" convention used in FormationsController.Terminer/
// TerminerFormation.
public static class SkillGapEngine
{
    public static List<SkillGapMatch> GetGaps(IEnumerable<Competence> competences)
        => competences
            .Where(c => c.NiveauActuel < c.NiveauCible)
            .Select(c => new SkillGapMatch(c, c.NiveauCible - c.NiveauActuel))
            .OrderByDescending(m => m.Ecart)
            .ToList();

    public static bool FormationCombleGap(Formation f, Competence gap)
        => (gap.SkillId != null && f.SkillId == gap.SkillId)
        || (!string.IsNullOrEmpty(gap.Nom) && gap.Nom.Equals(f.CompetenceVisee, StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<int> RecommendFormationIds(
        IEnumerable<Competence> competences,
        IEnumerable<Formation> catalogue,
        IEnumerable<int> exclureFormationIds)
    {
        var gaps = GetGaps(competences);
        var exclus = exclureFormationIds.ToHashSet();

        return catalogue
            .Where(f => !exclus.Contains(f.Id) && gaps.Any(g => FormationCombleGap(f, g.Competence)))
            .Select(f => f.Id);
    }
}
