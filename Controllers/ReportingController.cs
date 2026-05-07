using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;
using System.Linq;

namespace SIRH.EY.Controllers;

public class ReportingController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportingController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Reporting/ExecutiveDashboard
    public async Task<IActionResult> ExecutiveDashboard()
    {
        // KPI 1: Collaborateurs actifs
        var collaborateursActifs = await _context.Collaborateurs
            .CountAsync(c => c.Actif);
        ViewBag.CollaborateursActifs = collaborateursActifs;

        // KPI 2: Taux de compétences validées (niveau actuel >= niveau cible)
        var competences = await _context.Competences.ToListAsync();
        var competencesValidees = competences.Count(c => c.NiveauActuel >= c.NiveauCible);
        var tauxCompetences = competences.Any() 
            ? (int)((double)competencesValidees / competences.Count * 100) 
            : 0;
        ViewBag.TauxCompetencesValidees = tauxCompetences;
        ViewBag.TotalCompetences = competences.Count;

        // KPI 3: Progression formations
        var inscriptions = await _context.Inscriptions.ToListAsync();
        var formationsTerminees = inscriptions.Count(i => i.Terminee);
        var tauxFormations = inscriptions.Any() 
            ? (int)((double)formationsTerminees / inscriptions.Count * 100) 
            : 0;
        ViewBag.TauxFormations = tauxFormations;
        ViewBag.TotalInscriptions = inscriptions.Count;

        // KPI 4: Postes critiques (calculé sur les compétences critiques manquantes)
        var postesCritiques = competences.Count(c => c.NiveauCible - c.NiveauActuel >= 3);
        ViewBag.PostesCritiques = postesCritiques;

        // KPI 5: Remplaçants disponibles
        var remplacants = await _context.Collaborateurs
            .CountAsync(c => c.Actif && c.Grade == "Senior");
        ViewBag.RemplacantsDisponibles = remplacants;

        // KPI 6: Taux d'écarts critiques (skill gaps)
        var ecartsCritiques = competences.Count(c => c.NiveauCible - c.NiveauActuel >= 2);
        var tauxEcarts = competences.Any() 
            ? (int)((double)ecartsCritiques / competences.Count * 100) 
            : 0;
        ViewBag.TauxEcartsCritiques = tauxEcarts;

        // Data pour Chart.js - Evolution skill gaps (6 derniers mois)
        var evolutionData = new List<int> { 12, 15, 10, 8, 11, ecartsCritiques };
        ViewBag.EvolutionSkillGaps = evolutionData;

        // Data pour Chart.js - Top 5 compétences à développer (moyenne globale la plus faible)
        var topCompetencesManquantes = competences
            .GroupBy(c => c.Nom)
            .Select(g => new { 
                Nom = g.Key, 
                Count = g.Count(c => c.NiveauActuel < 3),
                AverageScore = g.Average(c => c.NiveauActuel)
            })
            .OrderBy(x => x.AverageScore)
            .Take(5)
            .ToList();
        ViewBag.TopCompetencesManquantes = topCompetencesManquantes;

        // Data pour Chart.js - Répartition par département (Moyenne des compétences)
        var repartitionDept = await _context.Competences
            .Include(c => c.Collaborateur)
            .Where(c => c.Collaborateur != null)
            .GroupBy(c => c.Collaborateur!.Departement)
            .Select(g => new { 
                Departement = g.Key ?? "Non assigné", 
                AverageScore = Math.Round(g.Average(c => c.NiveauActuel), 1) 
            })
            .ToListAsync();
        ViewBag.RepartitionParDepartement = repartitionDept;

        // Data pour Chart.js - Progression mensuelle collaborateurs
        var progressionMensuelle = new List<int> { 45, 52, 48, 58, 62, collaborateursActifs };
        ViewBag.ProgressionMensuelle = progressionMensuelle;

        // Data pour la heatmap - Compétences par collaborateur
        var collaborateurs = await _context.Collaborateurs
            .Where(c => c.Actif)
            .Include(c => c.Competences)
            .Take(20) // Limiter pour la heatmap
            .ToListAsync();
        
        var allCompetences = collaborateurs
            .SelectMany(c => c.Competences)
            .Select(comp => comp.Nom)
            .Distinct()
            .Take(15) // Limiter les colonnes
            .ToList();
        
        ViewBag.HeatmapCollaborateurs = collaborateurs;
        ViewBag.HeatmapCompetences = allCompetences;

        return View();
    }

    // =========================
    // SUCCESSION ANALYTICS
    // =========================
    public async Task<IActionResult> SuccessionAnalytics(string? departement = null, string? grade = null)
    {
        // Récupérer les collaborateurs actifs avec leurs compétences
        var query = _context.Collaborateurs
            .Where(c => c.Actif)
            .Include(c => c.Competences)
            .AsQueryable();

        if (!string.IsNullOrEmpty(departement))
            query = query.Where(c => c.Departement == departement);
        
        if (!string.IsNullOrEmpty(grade))
            query = query.Where(c => c.Grade == grade);

        var collaborateurs = await query.ToListAsync();

        // KPI 1: Postes critiques (sans remplaçant potentiel)
        var postesCritiques = await IdentifyPostesCritiquesAsync();
        ViewBag.PostesCritiques = postesCritiques;
        ViewBag.NbPostesCritiques = postesCritiques.Count;

        // KPI 2: Taux de couverture succession
        var seniors = collaborateurs.Count(c => c.Grade == "Senior" || c.Grade == "Manager");
        var totalPostes = collaborateurs.Count;
        var tauxCouverture = totalPostes > 0 ? (int)((double)seniors / totalPostes * 100) : 0;
        ViewBag.TauxCouverture = tauxCouverture;
        ViewBag.SeniorsDisponibles = seniors;

        // KPI 3: Départements critiques
        var deptStats = collaborateurs
            .GroupBy(c => c.Departement)
            .Select(g => new {
                Departement = g.Key,
                Total = g.Count(),
                Seniors = g.Count(c => c.Grade == "Senior" || c.Grade == "Manager"),
                Risque = g.Count(c => c.Grade == "Junior") > g.Count(c => c.Grade == "Senior") ? "High" : "Low"
            })
            .ToList();
        ViewBag.DeptStats = deptStats;

        // KPI 4: Top compétences rares (peu de personnes les possèdent à niveau élevé)
        var competencesRare = await _context.Competences
            .GroupBy(c => c.Nom)
            .Select(g => new {
                Competence = g.Key,
                NbExperts = g.Count(c => c.NiveauActuel >= 4),
                Total = g.Count()
            })
            .Where(x => x.NbExperts <= 2) // Moins de 2 experts
            .OrderBy(x => x.NbExperts)
            .Take(10)
            .ToListAsync();
        ViewBag.CompetencesRare = competencesRare;

        // KPI 5: Temps moyen de montée en compétence
        var progressionData = await CalculateProgressionTimeAsync();
        ViewBag.ProgressionData = progressionData;

        // KPI 6: High Potentials
        var highPotentials = await _context.TalentEvaluations
            .Include(t => t.Collaborateur)
            .Where(t => t.Actif && (t.Category == NineBoxCategory.Star || t.Category == NineBoxCategory.EmergingTalent))
            .Select(t => t.Collaborateur)
            .Distinct()
            .Take(10)
            .ToListAsync();
        ViewBag.HighPotentials = highPotentials;

        // Data pour heatmap - Compétences critiques
        var competencesCritiques = await _context.Competences
            .Where(c => c.NiveauCible - c.NiveauActuel >= 2)
            .Select(c => c.Nom)
            .Distinct()
            .Take(10)
            .ToListAsync();
        
        var collabsCritiques = collaborateurs.Take(15).ToList();
        ViewBag.HeatmapCollaborateurs = collabsCritiques;
        ViewBag.HeatmapCompetences = competencesCritiques;

        // Filtres
        ViewBag.Departements = await _context.Collaborateurs
            .Select(c => c.Departement)
            .Distinct()
            .ToListAsync();
        ViewBag.Grades = new[] { "Junior", "Senior", "Manager" };

        return View();
    }

    // API endpoint pour données chart.js
    [HttpGet]
    public async Task<IActionResult> GetSkillGapData()
    {
        var competences = await _context.Competences.ToListAsync();
        var ecarts = competences
            .GroupBy(c => c.Nom)
            .Select(g => new {
                Competence = g.Key,
                EcartMoyen = g.Average(c => c.NiveauCible - c.NiveauActuel)
            })
            .OrderByDescending(x => x.EcartMoyen)
            .Take(10)
            .ToList();
        
        return Json(ecarts);
    }

    // =========================
    // MÉTHODES PRIVÉES
    // =========================
    private async Task<List<PosteCritiqueViewModel>> IdentifyPostesCritiquesAsync()
    {
        var result = new List<PosteCritiqueViewModel>();
        
        // Récupérer tous les collaborateurs avec leurs compétences
        var collabs = await _context.Collaborateurs
            .Where(c => c.Actif)
            .Include(c => c.Competences)
            .ToListAsync();

        // Grouper par poste
        var postes = collabs.GroupBy(c => new { c.Poste, c.Departement });

        foreach (var poste in postes)
        {
            var titulaires = poste.ToList();
            var nbTitulaires = titulaires.Count;
            
            // Remplaçants potentiels (autres collaborateurs avec compétences similaires)
            var remplacants = collabs
                .Where(c => c.Id != titulaires.First().Id && 
                       c.Grade == "Senior" && 
                       c.Competences.Any(comp => titulaires.Any(t => t.Competences.Any(tc => tc.Nom == comp.Nom && tc.NiveauActuel >= comp.NiveauCible - 1))))
                .ToList();

            var nbRemplacants = remplacants.Count;
            
            // Calculer le risque
            var risque = (nbTitulaires, nbRemplacants) switch
            {
                (1, 0) => "Critique",
                (1, 1) => "Élevé",
                (2, 0) => "Élevé",
                (_, < 2) => "Moyen",
                _ => "Faible"
            };

            if (risque != "Faible")
            {
                result.Add(new PosteCritiqueViewModel
                {
                    Poste = poste.Key.Poste,
                    Departement = poste.Key.Departement,
                    NbTitulaires = nbTitulaires,
                    NbRemplacants = nbRemplacants,
                    NiveauRisque = risque,
                    ReadinessScore = nbRemplacants > 0 ? (int)((double)nbRemplacants / (nbTitulaires + nbRemplacants) * 100) : 0,
                    CompetencesCles = titulaires.SelectMany(t => t.Competences.Select(c => c.Nom)).Distinct().Take(5).ToList()
                });
            }
        }

        return result.OrderByDescending(p => p.NiveauRisque).ToList();
    }

    private async Task<List<ProgressionData>> CalculateProgressionTimeAsync()
    {
        // Simulation de données de progression (à remplacer par vraies données historiques)
        return new List<ProgressionData>
        {
            new ProgressionData { Mois = "M-6", NiveauMoyen = 2.1 },
            new ProgressionData { Mois = "M-5", NiveauMoyen = 2.3 },
            new ProgressionData { Mois = "M-4", NiveauMoyen = 2.6 },
            new ProgressionData { Mois = "M-3", NiveauMoyen = 2.9 },
            new ProgressionData { Mois = "M-2", NiveauMoyen = 3.2 },
            new ProgressionData { Mois = "M-1", NiveauMoyen = 3.5 }
        };
    }
}

public class PosteCritiqueViewModel
{
    public string? Poste { get; set; }
    public string? Departement { get; set; }
    public int NbTitulaires { get; set; }
    public int NbRemplacants { get; set; }
    public string? NiveauRisque { get; set; }
    public int ReadinessScore { get; set; }
    public List<string> CompetencesCles { get; set; } = new();
}

public class ProgressionData
{
    public string? Mois { get; set; }
    public double NiveauMoyen { get; set; }
}
