using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;
using SIRH.EY.Services;
using System.Text.Json;

namespace SIRH.EY.Controllers;

public class CompetencesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IReferentielRhService _referentielRhService;
    private readonly IPlanDeveloppementService _planDeveloppementService;

    // Définition des secteurs EY avec leurs compétences
    private static readonly List<SecteurEYDefinition> SecteursEY = new()
    {
        new SecteurEYDefinition {
            Id = "assurance", Nom = "Assurance", IconClass = "fas fa-search-dollar",
            Competences = new List<SecteurCompetenceDefinition> {
                new SecteurCompetenceDefinition { Nom = "Audit financier", Categorie = "Audit", NiveauRequis = 4, PosteCible = "Auditeur Senior" },
                new SecteurCompetenceDefinition { Nom = "Audit interne", Categorie = "Audit", NiveauRequis = 4, PosteCible = "Auditeur Interne" },
                new SecteurCompetenceDefinition { Nom = "Contrôle interne", Categorie = "Risk", NiveauRequis = 4, PosteCible = "Contrôleur" },
                new SecteurCompetenceDefinition { Nom = "IFRS", Categorie = "Audit", NiveauRequis = 5, PosteCible = "Expert Comptable" },
                new SecteurCompetenceDefinition { Nom = "Risk Assessment", Categorie = "Risk", NiveauRequis = 4, PosteCible = "Risk Manager" },
                new SecteurCompetenceDefinition { Nom = "Compliance", Categorie = "Risk", NiveauRequis = 3, PosteCible = "Compliance Officer" }
            }
        },
        new SecteurEYDefinition {
            Id = "consulting", Nom = "Consulting", IconClass = "fas fa-lightbulb",
            Competences = new List<SecteurCompetenceDefinition> {
                new SecteurCompetenceDefinition { Nom = "Business Process Reengineering", Categorie = "Méthodes", NiveauRequis = 4, PosteCible = "Consultant Senior" },
                new SecteurCompetenceDefinition { Nom = "BRD Execution", Categorie = "Méthodes", NiveauRequis = 4, PosteCible = "Business Analyst" },
                new SecteurCompetenceDefinition { Nom = "Requirements Gathering", Categorie = "Méthodes", NiveauRequis = 4, PosteCible = "Consultant" },
                new SecteurCompetenceDefinition { Nom = "Stakeholder Management", Categorie = "Soft skills", NiveauRequis = 5, PosteCible = "Manager" },
                new SecteurCompetenceDefinition { Nom = "Change Management", Categorie = "Management", NiveauRequis = 4, PosteCible = "Change Manager" },
                new SecteurCompetenceDefinition { Nom = "Project Management", Categorie = "Management", NiveauRequis = 4, PosteCible = "Chef de Projet" }
            }
        },
        new SecteurEYDefinition {
            Id = "strategy", Nom = "Strategy & Transactions", IconClass = "fas fa-chess",
            Competences = new List<SecteurCompetenceDefinition> {
                new SecteurCompetenceDefinition { Nom = "Financial Modeling", Categorie = "Finance", NiveauRequis = 5, PosteCible = "Analyste Financier" },
                new SecteurCompetenceDefinition { Nom = "Valuation", Categorie = "Finance", NiveauRequis = 5, PosteCible = "Expert Evaluation" },
                new SecteurCompetenceDefinition { Nom = "Due Diligence", Categorie = "Audit", NiveauRequis = 4, PosteCible = "Consultant M&A" },
                new SecteurCompetenceDefinition { Nom = "Market Analysis", Categorie = "Strategy", NiveauRequis = 4, PosteCible = "Strategist" },
                new SecteurCompetenceDefinition { Nom = "Investment Analysis", Categorie = "Finance", NiveauRequis = 4, PosteCible = "Investment Analyst" }
            }
        },
        new SecteurEYDefinition {
            Id = "tax", Nom = "TAX", IconClass = "fas fa-file-invoice-dollar",
            Competences = new List<SecteurCompetenceDefinition> {
                new SecteurCompetenceDefinition { Nom = "Fiscalité internationale", Categorie = "Fiscalité", NiveauRequis = 5, PosteCible = "Expert Fiscal" },
                new SecteurCompetenceDefinition { Nom = "Transfer Pricing", Categorie = "Fiscalité", NiveauRequis = 5, PosteCible = "Transfer Pricing Manager" },
                new SecteurCompetenceDefinition { Nom = "Tax Compliance", Categorie = "Fiscalité", NiveauRequis = 4, PosteCible = "Tax Manager" },
                new SecteurCompetenceDefinition { Nom = "VAT/GST", Categorie = "Fiscalité", NiveauRequis = 4, PosteCible = "Tax Consultant" },
                new SecteurCompetenceDefinition { Nom = "Corporate Tax", Categorie = "Fiscalité", NiveauRequis = 4, PosteCible = "Tax Advisor" }
            }
        },
        new SecteurEYDefinition {
            Id = "cbs", Nom = "CBS Support", IconClass = "fas fa-laptop-code",
            Competences = new List<SecteurCompetenceDefinition> {
                new SecteurCompetenceDefinition { Nom = "Azure Functions", Categorie = "Tech", NiveauRequis = 4, PosteCible = "Développeur Azure" },
                new SecteurCompetenceDefinition { Nom = "Azure Service Bus", Categorie = "Tech", NiveauRequis = 4, PosteCible = "Architecte Cloud" },
                new SecteurCompetenceDefinition { Nom = "Power Automate", Categorie = "Outils", NiveauRequis = 3, PosteCible = "Automation Specialist" },
                new SecteurCompetenceDefinition { Nom = "Customization D365", Categorie = "Tech", NiveauRequis = 5, PosteCible = "Développeur D365" },
                new SecteurCompetenceDefinition { Nom = "Data Migration", Categorie = "Tech", NiveauRequis = 4, PosteCible = "Data Engineer" },
                new SecteurCompetenceDefinition { Nom = "Inbound/Outbound Integration", Categorie = "Tech", NiveauRequis = 5, PosteCible = "Integration Architect" },
                new SecteurCompetenceDefinition { Nom = "RCA", Categorie = "Méthodes", NiveauRequis = 3, PosteCible = "Support Engineer" },
                new SecteurCompetenceDefinition { Nom = "Technical Design Document", Categorie = "Méthodes", NiveauRequis = 4, PosteCible = "Technical Lead" },
                new SecteurCompetenceDefinition { Nom = "Bilingual Arabic/English", Categorie = "Soft skills", NiveauRequis = 5, PosteCible = "Tous postes" }
            }
        }
    };

    public CompetencesController(
        ApplicationDbContext context,
        IReferentielRhService referentielRhService,
        IPlanDeveloppementService planDeveloppementService)
    {
        _context = context;
        _referentielRhService = referentielRhService;
        _planDeveloppementService = planDeveloppementService;
    }

    // GET: Competences?collaborateurId=xx
    // GET: Competences?collaborateurId=xx
