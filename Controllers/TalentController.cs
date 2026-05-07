using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;

namespace SIRH.EY.Controllers;

public class TalentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TalentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // =========================
    // DASHBOARD TALENT
    // =========================
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        
        var collaborateurs = await _context.Collaborateurs
            .Include(c => c.Competences)
            .Include(c => c.Inscriptions)
            .Where(c => c.Actif)
            .ToListAsync();

        // Évaluation dynamique pour tous
        var evalDynamique = collaborateurs.Select(c => new TalentEvalDynViewModel {
            Collaborateur = c,
            Perf = CalculatePerformanceScore(c),
            Pot = CalculatePotentielScore(c),
            Cat = Calculate9BoxCategory(CalculatePerformanceScore(c), CalculatePotentielScore(c)),
            Moyenne = c.Competences?.Any() == true ? Math.Round(c.Competences.Average(comp => comp.NiveauActuel), 1) : 0
        }).ToList();

        // Meilleurs Talents (Stars et Future Leaders) -> Moyenne >= 4
        var topTalents = evalDynamique
            .Where(e => e.Moyenne >= 4)
            .OrderByDescending(e => e.Moyenne)
            .Take(8)
            .Select(e => new TopTalentViewModel {
                Collaborateur = e.Collaborateur,
                ScoreGlobal = e.Moyenne,
                PerformanceScore = e.Perf,
                PotentielScore = e.Pot,
                Category = e.Cat,
                Badge = e.Moyenne >= 4.5 ? "Talent stratégique" : (e.Collaborateur?.Grade == "Manager" ? "Expert confirmé" : "Leader émergent")
            })
            .ToList();
        ViewBag.TopTalents = topTalents;

        // Collaborateurs à risque (moyenne < 2)
        var atRisk = evalDynamique
            .Where(e => e.Moyenne > 0 && e.Moyenne < 2)
            .Select(e => new AtRiskViewModel {
                Collaborateur = e.Collaborateur,
                Moyenne = e.Moyenne
            })
            .ToList();
        ViewBag.AtRisk = atRisk;

        // Succession Readiness
        var readyForSuccession = collaborateurs
            .Count(c => c.Grade == "Senior" || c.Grade == "Manager");
        ViewBag.SuccessionReady = readyForSuccession;

        // Stats OKR
        var totalOKRs = await _context.OKRs.CountAsync();
        var completedOKRs = await _context.OKRs.CountAsync(o => o.Statut == OKRStatut.Completed);
        var atRiskOKRs = await _context.OKRs.CountAsync(o => o.Statut == OKRStatut.AtRisk);
        
        ViewBag.TotalOKRs = totalOKRs;
        ViewBag.CompletedOKRs = completedOKRs;
        ViewBag.AtRiskOKRs = atRiskOKRs;
        ViewBag.OKRSuccessRate = totalOKRs > 0 ? (int)((double)completedOKRs / totalOKRs * 100) : 0;

        // Distribution 9-Box pour chart
        var boxDistribution = evalDynamique
            .GroupBy(e => e.Cat)
            .Select(g => new BoxDistViewModel { Category = g.Key.GetDisplayName(), Count = g.Count() })
            .ToList();
        ViewBag.BoxDistribution = boxDistribution;

        return View();
    }

    // =========================
    // MATRICE 9-BOX
    // =========================
    public async Task<IActionResult> Matrix9Box(string? departement = null, string? grade = null)
    {
        var query = _context.Collaborateurs
            .Include(c => c.Competences)
            .Include(c => c.Inscriptions)
            .Where(c => c.Actif)
            .AsQueryable();

        if (!string.IsNullOrEmpty(departement))
            query = query.Where(c => c.Departement == departement);
        
        if (!string.IsNullOrEmpty(grade))
            query = query.Where(c => c.Grade == grade);

        var collaborateurs = await query.ToListAsync();

        // Récupérer les évaluations manuelles existantes
        var manualEvaluations = await _context.TalentEvaluations
            .Where(t => t.Actif)
            .ToListAsync();

        var matrix = new Dictionary<NineBoxCategory, List<MatrixItemViewModel>>();
        foreach (var cat in Enum.GetValues<NineBoxCategory>())
        {
            matrix[cat] = new List<MatrixItemViewModel>();
        }

        foreach(var c in collaborateurs)
        {
            var manualEval = manualEvaluations.OrderByDescending(e => e.DateEvaluation).FirstOrDefault(e => e.CollaborateurId == c.Id);
            
            int perf = manualEval?.PerformanceScore ?? CalculatePerformanceScore(c);
            int pot = manualEval?.PotentielScore ?? CalculatePotentielScore(c);
            var category = manualEval?.Category ?? Calculate9BoxCategory(perf, pot);

            matrix[category].Add(new MatrixItemViewModel {
                Collaborateur = c,
                Perf = perf,
                Pot = pot,
                HasManualEval = manualEval != null
            });
        }

        ViewBag.Matrix = matrix;
        ViewBag.Departements = await _context.Collaborateurs
            .Select(c => c.Departement)
            .Distinct()
            .ToListAsync();
        ViewBag.Grades = new[] { "Junior", "Senior", "Manager", "Director" };

        return View();
    }

    // API GET pour charger les détails collaborateur dans le panel AJAX
    [HttpGet]
    public async Task<IActionResult> GetCollaborateurDetails(int id)
    {
        var collaborateur = await _context.Collaborateurs
            .Include(c => c.Competences)
            .Include(c => c.Inscriptions)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (collaborateur == null) return NotFound();

        var autoPerformance = CalculatePerformanceScore(collaborateur);
        var autoPotentiel = CalculatePotentielScore(collaborateur);
        var moyenne = collaborateur.Competences?.Any() == true ? Math.Round(collaborateur.Competences.Average(comp => comp.NiveauActuel), 1) : 0;
        var recommendedCat = Calculate9BoxCategory(autoPerformance, autoPotentiel);

        var topCompetences = collaborateur.Competences?
            .OrderByDescending(c => c.NiveauActuel)
            .Take(3)
            .ToList() ?? new List<Competence>();

        var competencesFaibles = collaborateur.Competences?
            .OrderBy(c => c.NiveauActuel)
            .Take(3)
            .ToList() ?? new List<Competence>();

        var totalCompetences = collaborateur.Competences?.Count ?? 0;
        var competencesValidees = collaborateur.Competences?.Count(c => c.NiveauActuel >= c.NiveauCible) ?? 0;
        var competencesCritiques = collaborateur.Competences?.Count(c => c.NiveauCible - c.NiveauActuel >= 2) ?? 0;
        var derniereEval = collaborateur.Competences?.Max(c => (DateTime?)c.DateEvaluation);
        var tauxCompletion = totalCompetences > 0 ? (competencesValidees * 100) / totalCompetences : 0;

        return PartialView("_EvaluatePanel", new EvaluatePanelViewModel {
            Collaborateur = collaborateur,
            AutoPerformance = autoPerformance,
            AutoPotentiel = autoPotentiel,
            Moyenne = moyenne,
            RecommendedCategory = recommendedCat.GetDisplayName(),
            TopCompetences = topCompetences,
            CompetencesADevelopper = competencesFaibles,
            TotalCompetences = totalCompetences,
            CompetencesValidees = competencesValidees,
            CompetencesCritiques = competencesCritiques,
            DerniereEvaluation = derniereEval,
            TauxCompletion = tauxCompletion
        });
    }

    [HttpPost]
    public async Task<IActionResult> EvaluateAjax(int collaborateurId, int performanceScore, int potentielScore, 
        string? commentairesPerformance, string? commentairesPotentiel)
    {
        var user = await _userManager.GetUserAsync(User);
        
        var evaluation = new TalentEvaluation
        {
            CollaborateurId = collaborateurId,
            PerformanceScore = performanceScore,
            PotentielScore = potentielScore,
            Category = Calculate9BoxCategory(performanceScore, potentielScore),
            CommentairesPerformance = commentairesPerformance,
            CommentairesPotentiel = commentairesPotentiel,
            EvaluateurId = user?.Id,
            DateEvaluation = DateTime.Now
        };

        _context.TalentEvaluations.Add(evaluation);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Évaluation enregistrée avec succès." });
    }

    // =========================
    // MODULE OKR
    // =========================
    public async Task<IActionResult> MyOKRs(int? collaborateurId = null)
    {
        var user = await _userManager.GetUserAsync(User);
        
        // Déterminer le collaborateur
        Collaborateur? collaborateur = null;
        if (collaborateurId.HasValue)
        {
            collaborateur = await _context.Collaborateurs.FindAsync(collaborateurId);
        }
        else
        {
            collaborateur = await _context.Collaborateurs
                .FirstOrDefaultAsync(c => c.UserId == user!.Id);
        }

        if (collaborateur == null) return NotFound();

        var okrs = await _context.OKRs
            .Include(o => o.KeyResults)
            .Where(o => o.CollaborateurId == collaborateur.Id)
            .OrderByDescending(o => o.Annee)
            .ThenBy(o => o.Trimestre)
            .ToListAsync();

        ViewBag.Collaborateur = collaborateur;
        ViewBag.CurrentYear = DateTime.Now.Year;

        return View(okrs);
    }

    public IActionResult CreateOKR(int collaborateurId)
    {
        ViewBag.CollaborateurId = collaborateurId;
        ViewBag.CurrentYear = DateTime.Now.Year;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateOKR(int collaborateurId, string objectif, string description,
        int annee, Trimestre trimestre, DateTime dateFinCible,
        List<string> keyResultDescriptions, List<double> keyResultTargets)
    {
        var user = await _userManager.GetUserAsync(User);

        var okr = new OKR
        {
            CollaborateurId = collaborateurId,
            Objectif = objectif,
            Description = description,
            Annee = annee,
            Trimestre = trimestre,
            DateFinCible = dateFinCible,
            ManagerId = user?.Id,
            Statut = OKRStatut.Draft
        };

        // Ajouter les Key Results
        for (int i = 0; i < keyResultDescriptions.Count; i++)
        {
            okr.KeyResults.Add(new KeyResult
            {
                Description = keyResultDescriptions[i],
                ValeurCible = keyResultTargets[i],
                Ordre = i
            });
        }

        _context.OKRs.Add(okr);
        await _context.SaveChangesAsync();

        TempData["Success"] = "OKR créé avec succès.";
        return RedirectToAction(nameof(MyOKRs), new { collaborateurId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateKeyResult(int keyResultId, double valeurActuelle)
    {
        var kr = await _context.KeyResults.FindAsync(keyResultId);
        if (kr == null) return NotFound();

        kr.ValeurActuelle = valeurActuelle;
        kr.Statut = valeurActuelle >= kr.ValeurCible ? KeyResultStatut.Completed : KeyResultStatut.InProgress;

        // Recalculer progression OKR
        var okr = await _context.OKRs
            .Include(o => o.KeyResults)
            .FirstOrDefaultAsync(o => o.Id == kr.OKRId);

        if (okr != null)
        {
            var totalProgression = okr.KeyResults.Sum(k => k.Progression);
            okr.ProgressionGlobale = okr.KeyResults.Any() ? totalProgression / okr.KeyResults.Count : 0;
            
            // Mettre à jour statut
            if (okr.ProgressionGlobale >= 100)
                okr.Statut = OKRStatut.Completed;
            else if (okr.ProgressionGlobale >= 70)
                okr.Statut = OKRStatut.OnTrack;
            else if (okr.ProgressionGlobale >= 30)
                okr.Statut = OKRStatut.Active;
            else
                okr.Statut = OKRStatut.AtRisk;
        }

        await _context.SaveChangesAsync();

        return Json(new { success = true, progression = kr.Progression });
    }

    [HttpPost]
    public async Task<IActionResult> ValidateOKR(int okrId)
    {
        var okr = await _context.OKRs.FindAsync(okrId);
        if (okr == null) return NotFound();

        okr.ValideParManager = true;
        okr.DateValidation = DateTime.Now;
        okr.Statut = OKRStatut.Active;

        await _context.SaveChangesAsync();

        TempData["Success"] = "OKR validé avec succès.";
        return RedirectToAction(nameof(MyOKRs), new { collaborateurId = okr.CollaborateurId });
    }

    // =========================
    // CALCULS
    // =========================
    private int CalculatePerformanceScore(Collaborateur c)
    {
        int score = 3; // Base
        
        // Auto-évaluations (moyenne des compétences)
        if (c.Competences?.Any() == true)
        {
            var avgCompetence = c.Competences.Average(comp => comp.NiveauActuel);
            score += (int)Math.Round(avgCompetence / 5.0 * 2); // 0-2 points
        }

        // Formations complétées
        if (c.Inscriptions?.Any() == true)
        {
            var formationRate = c.Inscriptions.Count(i => i.Terminee) / (double)c.Inscriptions.Count();
            score += formationRate > 0.8 ? 1 : 0;
        }

        return Math.Min(5, score);
    }

    private int CalculatePotentielScore(Collaborateur c)
    {
        int score = 3; // Base

        // Progression rapide (si ancienneté < 2 ans et déjà bon grade)
        var anciennete = (DateTime.Now - c.DateEmbauche).TotalDays / 365;
        if (anciennete < 2 && (c.Grade == "Senior" || c.Grade == "Manager"))
            score += 1;

        // Formations certifiantes (simulation)
        if (c.Inscriptions?.Any(i => i.Terminee) == true)
            score += 1;

        return Math.Min(5, score);
    }

    private NineBoxCategory Calculate9BoxCategory(int performance, int potentiel)
    {
        // Matrice 9-box standard
        return (performance, potentiel) switch
        {
            ( >= 4, >= 4 ) => NineBoxCategory.Star,
            ( >= 4, 3 ) => NineBoxCategory.FutureLeader,
            ( >= 4, <= 2 ) => NineBoxCategory.HighProfessional,
            ( 3, >= 4 ) => NineBoxCategory.EmergingTalent,
            ( 3, 3 ) => NineBoxCategory.SolidProfessional,
            ( 3, <= 2 ) => NineBoxCategory.InPlace,
            ( <= 2, >= 4 ) => NineBoxCategory.RisingStar,
            ( <= 2, 3 ) => NineBoxCategory.NeedDevelopment,
            _ => NineBoxCategory.Underperformer
        };
    }
}

public class TalentEvalDynViewModel
{
    public Collaborateur Collaborateur { get; set; }
    public int Perf { get; set; }
    public int Pot { get; set; }
    public NineBoxCategory Cat { get; set; }
    public double Moyenne { get; set; }
}

public class TopTalentViewModel
{
    public Collaborateur Collaborateur { get; set; }
    public double ScoreGlobal { get; set; }
    public int PerformanceScore { get; set; }
    public int PotentielScore { get; set; }
    public NineBoxCategory Category { get; set; }
    public string Badge { get; set; }
}

public class AtRiskViewModel
{
    public Collaborateur Collaborateur { get; set; }
    public double Moyenne { get; set; }
}

public class BoxDistViewModel
{
    public string Category { get; set; }
    public int Count { get; set; }
}

public class MatrixItemViewModel
{
    public Collaborateur Collaborateur { get; set; }
    public int Perf { get; set; }
    public int Pot { get; set; }
    public bool HasManualEval { get; set; }
}

public class EvaluatePanelViewModel
{
    public Collaborateur Collaborateur { get; set; }
    public int AutoPerformance { get; set; }
    public int AutoPotentiel { get; set; }
    public double Moyenne { get; set; }
    public string RecommendedCategory { get; set; }
    public List<Competence> TopCompetences { get; set; } = new();
    public List<Competence> CompetencesADevelopper { get; set; } = new();
    
    // Nouveaux KPIs RH
    public int TotalCompetences { get; set; }
    public int CompetencesValidees { get; set; }
    public int CompetencesCritiques { get; set; }
    public DateTime? DerniereEvaluation { get; set; }
    public int TauxCompletion { get; set; }
}
