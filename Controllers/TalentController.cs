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
        
        // Top Talents (Stars et Future Leaders)
        var topTalents = await _context.TalentEvaluations
            .Include(t => t.Collaborateur)
            .Where(t => t.Actif && (t.Category == NineBoxCategory.Star || t.Category == NineBoxCategory.FutureLeader))
            .OrderByDescending(t => t.PerformanceScore + t.PotentielScore)
            .Take(8)
            .ToListAsync();
        ViewBag.TopTalents = topTalents;

        // Collaborateurs à risque (Underperformers)
        var atRisk = await _context.TalentEvaluations
            .Include(t => t.Collaborateur)
            .Where(t => t.Actif && t.Category == NineBoxCategory.Underperformer)
            .ToListAsync();
        ViewBag.AtRisk = atRisk;

        // Succession Readiness
        var readyForSuccession = await _context.Collaborateurs
            .Where(c => c.Actif && c.Grade == "Senior")
            .CountAsync();
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
        var boxDistribution = await _context.TalentEvaluations
            .Where(t => t.Actif)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToListAsync();
        ViewBag.BoxDistribution = boxDistribution;

        return View();
    }

    // =========================
    // MATRICE 9-BOX
    // =========================
    public async Task<IActionResult> Matrix9Box(string? departement = null, string? grade = null)
    {
        var query = _context.TalentEvaluations
            .Include(t => t.Collaborateur)
            .Where(t => t.Actif)
            .AsQueryable();

        if (!string.IsNullOrEmpty(departement))
            query = query.Where(t => t.Collaborateur!.Departement == departement);
        
        if (!string.IsNullOrEmpty(grade))
            query = query.Where(t => t.Collaborateur!.Grade == grade);

        var evaluations = await query.ToListAsync();

        // Organiser par catégorie 9-box
        var matrix = new Dictionary<NineBoxCategory, List<TalentEvaluation>>();
        foreach (var cat in Enum.GetValues<NineBoxCategory>())
        {
            matrix[cat] = evaluations.Where(e => e.Category == cat).ToList();
        }

        ViewBag.Matrix = matrix;
        ViewBag.Departements = await _context.Collaborateurs
            .Select(c => c.Departement)
            .Distinct()
            .ToListAsync();
        ViewBag.Grades = new[] { "Junior", "Senior", "Manager" };

        return View();
    }

    // GET: Talent/Evaluate/5
    public async Task<IActionResult> Evaluate(int collaborateurId)
    {
        var collaborateur = await _context.Collaborateurs
            .Include(c => c.Competences)
            .Include(c => c.Inscriptions)
                .ThenInclude(i => i.Formation)
            .FirstOrDefaultAsync(c => c.Id == collaborateurId);

        if (collaborateur == null) return NotFound();

        // Calculer scores automatiques
        var autoPerformance = CalculatePerformanceScore(collaborateur);
        var autoPotentiel = CalculatePotentielScore(collaborateur);

        ViewBag.Collaborateur = collaborateur;
        ViewBag.AutoPerformance = autoPerformance;
        ViewBag.AutoPotentiel = autoPotentiel;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Evaluate(int collaborateurId, int performanceScore, int potentielScore, 
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

        TempData["Success"] = "Évaluation talent enregistrée avec succès.";
        return RedirectToAction(nameof(Matrix9Box));
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
