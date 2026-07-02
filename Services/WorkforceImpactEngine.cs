using SIRH.EY.Models;
using SIRH.EY.Models.InsightsAI;

namespace SIRH.EY.Services;

/// <summary>
/// Calcul pur du scoring d'impact de départ (successeurs, exposition, risque de continuité) —
/// extrait de WorkforceImpactService. Les décisions (type de successeur, niveau de risque,
/// signal d'exposition) sont déléguées à DecisionEngine, jamais recalculées ici.
/// </summary>
public static class WorkforceImpactEngine
{
    public static double Clamp(double value) => Math.Max(0, Math.Min(100, value));

    public static List<string> GetSkillNames(Collaborateur collaborateur) =>
        (collaborateur.Competences ?? Enumerable.Empty<Competence>())
            .Where(c => !string.IsNullOrWhiteSpace(c.Nom))
            .OrderByDescending(c => c.NiveauActuel)
            .Select(c => c.Nom.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static bool SharesAnySkill(Collaborateur collaborateur, List<string> targetSkills)
    {
        var skills = GetSkillNames(collaborateur);
        return targetSkills.Any(skill => skills.Contains(skill, StringComparer.OrdinalIgnoreCase));
    }

    public static double GetAverageLevel(Collaborateur collaborateur)
    {
        var skills = collaborateur.Competences ?? Enumerable.Empty<Competence>();
        return skills.Any() ? skills.Average(c => c.NiveauActuel) : 1.5;
    }

    public static bool IsSeniorProfile(string? grade) =>
        !string.IsNullOrWhiteSpace(grade)
        && (grade.Contains("Senior", StringComparison.OrdinalIgnoreCase) || grade.Contains("Manager", StringComparison.OrdinalIgnoreCase));

    public static List<WorkforceSuccessorViewModel> BuildSuccessors(
        Collaborateur target, List<string> targetSkills, List<Collaborateur> candidates)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;

        return candidates
            .Select(candidate =>
            {
                var skills = GetSkillNames(candidate);
                var shared = targetSkills.Count(skill => skills.Any(s => comparer.Equals(s, skill)));
                var skillScore = targetSkills.Any() ? 100.0 * shared / targetSkills.Count : 0;
                var levelScore = GetAverageLevel(candidate) * 14;
                var transversalBonus = string.Equals(candidate.Departement, target.Departement, StringComparison.OrdinalIgnoreCase) ? 7 : 12;
                var seniorityBonus = IsSeniorProfile(candidate.Grade) ? 8 : 0;
                var trainingBonus = (candidate.Inscriptions ?? Enumerable.Empty<Inscription>()).Count(i => i.Terminee || i.Progression >= 80) * 2;
                var readiness = Clamp(skillScore * 0.62 + levelScore * 0.18 + transversalBonus + seniorityBonus + trainingBonus);

                return new WorkforceSuccessorViewModel
                {
                    CollaborateurId = candidate.Id,
                    NomComplet = $"{candidate.Prenom} {candidate.Nom}".Trim(),
                    Poste = candidate.Poste ?? "N/A",
                    Departement = candidate.Departement ?? "N/A",
                    ReadinessScore = Math.Round(readiness, 1),
                    SharedCompetencies = shared,
                    SuccessorType = DecisionEngine.ClassifySuccessorType(readiness)
                };
            })
            .OrderByDescending(s => s.ReadinessScore)
            .ThenByDescending(s => s.SharedCompetencies)
            .Take(12)
            .ToList();
    }

    public static List<WorkforceDepartmentExposureViewModel> BuildDepartmentExposure(
        Collaborateur target, List<Collaborateur> allActive, List<string> targetSkills)
    {
        var departments = allActive
            .GroupBy(c => string.IsNullOrWhiteSpace(c.Departement) ? "Non defini" : c.Departement!)
            .Select(group =>
            {
                var impacted = group.Count(c => c.ManagerId == target.Id || SharesAnySkill(c, targetSkills));
                var exposure = Clamp(20 + impacted * 9 + (group.Key.Equals(target.Departement, StringComparison.OrdinalIgnoreCase) ? 18 : 0));
                return new WorkforceDepartmentExposureViewModel
                {
                    Department = group.Key,
                    ImpactedCollaborators = impacted,
                    ExposureScore = Math.Round(exposure, 1),
                    Signal = DecisionEngine.ClassifyExposureSignal(exposure)
                };
            })
            .OrderByDescending(d => d.ExposureScore)
            .Take(4)
            .ToList();

        if (!departments.Any())
        {
            departments.Add(new WorkforceDepartmentExposureViewModel
            {
                Department = target.Departement ?? "RH",
                ImpactedCollaborators = 1,
                ExposureScore = 58,
                Signal = "Dependance a qualifier"
            });
        }

        return departments;
    }

    public static double ComputeSkillExposure(List<string> targetSkills, List<Collaborateur> allActive)
    {
        if (!targetSkills.Any())
            return 55;

        var rareSkills = targetSkills.Count(skill => allActive.Count(c => GetSkillNames(c).Contains(skill, StringComparer.OrdinalIgnoreCase)) <= 1);
        return Clamp(35 + rareSkills * 18 + targetSkills.Count * 3);
    }

    public static double ComputeContinuityRisk(int targetSkillsCount, int impactedTeamCount, double bestReadiness) =>
        Clamp(35 + targetSkillsCount * 4 + impactedTeamCount * 5 - bestReadiness * 0.45);

    public static double ComputeOperationalImpact(int impactedTeamCount, double skillExposure) =>
        Clamp(30 + impactedTeamCount * 8 + skillExposure * 0.3);

    /// <summary>
    /// Reproduit exactement l'expression d'origine <c>Clamp(25 + departmentExposure.FirstOrDefault()?.ExposureScore ?? 45)</c> :
    /// avec la précédence de "??" (plus faible que "+"), si topExposureScore est null le résultat est Clamp(45),
    /// jamais Clamp(25 + 45). En pratique BuildDepartmentExposure ne retourne jamais une liste vide (fallback
    /// interne), donc la branche null est aujourd'hui inatteignable — préservée telle quelle, non "corrigée".
    /// </summary>
    public static double ComputeDepartmentFragility(double? topExposureScore) =>
        Clamp((25 + topExposureScore) ?? 45);

    public static double ComputeStrategicDependency(double continuityRisk, double operationalImpact, double skillExposure) =>
        Clamp((continuityRisk * 0.38) + (operationalImpact * 0.32) + (skillExposure * 0.3));
}
