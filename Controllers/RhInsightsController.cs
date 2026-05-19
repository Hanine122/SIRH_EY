using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;
using SIRH.EY.Models.InsightsAI;
using SIRH.EY.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SIRH.EY.Controllers;

/// <summary>
/// Controleur pour le module RH Insights - tour de controle proactive des risques RH.
/// </summary>
public class RhInsightsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPromotionReadinessService _promotionReadinessService;
    private readonly IWorkforceImpactService _workforceImpactService;

    public RhInsightsController(
        ApplicationDbContext context,
        IPromotionReadinessService promotionReadinessService,
        IWorkforceImpactService workforceImpactService)
    {
        _context = context;
        _promotionReadinessService = promotionReadinessService;
        _workforceImpactService = workforceImpactService;
    }

    // GET: RhInsights
    public async Task<IActionResult> Index()
    {
        var model = new RhInsightsViewModel();

        model.AlertesContinuite = await _context.Collaborateurs
            .Where(c => c.Statut == StatutCollaborateur.Vacant || c.Statut == StatutCollaborateur.EnPassation)
            .OrderBy(c => c.Statut)
            .ThenBy(c => c.Nom)
            .ToListAsync();

        var tousActifs = await _context.Collaborateurs
            .Include(c => c.Competences)
            .Include(c => c.Inscriptions)
            .Where(c => c.Actif)
            .ToListAsync();

        var formations = await _context.Formations
            .Include(f => f.Inscriptions)
            .AsNoTracking()
            .ToListAsync();

        model.SmartAlerts = GenerateSmartAlerts(tousActifs);
        BuildTalentIntelligence(model, tousActifs, formations);
        model.PromotionSimulator = await _promotionReadinessService.BuildSimulatorAsync();
        model.WorkforceImpactSimulator = await _workforceImpactService.BuildSimulatorAsync();

        return View(model);
    }

    /// <summary>
    /// API Endpoint: recupere les remplacants potentiels pour un poste vacant.
    /// Scanne tous les collaborateurs actifs pour garder un vivier transversal.
    /// </summary>
    [HttpGet]
    [Route("api/rhinsights/matching/{id}")]
    public async Task<IActionResult> GetMatchingRemplacants(int id)
    {
        var partant = await _context.Collaborateurs
            .Include(c => c.Competences)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (partant == null)
            return NotFound(new { message = "Collaborateur introuvable" });

        var surProfil = partant.Competences?
            .Where(c => !string.IsNullOrWhiteSpace(c.Nom))
            .Select(c => c.Nom.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        var surPoste = await _context.CompetencesRequisesParPoste
            .AsNoTracking()
            .Where(cr => cr.Poste == partant.Poste)
            .Select(cr => cr.Competence.Trim())
            .Distinct()
            .ToListAsync();

        var comparer = StringComparer.OrdinalIgnoreCase;
        var competencesRequises = surProfil
            .Union(surPoste, comparer)
            .Distinct(comparer)
            .ToList();

        var formations = await _context.Formations.AsNoTracking().ToListAsync();

        var tousActifs = await _context.Collaborateurs
            .Include(c => c.Competences)
            .Where(c => c.Actif && c.Id != id)
            .ToListAsync();

        var candidats = new List<RhInsightsCandidatDetail>();

        foreach (var candidat in tousActifs)
        {
            var nomsCandidat = candidat.Competences?
                .Where(c => !string.IsNullOrWhiteSpace(c.Nom))
                .Select(c => c.Nom.Trim())
                .Distinct(comparer)
                .ToList() ?? new List<string>();

            var communes = competencesRequises.Count(r => nomsCandidat.Any(a => comparer.Equals(a, r)));
            var manquantes = competencesRequises.Where(r => !nomsCandidat.Any(a => comparer.Equals(a, r))).ToList();
            var score = competencesRequises.Count > 0 ? Math.Round(100.0 * communes / competencesRequises.Count, 1) : 0;

            var deptPartant = (partant.Departement ?? "").Trim();
            var deptCandidat = (candidat.Departement ?? "").Trim();
            var profilTransversal = !string.Equals(deptPartant, deptCandidat, StringComparison.OrdinalIgnoreCase);

            var formationsRecommandees = new List<string>();
            foreach (var competence in manquantes.Take(5))
            {
                var formation = formations.FirstOrDefault(f =>
                    !string.IsNullOrEmpty(f.CompetenceVisee) &&
                    f.CompetenceVisee.Trim().Equals(competence, StringComparison.OrdinalIgnoreCase));

                formation ??= formations.FirstOrDefault(f =>
                    (f.Titre ?? "").Contains(competence, StringComparison.OrdinalIgnoreCase));

                if (formation != null && !string.IsNullOrWhiteSpace(formation.Titre) && !formationsRecommandees.Contains(formation.Titre))
                {
                    formationsRecommandees.Add(formation.Titre);
                }
                else if (formation == null)
                {
                    formationsRecommandees.Add($"Parcours recommande - {competence}");
                }
            }

            candidats.Add(new RhInsightsCandidatDetail
            {
                Id = candidat.Id,
                Prenom = candidat.Prenom ?? "",
                Nom = candidat.Nom ?? "",
                Email = candidat.Email ?? "",
                Poste = candidat.Poste ?? "",
                Departement = candidat.Departement ?? "",
                Grade = candidat.Grade ?? "",
                ScoreMatching = score,
                CompetencesManquantes = manquantes,
                CompetencesPossedees = competencesRequises.Where(r => nomsCandidat.Any(a => comparer.Equals(a, r))).ToList(),
                FormationsRecommandees = formationsRecommandees,
                ProfilTransversal = profilTransversal,
                NbCompetencesCommunes = communes
            });
        }

        var resultats = candidats
            .OrderByDescending(c => c.ScoreMatching)
            .ThenByDescending(c => c.ProfilTransversal)
            .ThenBy(c => c.Nom)
            .Take(3)
            .ToList();

        return Ok(new
        {
            partant = new
            {
                partant.Id,
                partant.Prenom,
                partant.Nom,
                partant.Poste,
                partant.Departement,
                partant.Statut,
                CompetencesRequises = competencesRequises
            },
            candidats = resultats
        });
    }

    /// <summary>
    /// API Endpoint: recupere toutes les alertes de continuite.
    /// </summary>
    [HttpGet]
    [Route("api/rhinsights/alertes")]
    public async Task<IActionResult> GetAlertesContinuite()
    {
        var alertes = await _context.Collaborateurs
            .Where(c => c.Statut == StatutCollaborateur.Vacant || c.Statut == StatutCollaborateur.EnPassation)
            .Select(c => new
            {
                c.Id,
                c.Prenom,
                c.Nom,
                c.Poste,
                c.Departement,
                c.Statut,
                DateStatut = c.Statut == StatutCollaborateur.Vacant ? "Poste vacant" : "En passation"
            })
            .OrderBy(c => c.Statut)
            .ThenBy(c => c.Nom)
            .ToListAsync();

        return Ok(alertes);
    }

    /// <summary>
    /// API Endpoint: compare deux collaborateurs via l'IA simulee.
    /// </summary>
    [HttpGet]
    [Route("api/rhinsights/compare/{id1}/{id2}")]
    public async Task<IActionResult> GetAiComparison(int id1, int id2)
    {
        var col1 = await _context.Collaborateurs.Include(c => c.Competences).FirstOrDefaultAsync(c => c.Id == id1);
        var col2 = await _context.Collaborateurs.Include(c => c.Competences).FirstOrDefaultAsync(c => c.Id == id2);

        if (col1 == null || col2 == null)
            return NotFound(new { message = "Un ou plusieurs collaborateurs introuvables." });

        var comp1 = col1.Competences?.Where(c => !string.IsNullOrWhiteSpace(c.Nom)).Select(c => c.Nom.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();
        var comp2 = col2.Competences?.Where(c => !string.IsNullOrWhiteSpace(c.Nom)).Select(c => c.Nom.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();

        var shared = comp1.Intersect(comp2, StringComparer.OrdinalIgnoreCase).ToList();
        var missing = comp1.Except(comp2, StringComparer.OrdinalIgnoreCase).ToList();
        var transversal = comp2.Except(comp1, StringComparer.OrdinalIgnoreCase).ToList();

        var compatibilityScore = comp1.Count > 0 ? Math.Round(100.0 * shared.Count / comp1.Count, 1) : 0;
        var readinessScore = Math.Min(100, compatibilityScore + (transversal.Count * 5));

        var response = new AiComparisonResponse
        {
            CompatibilityScore = compatibilityScore,
            SharedSkills = shared,
            MissingSkills = missing,
            TransversalSkills = transversal,
            ReadinessScore = readinessScore,
            AiSummary = $"Analyse IA : {col2.Prenom} possede {shared.Count} competences cles pour le poste de {col1.Poste}. Score de compatibilite : {compatibilityScore}%.",
            RecommendedFormations = missing.Take(3).Select(m => $"Formation recommandee : {m}").ToList()
        };

        return Ok(response);
    }

    [HttpPost]
    [Route("api/rhinsights/promotion-readiness")]
    public async Task<IActionResult> SimulatePromotionReadiness([FromBody] PromotionReadinessRequest request)
    {
        if (request == null || request.CollaborateurId <= 0)
            return BadRequest(new { message = "Collaborateur invalide." });

        var result = await _promotionReadinessService.SimulateAsync(request.CollaborateurId, request.TargetKey);
        if (result == null)
            return NotFound(new { message = "Collaborateur introuvable." });

        return Ok(result);
    }

    [HttpPost]
    [Route("api/rhinsights/workforce-impact")]
    public async Task<IActionResult> SimulateWorkforceImpact([FromBody] WorkforceImpactRequest request)
    {
        if (request == null || request.CollaborateurId <= 0)
            return BadRequest(new { message = "Collaborateur invalide." });

        var result = await _workforceImpactService.SimulateAsync(request.CollaborateurId);
        if (result == null)
            return NotFound(new { message = "Collaborateur introuvable." });

        return Ok(result);
    }

    private List<SmartAlertViewModel> GenerateSmartAlerts(List<Collaborateur> tousActifs)
    {
        var alerts = new List<SmartAlertViewModel>();
        var managers = tousActifs.Where(c => IsManagerGrade(c.Grade)).ToList();

        foreach (var manager in managers.Take(3))
        {
            alerts.Add(new SmartAlertViewModel
            {
                CollaborateurId = manager.Id,
                Title = "Position critique",
                Description = $"{manager.Poste} sans successeur direct identifie.",
                AlertType = "Succession",
                Severity = "High",
                Recommendation = "Identifier deux successeurs potentiels et lancer une comparaison de competences.",
                AiBadge = "Succession AI",
                ConfidenceScore = 88
            });
        }

        return alerts;
    }

    private void BuildTalentIntelligence(RhInsightsViewModel model, List<Collaborateur> tousActifs, List<Formation> formations)
    {
        BuildExecutiveKpis(model, tousActifs, formations);
        EnsureStrategicSmartAlerts(model, tousActifs);
        BuildHiddenTalents(model, tousActifs);
        BuildSkillHeatmaps(model, tousActifs);
        BuildFormationInsights(model, tousActifs, formations);
    }

    private void BuildExecutiveKpis(RhInsightsViewModel model, List<Collaborateur> tousActifs, List<Formation> formations)
    {
        if (model.KpiCards.Any())
            return;

        var activeCount = Math.Max(1, tousActifs.Count);
        var continuityAlerts = model.AlertesContinuite.Count;
        var managerCount = tousActifs.Count(c => IsManagerGrade(c.Grade));
        var highPotentialCount = tousActifs.Count(c => GetCompetenceScore(c) >= 4);
        var inscriptions = tousActifs.SelectMany(c => c.Inscriptions ?? Enumerable.Empty<Inscription>()).ToList();
        var completedTrainings = inscriptions.Count(i => i.Terminee || i.Progression >= 80);
        var formationImpact = inscriptions.Any() ? Percent(completedTrainings, inscriptions.Count) : Math.Min(92, 58 + formations.Count * 4);
        var shortageIndex = ComputeSkillShortageIndex(tousActifs);
        var successionReadiness = Math.Max(42, Math.Min(96, 72 + highPotentialCount * 3 - continuityAlerts * 6));
        var continuityRisk = Math.Min(100, continuityAlerts * 18 + Math.Max(0, managerCount - highPotentialCount) * 5);
        var criticalPositions = Math.Max(continuityAlerts, managerCount);

        model.KpiCards.Add(new ExecutiveKpiCardViewModel { Title = "Succession readiness", Value = $"{successionReadiness:0}%", NumericValue = successionReadiness, ValueSuffix = "%", Trend = "+5%", IconClass = "fas fa-user-shield", ColorClass = "text-success", Tone = "success", Subtitle = "Couverture des relais possibles", Insight = "Relais prioritaires detectes sur les roles critiques." });
        model.KpiCards.Add(new ExecutiveKpiCardViewModel { Title = "Continuity risk", Value = $"{continuityRisk:0}%", NumericValue = continuityRisk, ValueSuffix = "%", Trend = continuityRisk > 30 ? "+3%" : "-2%", IconClass = "fas fa-exclamation-triangle", ColorClass = "text-warning", Tone = continuityRisk > 30 ? "warning" : "success", Subtitle = "Risque sur postes vacants ou passation", Insight = $"{continuityAlerts} alerte(s) de continuite active(s)." });
        model.KpiCards.Add(new ExecutiveKpiCardViewModel { Title = "Critical positions", Value = criticalPositions.ToString(), NumericValue = criticalPositions, Trend = "+1", IconClass = "fas fa-sitemap", ColorClass = "text-danger", Tone = "danger", Subtitle = "Roles sensibles a monitorer", Insight = "Managers et postes de continuite agreges." });
        model.KpiCards.Add(new ExecutiveKpiCardViewModel { Title = "High potential talents", Value = highPotentialCount.ToString(), NumericValue = highPotentialCount, Trend = "+4", IconClass = "fas fa-star", ColorClass = "text-primary", Tone = "primary", Subtitle = "Collaborateurs a fort signal", Insight = $"{Percent(highPotentialCount, activeCount):0}% du vivier actif." });
        model.KpiCards.Add(new ExecutiveKpiCardViewModel { Title = "Formation impact", Value = $"{formationImpact:0}%", NumericValue = formationImpact, ValueSuffix = "%", Trend = "+7%", IconClass = "fas fa-graduation-cap", ColorClass = "text-info", Tone = "info", Subtitle = "Progression formation exploitable", Insight = $"{completedTrainings} formation(s) terminee(s) ou avancee(s)." });
        model.KpiCards.Add(new ExecutiveKpiCardViewModel { Title = "Skill shortage index", Value = $"{shortageIndex:0}%", NumericValue = shortageIndex, ValueSuffix = "%", Trend = shortageIndex > 35 ? "+2%" : "-1%", IconClass = "fas fa-layer-group", ColorClass = "text-secondary", Tone = shortageIndex > 35 ? "warning" : "neutral", Subtitle = "Tension sur competences rares", Insight = "Index base sur ecarts niveau actuel / cible." });
    }

    private void EnsureStrategicSmartAlerts(RhInsightsViewModel model, List<Collaborateur> tousActifs)
    {
        var anchor = tousActifs.FirstOrDefault();
        var collaborateurId = anchor?.Id ?? 0;
        var mostCommonSkill = tousActifs
            .SelectMany(c => c.Competences ?? Enumerable.Empty<Competence>())
            .Where(c => !string.IsNullOrWhiteSpace(c.Nom))
            .GroupBy(c => c.Nom.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "Leadership";

        var strategicAlerts = new List<SmartAlertViewModel>
        {
            new() { CollaborateurId = collaborateurId, Title = "Reserve de succession a renforcer", Description = "Les postes critiques doivent avoir au moins deux relais identifies.", AlertType = "Succession", Severity = model.AlertesContinuite.Any() ? "High" : "Medium", Recommendation = "Prioriser une revue manager et une matrice de remplacement sur 30 jours.", AiBadge = "Continuity AI", ConfidenceScore = 91 },
            new() { CollaborateurId = collaborateurId, Title = "Competence strategique sous surveillance", Description = $"La competence {mostCommonSkill} concentre une forte dependance operationnelle.", AlertType = "SkillHeatmap", Severity = "Medium", Recommendation = "Diversifier la couverture via formation ciblee et binomage transverse.", AiBadge = "Skill AI", ConfidenceScore = 84 },
            new() { CollaborateurId = collaborateurId, Title = "Impact formation a convertir", Description = "Les parcours en cours peuvent alimenter le vivier de succession.", AlertType = "Training", Severity = "Low", Recommendation = "Associer les formations critiques aux postes et grades cibles.", AiBadge = "Learning AI", ConfidenceScore = 79 },
            new() { CollaborateurId = collaborateurId, Title = "Mobilite interne proactive", Description = "Des talents polyvalents peuvent reduire le risque de rupture.", AlertType = "Mobility", Severity = "Medium", Recommendation = "Comparer les profils transverses avant toute recherche externe.", AiBadge = "Mobility AI", ConfidenceScore = 86 }
        };

        foreach (var alert in strategicAlerts)
        {
            if (model.SmartAlerts.Count >= 5)
                break;

            if (!model.SmartAlerts.Any(a => a.Title == alert.Title))
                model.SmartAlerts.Add(alert);
        }
    }

    private void BuildHiddenTalents(RhInsightsViewModel model, List<Collaborateur> tousActifs)
    {
        if (model.HiddenTalents.Any())
            return;

        var candidates = tousActifs
            .Select(c => new
            {
                Collaborateur = c,
                Skills = (c.Competences ?? Enumerable.Empty<Competence>()).ToList(),
                Trainings = (c.Inscriptions ?? Enumerable.Empty<Inscription>()).Count(i => i.Terminee || i.Progression >= 60),
                Score = GetCompetenceScore(c)
            })
            .OrderByDescending(c => c.Score)
            .ThenByDescending(c => c.Trainings)
            .Take(4)
            .ToList();

        var labels = new[] { "Polyvalent talent", "Future leader", "Highly trained", "Fast learner" };
        var signals = new[] { "Couverture multi-competences", "Signal leadership", "Formation consolidee", "Progression rapide" };

        for (var i = 0; i < candidates.Count; i++)
        {
            var item = candidates[i];
            model.HiddenTalents.Add(new HiddenTalentViewModel
            {
                CollaborateurId = item.Collaborateur.Id,
                NomComplet = $"{item.Collaborateur.Prenom} {item.Collaborateur.Nom}",
                Departement = item.Collaborateur.Departement ?? "N/A",
                PosteActuel = item.Collaborateur.Poste ?? "N/A",
                ReadinessScore = Math.Min(96, Math.Round(62 + item.Score * 7 + item.Trainings * 3, 1)),
                EvolutionPotentielle = IsManagerGrade(item.Collaborateur.Grade) ? "Leadership transverse" : "Manager / Expert",
                CompetencesCles = item.Skills.OrderByDescending(s => s.NiveauActuel).Select(s => s.Nom).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToList(),
                TalentType = labels[Math.Min(i, labels.Length - 1)],
                Signal = signals[Math.Min(i, signals.Length - 1)],
                FormationCount = item.Trainings
            });
        }

        if (!model.HiddenTalents.Any())
        {
            model.HiddenTalents.Add(new HiddenTalentViewModel
            {
                NomComplet = "Vivier a qualifier",
                Departement = "RH",
                PosteActuel = "Analyse en attente",
                ReadinessScore = 68,
                EvolutionPotentielle = "Succession ciblee",
                TalentType = "Talent intelligence",
                Signal = "Completer les competences pour activer le scoring",
                CompetencesCles = new List<string> { "Leadership", "Polyvalence", "Apprentissage" }
            });
        }
    }

    private void BuildSkillHeatmaps(RhInsightsViewModel model, List<Collaborateur> tousActifs)
    {
        if (model.SkillHeatmaps.Any())
            return;

        var skills = tousActifs
            .SelectMany(c => c.Competences ?? Enumerable.Empty<Competence>())
            .Where(c => !string.IsNullOrWhiteSpace(c.Nom))
            .GroupBy(c => c.Nom.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var mastered = g.Count(c => c.NiveauActuel >= 3);
                var required = Math.Max(mastered + g.Count(c => c.NiveauCible > c.NiveauActuel), Math.Max(3, mastered + 2));
                var coverage = Percent(mastered, required);
                return new SkillHeatmapViewModel
                {
                    Competence = g.Key,
                    NbCollaborateursMaitrisant = mastered,
                    NbCollaborateursRequis = required,
                    Couverture = coverage,
                    Status = coverage < 45 ? "Critical" : coverage < 75 ? "Warning" : "Healthy",
                    Category = coverage < 45 ? "Critical competency" : g.Count() <= 2 ? "Rare competency" : coverage < 75 ? "Strategic skill" : "Overloaded skill",
                    Insight = coverage < 45 ? "Renfort prioritaire requis" : coverage < 75 ? "Formation recommandee" : "Couverture robuste"
                };
            })
            .OrderBy(h => h.Couverture)
            .ThenByDescending(h => h.NbCollaborateursRequis)
            .Take(8)
            .ToList();

        model.SkillHeatmaps.AddRange(skills);

        if (!model.SkillHeatmaps.Any())
        {
            model.SkillHeatmaps.Add(new SkillHeatmapViewModel { Competence = "Leadership", NbCollaborateursMaitrisant = 2, NbCollaborateursRequis = 5, Couverture = 40, Status = "Critical", Category = "Critical competency", Insight = "Structurer un parcours manager" });
            model.SkillHeatmaps.Add(new SkillHeatmapViewModel { Competence = "Data analytics", NbCollaborateursMaitrisant = 4, NbCollaborateursRequis = 8, Couverture = 50, Status = "Warning", Category = "Rare competency", Insight = "Renforcer le vivier expert" });
            model.SkillHeatmaps.Add(new SkillHeatmapViewModel { Competence = "Gestion de projet", NbCollaborateursMaitrisant = 9, NbCollaborateursRequis = 10, Couverture = 90, Status = "Healthy", Category = "Strategic skill", Insight = "Capitaliser via mentoring" });
        }
    }

    private void BuildFormationInsights(RhInsightsViewModel model, List<Collaborateur> tousActifs, List<Formation> formations)
    {
        if (model.FormationInsights.Any())
            return;

        var activeCount = Math.Max(1, tousActifs.Count);
        var insights = formations
            .OrderByDescending(f => f.Inscriptions?.Count ?? 0)
            .ThenBy(f => f.DateDebut)
            .Take(4)
            .Select(f =>
            {
                var enrolled = f.Inscriptions?.Count ?? 0;
                var targetSkill = string.IsNullOrWhiteSpace(f.CompetenceVisee) ? f.Categorie ?? "Competence cible" : f.CompetenceVisee;
                return new FormationInsightViewModel
                {
                    FormationTitre = f.Titre,
                    UrgencyScore = Math.Min(95, 48 + Percent(enrolled, activeCount) / 2 + (f.DateDebut >= DateTime.Today ? 12 : 0)),
                    Cible = string.IsNullOrWhiteSpace(f.Categorie) ? "Population cible RH" : f.Categorie,
                    NbCollaborateursImpactes = Math.Max(enrolled, Math.Min(activeCount, Math.Max(3, f.CapaciteMax / 2))),
                    ExpectedImpact = $"Renforce {targetSkill}",
                    ReadinessGain = Math.Min(18, 6 + enrolled),
                    TargetedCompetencies = new List<string> { targetSkill }
                };
            })
            .ToList();

        model.FormationInsights.AddRange(insights);

        if (!model.FormationInsights.Any())
        {
            model.FormationInsights.Add(new FormationInsightViewModel { FormationTitre = "Parcours Leadership & succession", UrgencyScore = 86, Cible = "Managers et seniors", NbCollaborateursImpactes = 12, ExpectedImpact = "Reduction du risque de continuite", ReadinessGain = 14, TargetedCompetencies = new List<string> { "Leadership", "Gestion de projet" } });
            model.FormationInsights.Add(new FormationInsightViewModel { FormationTitre = "Academie competences critiques", UrgencyScore = 72, Cible = "Talents polyvalents", NbCollaborateursImpactes = 18, ExpectedImpact = "Couverture des competences rares", ReadinessGain = 11, TargetedCompetencies = new List<string> { "Data analytics", "Expertise metier" } });
        }
    }

    private static bool IsManagerGrade(string? grade)
    {
        return !string.IsNullOrWhiteSpace(grade)
            && grade.Contains("Manager", StringComparison.OrdinalIgnoreCase);
    }

    private static double GetCompetenceScore(Collaborateur collaborateur)
    {
        var competences = (collaborateur.Competences ?? Enumerable.Empty<Competence>()).ToList();
        if (!competences.Any())
            return 0;

        var averageLevel = competences.Average(c => c.NiveauActuel);
        var polyvalenceBonus = Math.Min(1.5, competences.Count * 0.2);
        return Math.Min(5, averageLevel + polyvalenceBonus);
    }

    private static int ComputeSkillShortageIndex(List<Collaborateur> tousActifs)
    {
        var competences = tousActifs.SelectMany(c => c.Competences ?? Enumerable.Empty<Competence>()).ToList();
        if (!competences.Any())
            return 32;

        var gaps = competences.Sum(c => Math.Max(0, c.NiveauCible - c.NiveauActuel));
        var maxGap = Math.Max(1, competences.Count * 4);
        return (int)Math.Round(100.0 * gaps / maxGap);
    }

    private static int Percent(int value, int total)
    {
        if (total <= 0)
            return 0;

        return (int)Math.Round(100.0 * value / total);
    }
}

/// <summary>
/// Detail d'un candidat pour RH Insights.
/// </summary>
public class RhInsightsCandidatDetail
{
    public int Id { get; set; }
    public string Prenom { get; set; } = "";
    public string Nom { get; set; } = "";
    public string Email { get; set; } = "";
    public string Poste { get; set; } = "";
    public string Departement { get; set; } = "";
    public string Grade { get; set; } = "";
    public double ScoreMatching { get; set; }
    public List<string> CompetencesManquantes { get; set; } = new();
    public List<string> CompetencesPossedees { get; set; } = new();
    public List<string> FormationsRecommandees { get; set; } = new();
    public bool ProfilTransversal { get; set; }
    public int NbCompetencesCommunes { get; set; }
}