public async Task<IActionResult> Index(int? collaborateurId, string categorie)
{
    if (collaborateurId == null)
    {
        ViewBag.Message = "Veuillez sÃ©lectionner un collaborateur.";
        return View(new List<Competence>());
    }

    var collaborateur = await _context.Collaborateurs.FindAsync(collaborateurId);
    if (collaborateur == null) return NotFound();

    ViewBag.CollaborateurNom = collaborateur.Prenom + " " + collaborateur.Nom;
    ViewBag.CollaborateurId = collaborateurId;
    ViewBag.GradeCollaborateur = collaborateur.Grade;

    // ===== RÃ©cupÃ©rer les compÃ©tences requises pour le poste =====
    // Assurez-vous que la table CompetencesRequisesParPoste existe (migration faite)
    var competencesRequisesPoste = await _context.CompetencesRequisesParPoste
        .Where(cr => cr.Poste == collaborateur.Poste)
        .ToListAsync();
    ViewBag.CompetencesRequisesPoste = competencesRequisesPoste;
    // ==========================================================

    var competences = _context.Competences
        .Include(c => c.EvaluationCompetence)
        .Include(c => c.CategorieCompetence)
        .Where(c => c.CollaborateurId == collaborateurId);
    if (!string.IsNullOrEmpty(categorie))
    {
        competences = competences.Where(c => c.CategorieCompetence != null && c.CategorieCompetence.Nom == categorie);
    }
    var liste = await competences.ToListAsync();

    var categories = await _context.CategoriesCompetences.Select(c => c.Nom).ToListAsync();
    ViewBag.Categories = categories;
    ViewBag.CategorieCourante = categorie;

    var plans = await _context.PlansDeveloppement
    .Include(p => p.Formation)
    .Where(p => p.CollaborateurId == collaborateurId)
    .ToListAsync();
ViewBag.PlanDeveloppement = plans;

    return View(liste);
}

