using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;
using SIRH.EY.Models.InsightsAI;

namespace SIRH.EY.Services;

public class WorkforceImpactService : IWorkforceImpactService
{
    private readonly ApplicationDbContext _context;

    public WorkforceImpactService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WorkforceImpactSimulatorViewModel> BuildSimulatorAsync()
    {
        var collaborateurs = await _context.Collaborateurs
            .AsNoTracking()
            .Where(c => c.Actif)
            .OrderBy(c => c.Nom)
            .ThenBy(c => c.Prenom)
            .Select(c => new PromotionCollaborateurOptionViewModel
            {
                Id = c.Id,
                NomComplet = (c.Prenom + " " + c.Nom).Trim(),
                Poste = c.Poste ?? "",
                Grade = c.Grade ?? "",
                Departement = c.Departement ?? ""
            })
            .ToListAsync();

        var defaultCollaborateur = collaborateurs
            .OrderByDescending(c => c.Grade.Contains("Manager"))
            .ThenBy(c => c.NomComplet)
            .FirstOrDefault();

        return new WorkforceImpactSimulatorViewModel
        {
            Collaborateurs = collaborateurs,
            DefaultResult = defaultCollaborateur != null
                ? await SimulateAsync(defaultCollaborateur.Id)
                : BuildFallbackResult()
        };
    }

    public async Task<WorkforceImpactResultViewModel?> SimulateAsync(int collaborateurId)
    {
        var target = await _context.Collaborateurs
            .Include(c => c.Competences)
            .Include(c => c.Equipe)
            .FirstOrDefaultAsync(c => c.Id == collaborateurId && c.Actif);

        if (target == null)
            return null;

        var allActive = await _context.Collaborateurs
            .Include(c => c.Competences)
            .Include(c => c.Inscriptions)
            .Where(c => c.Actif && c.Id != target.Id)
            .ToListAsync();

        var targetSkills = GetSkillNames(target);
        if (!targetSkills.Any())
            targetSkills = BuildFallbackSkills(target);

        var successors = BuildSuccessors(target, targetSkills, allActive);
        var immediate = successors.Where(s => s.ReadinessScore >= 75).Take(3).ToList();
        var partial = successors.Where(s => s.ReadinessScore >= 45 && s.ReadinessScore < 75).Take(3).ToList();
        var highPotential = successors
            .Where(s => s.ReadinessScore < 75)
            .OrderByDescending(s => s.SuccessorType == "High potential")
            .ThenByDescending(s => s.ReadinessScore)
            .Take(3)
            .ToList();

        var sameDepartment = allActive.Where(c => string.Equals(c.Departement, target.Departement, StringComparison.OrdinalIgnoreCase)).ToList();
        var impactedTeamCount = (target.Equipe?.Count ?? 0) + sameDepartment.Count(c => c.ManagerId == target.Id);
        var skillExposure = ComputeSkillExposure(targetSkills, allActive);
        var departmentExposure = BuildDepartmentExposure(target, allActive, targetSkills);
        var bestReadiness = successors.FirstOrDefault()?.ReadinessScore ?? 0;
        var continuityRisk = Clamp(35 + targetSkills.Count * 4 + impactedTeamCount * 5 - bestReadiness * 0.45);
        var operationalImpact = Clamp(30 + impactedTeamCount * 8 + skillExposure * 0.3);
        var fragility = Clamp(25 + departmentExposure.FirstOrDefault()?.ExposureScore ?? 45);
        var strategicDependency = Clamp((continuityRisk * 0.38) + (operationalImpact * 0.32) + (skillExposure * 0.3));
        var riskLevel = strategicDependency >= 75 ? "Critical" : strategicDependency >= 55 ? "Elevated" : "Controlled";

        return new WorkforceImpactResultViewModel
        {
            CollaborateurId = target.Id,
            CollaborateurNom = $"{target.Prenom} {target.Nom}".Trim(),
            Role = $"{target.Poste} - {target.Grade}".Trim(' ', '-'),
            Departement = target.Departement ?? "N/A",
            CompetenciesLost = targetSkills.Take(8).ToList(),
            ContinuityRisk = Math.Round(continuityRisk, 1),
            OperationalImpact = Math.Round(operationalImpact, 1),
            DepartmentFragility = Math.Round(fragility, 1),
            StrategicDependencyScore = Math.Round(strategicDependency, 1),
            RiskLevel = riskLevel,
            DepartmentExposure = departmentExposure,
            ImmediateSuccessors = immediate,
            PartialSuccessors = partial,
            HighPotentialAlternatives = highPotential,
            RecommendedActions = BuildRecommendations(riskLevel, immediate.Any(), targetSkills),
            ExecutiveInsight = BuildExecutiveInsight(target, riskLevel, strategicDependency, bestReadiness, targetSkills)
        };
    }