// GET: Competences/Catalogue/5
public async Task<IActionResult> Catalogue(int? collaborateurId)
{
    if (collaborateurId == null) return NotFound();
    
    var collaborateur = await _context.Collaborateurs
        .Include(c => c.Competences)
        .FirstOrDefaultAsync(c => c.Id == collaborateurId);
    
    if (collaborateur == null) return NotFound();

    // Récupérer les compétences existantes du collaborateur
    var competencesExistantes = collaborateur.Competences?.ToList() ?? new List<Competence>();
    var nomsCompetencesExistantes = competencesExistantes
        .Where(c => !string.IsNullOrWhiteSpace(c.Nom))
        .Select(c => c.Nom.Trim().ToLower())
        .ToHashSet();

    // Construire le modèle avec les secteurs EY
    var model = new CompetenceCatalogViewModel
    {
        CollaborateurId = collaborateur.Id,
        CollaborateurNom = $"{collaborateur.Prenom} {collaborateur.Nom}",
        Grade = collaborateur.Grade,
        Poste = collaborateur.Poste,
        Departement = collaborateur.Departement,
        CompetencesExistantes = competencesExistantes
    };

    // Pour chaque secteur EY, construire la liste des compétences
    foreach (var secteurDef in SecteursEY)
    {
        var secteur = new SecteurEY
        {
            Id = secteurDef.Id,
            Nom = secteurDef.Nom,
            IconClass = secteurDef.IconClass
        };

        foreach (var compDef in secteurDef.Competences)
        {
            var estDejaAcquise = nomsCompetencesExistantes.Contains(compDef.Nom.Trim().ToLower());
            var competenceExistant = competencesExistantes
                .FirstOrDefault(c => string.Equals(c.Nom, compDef.Nom, StringComparison.OrdinalIgnoreCase));

            secteur.Competences.Add(new CompetenceCardViewModel
            {
                Id = competenceExistant?.Id ?? 0, // 0 si pas encore dans la base
                Nom = compDef.Nom,
                Categorie = compDef.Categorie,
                NiveauRequis = compDef.NiveauRequis,
                PosteCible = compDef.PosteCible,
                EstDejaAcquise = estDejaAcquise,
                NiveauActuel = competenceExistant?.NiveauActuel ?? 0,
                TypeCompetence = GetCompetenceType(compDef.Categorie)
            });
        }

        model.Secteurs.Add(secteur);
    }

    return View(model);
}