    private static List<WorkforceSuccessorViewModel> BuildSuccessors(Collaborateur target, List<string> targetSkills, List<Collaborateur> candidates)
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
                    SuccessorType = readiness >= 75 ? "Immediate successor" : readiness >= 45 ? "Partial successor" : "High potential"
                };
            })
            .OrderByDescending(s => s.ReadinessScore)
            .ThenByDescending(s => s.SharedCompetencies)
            .Take(12)
            .ToList();
    }

    private static List<WorkforceDepartmentExposureViewModel> BuildDepartmentExposure(Collaborateur target, List<Collaborateur> allActive, List<string> targetSkills)
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
                    Signal = exposure >= 70 ? "Forte dependance" : exposure >= 45 ? "Fragilite a surveiller" : "Exposition controlee"
                };
            })
            .OrderByDescending(d => d.ExposureScore)
            .Take(4)
            .ToList();

        if (!departments.Any())
        {
            departments.Add(new WorkforceDepartmentExposureViewModel { Department = target.Departement ?? "RH", ImpactedCollaborators = 1, ExposureScore = 58, Signal = "Dependance a qualifier" });
        }

        return departments;
    }

    private static List<WorkforceActionRecommendationViewModel> BuildRecommendations(string riskLevel, bool hasImmediateSuccessor, List<string> targetSkills)
    {
        var priority = riskLevel == "Critical" ? "High" : riskLevel == "Elevated" ? "Medium" : "Low";
        var keySkill = targetSkills.FirstOrDefault() ?? "competence critique";
        var actions = new List<WorkforceActionRecommendationViewModel>
        {
            new() { Title = hasImmediateSuccessor ? "Activer un plan de passation" : "Renforcer la succession", Description = hasImmediateSuccessor ? "Planifier une passation structuree avec le meilleur successeur identifie." : "Aucun successeur immediat robuste : lancer une revue de mobilite interne.", Category = "Succession", Priority = priority },
            new() { Title = $"Proteger {keySkill}", Description = "Creer un parcours formation cible et documenter les savoirs critiques.", Category = "Formation", Priority = priority },
            new() { Title = "Redistribuer la charge", Description = "Identifier les activites critiques et repartir temporairement les responsabilites sur 2 a 3 relais.", Category = "Workload", Priority = "Medium" },
            new() { Title = "Scenario recrutement", Description = "Preparer une option externe si le score de dependance reste eleve apres mobilite interne.", Category = "Recruitment", Priority = riskLevel == "Critical" ? "High" : "Medium" }
        };

        return actions;
    }

    private static string BuildExecutiveInsight(Collaborateur target, string riskLevel, double dependency, double bestReadiness, List<string> skills)
    {
        var name = $"{target.Prenom} {target.Nom}".Trim();
        var skill = skills.FirstOrDefault() ?? "competence critique";
        var readinessText = bestReadiness >= 75 ? "un successeur immediat existe" : bestReadiness >= 45 ? "des successeurs partiels existent" : "aucun successeur immediat n'est disponible";
        return $"{target.Departement ?? "Le departement"} verrait son risque de dependance augmenter a {dependency:0}% si {name} quittait son role. {readinessText}. Exposition critique detectee sur {skill}; recommander passation, mobilite interne et formation ciblee.";
    }

    private static List<string> GetSkillNames(Collaborateur collaborateur)
    {
        return (collaborateur.Competences ?? Enumerable.Empty<Competence>())
            .Where(c => !string.IsNullOrWhiteSpace(c.Nom))
            .OrderByDescending(c => c.NiveauActuel)
            .Select(c => c.Nom.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool SharesAnySkill(Collaborateur collaborateur, List<string> targetSkills)
    {
        var skills = GetSkillNames(collaborateur);
        return targetSkills.Any(skill => skills.Contains(skill, StringComparer.OrdinalIgnoreCase));
    }

    private static List<string> BuildFallbackSkills(Collaborateur target)
    {
        var skills = new List<string> { "Communication", "Gestion de projet", "Expertise metier" };
        if ((target.Grade ?? "").Contains("Manager", StringComparison.OrdinalIgnoreCase) || (target.Poste ?? "").Contains("Manager", StringComparison.OrdinalIgnoreCase))
            skills.AddRange(new[] { "Leadership", "Stakeholder management" });
        return skills.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static double ComputeSkillExposure(List<string> targetSkills, List<Collaborateur> allActive)
    {
        if (!targetSkills.Any())
            return 55;

        var rareSkills = targetSkills.Count(skill => allActive.Count(c => GetSkillNames(c).Contains(skill, StringComparer.OrdinalIgnoreCase)) <= 1);
        return Clamp(35 + rareSkills * 18 + targetSkills.Count * 3);
    }

    private static double GetAverageLevel(Collaborateur collaborateur)
    {
        var skills = collaborateur.Competences ?? Enumerable.Empty<Competence>();
        return skills.Any() ? skills.Average(c => c.NiveauActuel) : 1.5;
    }

    private static bool IsSeniorProfile(string? grade)
    {
        return !string.IsNullOrWhiteSpace(grade)
            && (grade.Contains("Senior", StringComparison.OrdinalIgnoreCase) || grade.Contains("Manager", StringComparison.OrdinalIgnoreCase));
    }

    private static double Clamp(double value)
    {
        return Math.Max(0, Math.Min(100, value));
    }

    private static WorkforceImpactResultViewModel BuildFallbackResult()
    {
        return new WorkforceImpactResultViewModel
        {
            CollaborateurNom = "Role critique a selectionner",
            Role = "Manager - role sensible",
            Departement = "RH",
            ContinuityRisk = 68,
            OperationalImpact = 61,
            DepartmentFragility = 57,
            StrategicDependencyScore = 64,
            RiskLevel = "Elevated",
            ExecutiveInsight = "Selectionnez un collaborateur pour simuler la dependance organisationnelle, les successeurs et les actions RH recommandees.",
            CompetenciesLost = new List<string> { "Leadership", "Gestion de projet", "Communication" },
            DepartmentExposure = new List<WorkforceDepartmentExposureViewModel>
            {
                new() { Department = "RH", ImpactedCollaborators = 3, ExposureScore = 64, Signal = "Fragilite a surveiller" }
            },
            PartialSuccessors = new List<WorkforceSuccessorViewModel>
            {
                new() { NomComplet = "Vivier interne", Poste = "Successeur partiel", Departement = "Transverse", ReadinessScore = 58, SharedCompetencies = 2, SuccessorType = "Partial successor" }
            },
            RecommendedActions = BuildRecommendations("Elevated", false, new List<string> { "Leadership" })
        };
    }
}