// POST: Competences/AjouterCompetencesCatalogue
[HttpPost]
[Route("api/competences/ajouter-catalogue")]
public async Task<IActionResult> AjouterCompetencesCatalogue([FromBody] JsonElement requestData)
{
    try
    {
        int collaborateurId = requestData.TryGetProperty("collaborateurId", out var idElem) ? idElem.GetInt32() : 0;
        if (collaborateurId <= 0) return BadRequest(new { success = false, message = "Collaborateur invalide" });

        var collaborateur = await _context.Collaborateurs.FindAsync(collaborateurId);
        if (collaborateur == null) return NotFound(new { success = false, message = "Collaborateur introuvable" });

        if (!requestData.TryGetProperty("competences", out var competencesElem) || !competencesElem.EnumerateArray().Any())
            return BadRequest(new { success = false, message = "Aucune compétence sélectionnée" });

        var competencesAAjouter = competencesElem.EnumerateArray().ToList();
        int addedCount = 0;

        foreach (var compElem in competencesAAjouter)
        {
            var nom = compElem.GetProperty("nom").GetString()?.Trim();
            var niveau = compElem.GetProperty("niveau").TryGetInt32(out var n) ? n : 3;

            if (string.IsNullOrWhiteSpace(nom)) continue;

            // Vérifier si la compétence existe déjà
            var existe = await _context.Competences
                .AnyAsync(c => c.CollaborateurId == collaborateurId && 
                              string.Equals(c.Nom, nom, StringComparison.OrdinalIgnoreCase));
            
            if (!existe)
            {
                // Trouver la catégorie correspondante ou créer "Tech/Fonctionnel" par défaut
                int? categorieId = null;
                var categorieNom = GetDefaultCategorie(nom);
                var categorie = await _context.CategoriesCompetences
                    .FirstOrDefaultAsync(cat => cat.Nom == categorieNom);
                
                if (categorie != null)
                    categorieId = categorie.Id;

                _context.Competences.Add(new Competence
                {
                    Nom = nom,
                    CollaborateurId = collaborateurId,
                    NiveauActuel = 2, // Niveau par défaut à l'ajout
                    NiveauCible = niveau,
                    CategorieCompetenceId = categorieId,
                    DateEvaluation = DateTime.Now
                });
                addedCount++;
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, count = addedCount });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}

private string GetCompetenceType(string? categorie)
{
    if (string.IsNullOrWhiteSpace(categorie)) return "Technique";
    
    var techCategories = new[] { "Tech", "Outils", "Data", "Audit" };
    var fonctionnelCategories = new[] { "Méthodes", "Management", "Soft skills", "RH" };
    
    if (techCategories.Any(t => categorie.Contains(t, StringComparison.OrdinalIgnoreCase)))
        return "Technique";
    if (fonctionnelCategories.Any(f => categorie.Contains(f, StringComparison.OrdinalIgnoreCase)))
        return "Fonctionnel";
    
    return "Transverse";
}

private string GetDefaultCategorie(string competenceNom)
{
    var lower = competenceNom.ToLower();
    if (lower.Contains("azure") || lower.Contains("d365") || lower.Contains("power automate") || 
        lower.Contains("integration") || lower.Contains("data migration"))
        return "Tech";
    if (lower.Contains("audit") || lower.Contains("ifrs") || lower.Contains("financial"))
        return "Audit";
    if (lower.Contains("fiscal") || lower.Contains("tax") || lower.Contains("vat") || lower.Contains("transfer pricing"))
        return "Fiscalité";
    if (lower.Contains("management") || lower.Contains("stakeholder") || lower.Contains("change"))
        return "Management";
    if (lower.Contains("brd") || lower.Contains("requirements") || lower.Contains("business process"))
        return "Méthodes";
    
    return "Métier";
}

public async Task<IActionResult> MatriceEquipe(int? collaborateurId)
{
    // Si un collaborateur est passÃ©, on prend son dÃ©partement
    var departement = "";
    if (collaborateurId.HasValue)
    {
        var collab = await _context.Collaborateurs.FindAsync(collaborateurId.Value);
        if (collab != null) departement = collab.Departement;
    }

    var collaborateurs = await _context.Collaborateurs
        .Where(c => c.Actif && (string.IsNullOrEmpty(departement) || c.Departement == departement))
        .ToListAsync();

    var toutesCompetences = await _context.Competences
        .Include(c => c.Collaborateur)
        .ToListAsync();

    var viewModel = new MatriceEquipeViewModel
    {
        Collaborateurs = collaborateurs,
        CompetencesParCollaborateur = toutesCompetences
            .Where(c => collaborateurs.Select(coll => coll.Id).Contains(c.CollaborateurId))
            .GroupBy(c => c.CollaborateurId)
            .ToDictionary(g => g.Key, g => g.ToList())
    };
    ViewBag.CollaborateurId = collaborateurId;
    return View(viewModel);
}

    // GET: Competences/Evaluate/5
    public async Task<IActionResult> Evaluate(int id)
    {
        var competence = await _context.Competences.FindAsync(id);
        if (competence == null) return NotFound();
        return PartialView("_EvaluateModal", competence);
    }

    // POST: Competences/Evaluate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Evaluate(int id, int nouveauNiveau, DateTime dateEvaluation)
    {
        var competence = await _context.Competences.FindAsync(id);
        if (competence == null) return NotFound();
        competence.NiveauActuel = nouveauNiveau;
        competence.DateEvaluation = dateEvaluation;
        _context.Update(competence);
        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // GET: Competences/AutoEvaluation/5
    public async Task<IActionResult> AutoEvaluation(int id)
    {
        var competence = await _context.Competences
            .Include(c => c.Collaborateur)
            .Include(c => c.EvaluationCompetence)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (competence == null) return NotFound();

        var evaluation = competence.EvaluationCompetence;
        var seuilRh = evaluation?.SeuilRh ?? await GetSeuilRhAsync(competence);

        var vm = new AutoEvaluationCompetenceViewModel
        {
            CompetenceId = competence.Id,
            CollaborateurId = competence.CollaborateurId,
            CompetenceNom = competence.Nom,
            CollaborateurNom = $"{competence.Collaborateur?.Prenom} {competence.Collaborateur?.Nom}".Trim(),
            Poste = competence.Collaborateur?.Poste,
            Categorie = competence.CategorieCompetence?.Nom,
            SeuilRh = seuilRh,
            AutoEvaluationCollaborateur = evaluation?.AutoEvaluationCollaborateur ?? Math.Clamp(competence.NiveauActuel * 20, 0, 100),
            EvaluationManager = evaluation?.EvaluationManager,
            ValidationManager = evaluation?.ValidationManager ?? false,
            CommentaireCollaborateur = evaluation?.CommentaireCollaborateur,
            CommentaireManager = evaluation?.CommentaireManager
        };

        return View(vm);
    }

    // POST: Competences/AutoEvaluation
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AutoEvaluation(AutoEvaluationCompetenceViewModel vm)
    {
        var competence = await _context.Competences
            .Include(c => c.EvaluationCompetence)
            .FirstOrDefaultAsync(c => c.Id == vm.CompetenceId);
        if (competence == null) return NotFound();

        if (!ModelState.IsValid)
        {
            var collaborateur = await _context.Collaborateurs.FindAsync(competence.CollaborateurId);
            vm.CollaborateurId = competence.CollaborateurId;
            vm.CompetenceNom = competence.Nom;
            vm.CollaborateurNom = collaborateur == null ? "" : $"{collaborateur.Prenom} {collaborateur.Nom}";
            vm.Poste = collaborateur?.Poste;
            vm.Categorie = competence.CategorieCompetence?.Nom;
            return View(vm);
        }

        var evaluation = competence.EvaluationCompetence;
        if (evaluation == null)
        {
            evaluation = new EvaluationCompetence
            {
                CompetenceId = competence.Id
            };
            _context.EvaluationsCompetences.Add(evaluation);
        }

        evaluation.SeuilRh = vm.SeuilRh;
        evaluation.AutoEvaluationCollaborateur = vm.AutoEvaluationCollaborateur;
        evaluation.CommentaireCollaborateur = vm.CommentaireCollaborateur;
        evaluation.DateAutoEvaluation = DateTime.Now;
        evaluation.ValidationManager = false;
        evaluation.DateValidationManager = null;
        evaluation.CommentaireManager = null;
        evaluation.EvaluationManager = null;

        competence.NiveauActuel = Math.Max(1, Math.Min(5, (int)Math.Ceiling(vm.AutoEvaluationCollaborateur / 20.0)));
        competence.DateEvaluation = DateTime.Now;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Auto-Ã©valuation enregistrÃ©e. Le manager pourra maintenant la valider ou l'ajuster.";
        return RedirectToAction(nameof(Index), new { collaborateurId = competence.CollaborateurId });
    }

    // GET: Competences/ValidationManager/5
    public async Task<IActionResult> ValidationManager(int id)
    {
        var competence = await _context.Competences
            .Include(c => c.Collaborateur)
            .Include(c => c.EvaluationCompetence)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (competence == null) return NotFound();

        var evaluation = competence.EvaluationCompetence;
        if (evaluation == null)
        {
            TempData["Error"] = "Le collaborateur doit d'abord renseigner son auto-Ã©valuation.";
            return RedirectToAction(nameof(Index), new { collaborateurId = competence.CollaborateurId });
        }

        var vm = new ValidationManagerCompetenceViewModel
        {
            CompetenceId = competence.Id,
            CollaborateurId = competence.CollaborateurId,
            CompetenceNom = competence.Nom,
            CollaborateurNom = $"{competence.Collaborateur?.Prenom} {competence.Collaborateur?.Nom}".Trim(),
            Poste = competence.Collaborateur?.Poste,
            Categorie = competence.CategorieCompetence?.Nom,
            SeuilRh = evaluation.SeuilRh,
            AutoEvaluationCollaborateur = evaluation.AutoEvaluationCollaborateur,
            EvaluationManager = evaluation.EvaluationManager ?? evaluation.AutoEvaluationCollaborateur,
            ValidationManager = evaluation.ValidationManager,
            CommentaireCollaborateur = evaluation.CommentaireCollaborateur,
            CommentaireManager = evaluation.CommentaireManager
        };

        return View(vm);
    }

    // POST: Competences/ValidationManager
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidationManager(ValidationManagerCompetenceViewModel vm)
    {
        var competence = await _context.Competences
            .Include(c => c.EvaluationCompetence)
            .FirstOrDefaultAsync(c => c.Id == vm.CompetenceId);
        if (competence == null) return NotFound();
        if (competence.EvaluationCompetence == null)
        {
            TempData["Error"] = "Aucune auto-Ã©valuation Ã  valider.";
            return RedirectToAction(nameof(Index), new { collaborateurId = competence.CollaborateurId });
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var evaluation = competence.EvaluationCompetence;
        evaluation.EvaluationManager = vm.EvaluationManager;
        evaluation.ValidationManager = vm.ValidationManager;
        evaluation.CommentaireManager = vm.CommentaireManager;
        evaluation.DateValidationManager = DateTime.Now;

        competence.NiveauActuel = Math.Max(1, Math.Min(5, (int)Math.Ceiling(vm.EvaluationManager / 20.0)));
        competence.DateEvaluation = DateTime.Now;

        await _context.SaveChangesAsync();
        TempData["Success"] = vm.ValidationManager
            ? "Ã‰valuation manager validÃ©e et enregistrÃ©e."
            : "Correction manager enregistrÃ©e (validation non finalisÃ©e).";
        return RedirectToAction(nameof(Index), new { collaborateurId = competence.CollaborateurId });
    }

public async Task<IActionResult> Explorer(int collaborateurId)
{
    var competences = await _context.Competences
        .Where(c => c.CollaborateurId == collaborateurId)
        .ToListAsync();
    var collaborateur = await _context.Collaborateurs.FindAsync(collaborateurId);
    ViewBag.Collaborateur = collaborateur;
    return View(competences);
}[HttpPost]
public async Task<IActionResult> PlanifierExamen(int inscriptionId, DateTime dateExamen)
{
    var inscription = await _context.Inscriptions.FindAsync(inscriptionId);
    if (inscription == null) return NotFound();
    inscription.DateExamen = dateExamen;
    await _context.SaveChangesAsync();
    TempData["Success"] = "Examen planifiÃ©.";
    return RedirectToAction(nameof(Index), new { collaborateurId = inscription.CollaborateurId });
}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenererPlanDeveloppement(int collaborateurId, string? returnUrl = null)
    {
        var collaborateur = await _context.Collaborateurs.FindAsync(collaborateurId);
        if (collaborateur == null) return NotFound();

        var result = await _planDeveloppementService.GenererPourCollaborateurAsync(collaborateurId);
        TempData["Success"] = result.Message;
        return RedirectAfterPlanGeneration(collaborateurId, returnUrl);
    }

    private IActionResult RedirectAfterPlanGeneration(int collaborateurId, string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction(nameof(Index), new { collaborateurId });
    }

    // GET: Competences/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var competence = await _context.Competences
            .Include(c => c.Collaborateur)
            .Include(c => c.CategorieCompetence)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (competence == null) return NotFound();
        return View(competence);
    }

    // GET: Competences/Create
    public async Task<IActionResult> Create(int? collaborateurId)
    {
        if (collaborateurId == null)
        {
            return RedirectToAction("Index", "Collaborateurs");
        }
        ViewBag.CollaborateurId = collaborateurId;
        await PrepareCreateViewDataAsync(collaborateurId.Value);
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetCompetencesParGrade(string? grade)
    {
        var competences = await _referentielRhService.GetCompetencesDisponiblesParGradeAsync(grade);
        return Json(competences.Select(c => new
        {
            nom = c.Competence,
            poste = c.Poste,
            niveauRequis = c.NiveauRequis
        }));
    }

    // POST: Competences/Create
    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create([Bind("Id,Nom,CategorieCompetenceId,NiveauActuel,DateEvaluation,CollaborateurId")] Competence competence, List<string> selectedCompetences, string? selectedGrade)
{
    var collaborateur = await _context.Collaborateurs.FindAsync(competence.CollaborateurId);
    var grade = !string.IsNullOrWhiteSpace(selectedGrade) ? selectedGrade : collaborateur?.Grade;
    var niveauCible = CompetenceRules.GetNiveauCibleParGrade(grade ?? "Junior");
    var nomsCompetences = selectedCompetences
        .Where(c => !string.IsNullOrWhiteSpace(c))
        .Select(c => c.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    if (!nomsCompetences.Any() && !string.IsNullOrWhiteSpace(competence.Nom))
        nomsCompetences.Add(competence.Nom.Trim());

    if (!nomsCompetences.Any())
        ModelState.AddModelError(nameof(competence.Nom), "Selectionnez au moins une competence.");

    if (ModelState.IsValid)
    {
        foreach (var nom in nomsCompetences)
        {
            _context.Competences.Add(new Competence
            {
                Nom = nom,
                CategorieCompetenceId = competence.CategorieCompetenceId,
                NiveauActuel = competence.NiveauActuel,
                NiveauCible = niveauCible,
                DateEvaluation = competence.DateEvaluation,
                CollaborateurId = competence.CollaborateurId
            });
        }
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { collaborateurId = competence.CollaborateurId });
    }
    await PrepareCreateViewDataAsync(competence.CollaborateurId, grade);
    return View(competence);
}
    // GET: Competences/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var competence = await _context.Competences
            .Include(c => c.CategorieCompetence)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (competence == null) return NotFound();
        ViewBag.CollaborateurId = competence.CollaborateurId;
        ViewBag.Categories = new SelectList(await _context.CategoriesCompetences.ToListAsync(), "Id", "Nom", competence.CategorieCompetenceId);
        return View(competence);
    }

    // POST: Competences/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nom,CategorieCompetenceId,NiveauActuel,NiveauCible,DateEvaluation,CollaborateurId")] Competence competence)
    {
        if (id != competence.Id) return NotFound();
        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(competence);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Competences.Any(e => e.Id == id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index), new { collaborateurId = competence.CollaborateurId });
        }
        ViewBag.CollaborateurId = competence.CollaborateurId;
        ViewBag.Categories = new SelectList(await _context.CategoriesCompetences.ToListAsync(), "Id", "Nom", competence.CategorieCompetenceId);
        return View(competence);
    }

    // GET: Competences/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var competence = await _context.Competences
            .Include(c => c.Collaborateur)
            .Include(c => c.CategorieCompetence)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (competence == null) return NotFound();
        return View(competence);
    }

    // POST: Competences/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var competence = await _context.Competences.FindAsync(id);
        if (competence == null) return NotFound();
        int collaborateurId = competence.CollaborateurId;
        _context.Competences.Remove(competence);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { collaborateurId });
    }

    private async Task<int> GetSeuilRhAsync(Competence competence)
    {
        var collaborateur = await _context.Collaborateurs.FindAsync(competence.CollaborateurId);
        if (!string.IsNullOrWhiteSpace(collaborateur?.Poste))
        {
            var regle = await _context.CompetencesRequisesParPoste
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Poste == collaborateur.Poste && r.Competence == competence.Nom);
            if (regle != null)
                return Math.Clamp(regle.NiveauRequis * 20, 0, 100);
        }

        return Math.Clamp(competence.NiveauCible * 20, 0, 100);
    }

    private async Task PrepareCreateViewDataAsync(int collaborateurId, string? gradeOverride = null)
    {
        var collaborateur = await _context.Collaborateurs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == collaborateurId);
        var grade = gradeOverride ?? collaborateur?.Grade ?? "";

        ViewBag.CollaborateurGrade = grade;
        
        // Créer les catégories par défaut si elles n'existent pas
        if (!await _context.CategoriesCompetences.AnyAsync())
        {
            var defaultCategories = new[] { "Audit", "Data", "Leadership", "Management", "Métier", "Outils", "Risk", "Soft skills", "RH", "Fiscalité", "Méthodes" };
            foreach (var cat in defaultCategories)
            {
                _context.CategoriesCompetences.Add(new CategorieCompetence { Nom = cat });
            }
            await _context.SaveChangesAsync();
        }
        
        var categories = await _context.CategoriesCompetences.ToListAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Nom");
        ViewBag.CompetencesDisponibles = await _referentielRhService.GetCompetencesDisponiblesParGradeAsync(grade);
    }
}
