using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Authorization;
using SIRH.EY.Data;
using SIRH.EY.Models;
using SIRH.EY.Services;

namespace SIRH.EY.Controllers
{
    public class FormationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IParametreService _parametreService;
        private readonly ITeamAccessService _teamAccess;
        private readonly IOwnershipService _ownership;
        private readonly ICompetenceLifecycleService _competenceLifecycle;
        private readonly INotificationService _notifications;

        public FormationsController(
            ApplicationDbContext context,
            IParametreService parametreService,
            ITeamAccessService teamAccess,
            IOwnershipService ownership,
            ICompetenceLifecycleService competenceLifecycle,
            INotificationService notifications)
        {
            _context = context;
            _parametreService = parametreService;
            _teamAccess = teamAccess;
            _ownership = ownership;
            _competenceLifecycle = competenceLifecycle;
            _notifications = notifications;
        }

        // GET: Formations (version simplifiée : catalogue complet)
        public async Task<IActionResult> Index(int? collaborateurId)
        {
            if (collaborateurId == null)
            {
                // Default to the current user's own profile
                collaborateurId = await _teamAccess.GetCurrentCollaborateurIdAsync(User);
                if (collaborateurId == null)
                {
                    var premier = await _context.Collaborateurs.FirstOrDefaultAsync();
                    if (premier == null) return RedirectToAction("Create", "Collaborateurs");
                    collaborateurId = premier.Id;
                }
            }

            if (!await _teamAccess.CanAccessCollaborateurAsync(User, collaborateurId.Value))
                return Forbid();

            ViewBag.CollaborateurId = collaborateurId;

            var collaborateur = await _context.Collaborateurs
                .FirstOrDefaultAsync(c => c.Id == collaborateurId);
            ViewBag.Collaborateur = collaborateur;

            var plans = await _context.PlansDeveloppement
                .Where(p => p.CollaborateurId == collaborateurId)
                .ToListAsync();
            ViewBag.PlansDeveloppement = plans;

            var inscriptions = await _context.Inscriptions
                .Include(i => i.Formation)
                .Include(i => i.EvaluationPostFormation)
                .Include(i => i.EvaluationSuiviFormation)
                .Where(i => i.CollaborateurId == collaborateurId)
                .ToListAsync();
            ViewBag.Inscriptions = inscriptions;

            var toutesFormations = await _context.Formations.OrderBy(f => f.Titre).ToListAsync();
            var formationsInscritesIds = inscriptions.Select(i => i.FormationId).Distinct().ToList();
            ViewBag.Catalogue = toutesFormations.Where(f => !formationsInscritesIds.Contains(f.Id)).ToList();
            ViewBag.ToutesFormations = toutesFormations;
            ViewBag.FormationsInscritesIds = formationsInscritesIds;
            ViewBag.DepartementsFormation = toutesFormations
                .Select(f => f.DepartementCible ?? f.Categorie)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct()
                .OrderBy(v => v)
                .ToList();
            ViewBag.PostesFormation = toutesFormations
                .Select(f => f.PosteCible)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct()
                .OrderBy(v => v)
                .ToList();
            ViewBag.DomainesFormation = toutesFormations
                .Select(f => f.DomaineCompetence ?? f.CompetenceVisee)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct()
                .OrderBy(v => v)
                .ToList();

            // Utilisation correcte du paramétrage (exemple)
            int delai = _parametreService.GetValue<int>("DELAI_VALIDATION_FORMATION", 5);
            // Attention : formation n'est pas définie ici. C'était une erreur. 
            // Si vous voulez vérifier des formations en retard, il faut boucler ou utiliser une autre logique.
            // Exemple (commenté) :
            // var formationsEnRetard = inscriptions.Where(i => (DateTime.Now - i.DateInscription).Days > delai).ToList();

            var certifications = inscriptions.Where(i => i.Terminee).ToList();
            ViewBag.Certifications = certifications;

            return View();
        }
        // Télécharger le certificat PDF (formation terminée uniquement)
        public async Task<IActionResult> TelechargerCertificat(int inscriptionId)
        {
            if (!await _ownership.OwnsInscriptionAsync(User, inscriptionId))
                return Forbid();

            var inscription = await _context.Inscriptions
                .Include(i => i.Formation)
                .Include(i => i.Collaborateur)
                .FirstOrDefaultAsync(i => i.Id == inscriptionId);
            if (inscription == null)
            {
                TempData["Error"] = "Inscription introuvable.";
                return RedirectToAction(nameof(Index));
            }

            if (!inscription.Terminee)
            {
                TempData["Error"] = "Le certificat est disponible uniquement une fois la formation terminée.";
                return RedirectToAction(nameof(Index), new { collaborateurId = inscription.CollaborateurId });
            }

            var pdf = CertificatFormationPdf.Generer(inscription);
            var baseName = System.Text.RegularExpressions.Regex.Replace(
                inscription.Formation?.Titre ?? "formation",
                @"[^\w\-]+", "_",
                System.Text.RegularExpressions.RegexOptions.None,
                TimeSpan.FromSeconds(1)).Trim('_');
            if (string.IsNullOrEmpty(baseName)) baseName = "formation";
            return File(pdf, "application/pdf", $"Certificat_{baseName}.pdf");
        }

        // Reprendre / démarrer : espace module (prototype)
        public async Task<IActionResult> ReprendreFormation(int inscriptionId)
        {
            if (!await _ownership.OwnsInscriptionAsync(User, inscriptionId))
                return Forbid();

            var inscription = await _context.Inscriptions
                .Include(i => i.Formation)
                .Include(i => i.Collaborateur)
                .FirstOrDefaultAsync(i => i.Id == inscriptionId);
            if (inscription == null || inscription.Terminee)
                return NotFound();

            if (inscription.Statut == StatutInscription.PendingApproval || inscription.Statut == StatutInscription.Rejected)
            {
                TempData["Error"] = inscription.Statut == StatutInscription.PendingApproval
                    ? "Cette inscription est en attente d'approbation de votre manager."
                    : "Cette demande d'inscription a été refusée par votre manager.";
                return RedirectToAction(nameof(Index), new { collaborateurId = inscription.CollaborateurId });
            }

            return View("ModuleFormation", inscription);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AvancerModule(int inscriptionId, int deltaPourcent = 20)
        {
            if (!await _ownership.OwnsInscriptionAsync(User, inscriptionId))
                return Forbid();

            var inscription = await _context.Inscriptions
                .Include(i => i.Formation)
                .FirstOrDefaultAsync(i => i.Id == inscriptionId);
            if (inscription == null || inscription.Terminee)
                return NotFound();

            if (inscription.Statut != StatutInscription.Approved && inscription.Statut != StatutInscription.InProgress)
                return Forbid();

            if (inscription.Statut == StatutInscription.Approved)
                inscription.Statut = StatutInscription.InProgress;

            inscription.Progression = Math.Min(100, Math.Max(0, inscription.Progression + deltaPourcent));
            await _context.SaveChangesAsync();
            TempData["Success"] = inscription.Progression >= 100
                ? "Parcours module terminé à 100 % — vous pouvez valider la formation depuis le centre."
                : $"Progression enregistrée : {inscription.Progression} %.";
            return RedirectToAction(nameof(ReprendreFormation), new { inscriptionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlanifierExamen(int inscriptionId, DateTime dateExamen, string? lieu = null, string? commentaire = null)
        {
            if (!await _ownership.OwnsInscriptionAsync(User, inscriptionId))
                return Forbid();

            var inscription = await _context.Inscriptions
                .Include(i => i.Formation)
                .FirstOrDefaultAsync(i => i.Id == inscriptionId);
            if (inscription == null) return NotFound();
            if (dateExamen.Date < DateTime.Today)
            {
                TempData["Error"] = "La date d'examen doit être aujourd'hui ou dans le futur.";
                return RedirectToAction(nameof(PlanifierExamen), new { inscriptionId });
            }

            inscription.DateExamen = dateExamen;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Examen planifié le {dateExamen:dd/MM/yyyy}" +
                (string.IsNullOrWhiteSpace(lieu) ? "." : $" — lieu : {lieu}.");
            return RedirectToAction(nameof(Index), new { collaborateurId = inscription.CollaborateurId });
        }

        [HttpGet]
        public async Task<IActionResult> PlanifierExamen(int inscriptionId)
        {
            var inscription = await _context.Inscriptions
                .Include(i => i.Formation)
                .Include(i => i.Collaborateur)
                .FirstOrDefaultAsync(i => i.Id == inscriptionId);
            if (inscription == null)
            {
                TempData["Error"] = "Sélectionnez d'abord une formation en cours pour planifier un examen.";
                return RedirectToAction(nameof(Index));
            }

            return View(inscription);
        }
        // POST: Inscrire
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Inscrire(int formationId, int collaborateurId)
        {
            if (!await _teamAccess.CanAccessCollaborateurAsync(User, collaborateurId))
                return Forbid();
            var formation = await _context.Formations.FindAsync(formationId);
            if (formation != null && formation.PlacesPrises < formation.CapaciteMax)
            {
                var inscription = new Inscription
                {
                    FormationId = formationId,
                    CollaborateurId = collaborateurId,
                    DateInscription = DateTime.Now,
                    Terminee = false,
                    Statut = StatutInscription.PendingApproval
                };
                _context.Inscriptions.Add(inscription);
                formation.PlacesPrises++;

                var collaborateur = await _context.Collaborateurs.FindAsync(collaborateurId);
                if (collaborateur?.ManagerId != null)
                {
                    await _notifications.NotifyAsync(
                        collaborateur.ManagerId.Value,
                        $"{collaborateur.Prenom} {collaborateur.Nom} demande votre approbation pour la formation « {formation.Titre} ».",
                        Url.Action(nameof(ApprobationsFormation)));
                }

                await _context.SaveChangesAsync();
                TempData["Message"] = "Demande d'inscription envoyée, en attente d'approbation du manager.";
            }
            else
            {
                TempData["Erreur"] = "Plus de places disponibles.";
            }
            return RedirectToAction(nameof(Index), new { collaborateurId });
        }

        // POST: Annuler inscription (optionnel, mais conservé)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnnulerInscription(int inscriptionId)
        {
            if (!await _ownership.OwnsInscriptionAsync(User, inscriptionId))
                return Forbid();

            var inscription = await _context.Inscriptions
                .Include(i => i.Formation)
                .FirstOrDefaultAsync(i => i.Id == inscriptionId);
            if (inscription == null) return NotFound();

            var formation = inscription.Formation;
            if (formation != null)
            {
                formation.PlacesPrises = Math.Max(0, formation.PlacesPrises - 1);
                _context.Update(formation);
            }
            _context.Inscriptions.Remove(inscription);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Inscription annulée.";
            return RedirectToAction(nameof(Index), new { collaborateurId = inscription.CollaborateurId });
        }

        // GET: Formations/ApprobationsFormation — manager inbox of pending enrollment requests
        // for their direct reports (same team scope as ManagerActionCenterService).
        [Authorize(Roles = Roles.ITAdminOrRHOrManager)]
        public async Task<IActionResult> ApprobationsFormation()
        {
            var teamQuery = await _teamAccess.ApplyAccessFilterAsync(User, _context.Collaborateurs);
            var teamIds = await teamQuery.Select(c => c.Id).ToListAsync();
            var ownId = await _teamAccess.GetCurrentCollaborateurIdAsync(User);

            var pending = await _context.Inscriptions
                .Include(i => i.Formation)
                .Include(i => i.Collaborateur)
                .Where(i => teamIds.Contains(i.CollaborateurId)
                         && i.CollaborateurId != ownId
                         && i.Statut == StatutInscription.PendingApproval)
                .OrderBy(i => i.DateInscription)
                .ToListAsync();

            return View(pending);
        }

        // POST: Formations/ApprouverInscription
        [HttpPost]
        [Authorize(Roles = Roles.ITAdminOrRHOrManager)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprouverInscription(int inscriptionId)
        {
            var inscription = await _context.Inscriptions
                .Include(i => i.Formation)
                .Include(i => i.Collaborateur)
                .FirstOrDefaultAsync(i => i.Id == inscriptionId);
            if (inscription == null) return NotFound();

            if (!await CanDecideOnInscriptionAsync(inscription))
                return Forbid();

            if (inscription.Statut != StatutInscription.PendingApproval)
            {
                TempData["Error"] = "Cette demande a déjà été traitée.";
                return RedirectToAction(nameof(ApprobationsFormation));
            }

            inscription.Statut = StatutInscription.Approved;
            await _notifications.NotifyAsync(
                inscription.CollaborateurId,
                $"Votre inscription à « {inscription.Formation?.Titre} » a été approuvée par votre manager.",
                Url.Action(nameof(Details), new { id = inscription.FormationId, collaborateurId = inscription.CollaborateurId }));

            await _context.SaveChangesAsync();
            TempData["Success"] = "Inscription approuvée.";
            return RedirectToAction(nameof(ApprobationsFormation));
        }

        // POST: Formations/RejeterInscription
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.ITAdminOrRHOrManager)]
        public async Task<IActionResult> RejeterInscription(int inscriptionId, string? motif = null)
        {
            var inscription = await _context.Inscriptions
                .Include(i => i.Formation)
                .FirstOrDefaultAsync(i => i.Id == inscriptionId);
            if (inscription == null) return NotFound();

            if (!await CanDecideOnInscriptionAsync(inscription))
                return Forbid();

            if (inscription.Statut != StatutInscription.PendingApproval)
            {
                TempData["Error"] = "Cette demande a déjà été traitée.";
                return RedirectToAction(nameof(ApprobationsFormation));
            }

            inscription.Statut = StatutInscription.Rejected;
            if (inscription.Formation != null)
                inscription.Formation.PlacesPrises = Math.Max(0, inscription.Formation.PlacesPrises - 1);

            var message = string.IsNullOrWhiteSpace(motif)
                ? $"Votre demande d'inscription à « {inscription.Formation?.Titre} » a été refusée par votre manager."
                : $"Votre demande d'inscription à « {inscription.Formation?.Titre} » a été refusée : {motif}";
            await _notifications.NotifyAsync(inscription.CollaborateurId, message);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Inscription refusée.";
            return RedirectToAction(nameof(ApprobationsFormation));
        }

        // 4-eyes guard, same rule as CompetencesController.ValidationManager: a manager may
        // decide for their direct reports but never for themselves; RH/ITAdmin are unrestricted.
        private async Task<bool> CanDecideOnInscriptionAsync(Inscription inscription)
        {
            if (!await _teamAccess.CanAccessCollaborateurAsync(User, inscription.CollaborateurId))
                return false;

            if (User.IsInRole(Roles.Manager) && !User.IsInRole(Roles.RH) && !User.IsInRole(Roles.ITAdmin))
            {
                var ownId = await _teamAccess.GetCurrentCollaborateurIdAsync(User);
                if (ownId.HasValue && inscription.CollaborateurId == ownId.Value)
                    return false;
            }

            return true;
        }

        // GET: Formations/Create
        [Authorize(Roles = Roles.ITAdminOrRH)]
        public IActionResult Create()
        {
            PrepareFormationViewData();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = Roles.ITAdminOrRH)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Titre,Formateur,DureeHeures,CapaciteMax,PlacesPrises,Categorie,DateDebut,Organisme,CompetenceVisee,DepartementCible,MetierCible,PosteCible,DomaineCompetence,NiveauDifficulte,EstCertifiante,Plateforme,ExternalUrl,Description,CompetencesRequises,CertificationNom,SupportPdfUrl,MentorEmail,EstStrategique,EstForteDemande")] Formation formation)
        {
            if (ModelState.IsValid)
            {
                _context.Add(formation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            PrepareFormationViewData();
            return View(formation);
        }

        // GET: Formations/Edit/5
        [Authorize(Roles = Roles.ITAdminOrRH)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var formation = await _context.Formations.FindAsync(id);
            if (formation == null) return NotFound();
            PrepareFormationViewData();
            return View(formation);
        }

        [HttpPost]
        [Authorize(Roles = Roles.ITAdminOrRH)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Titre,Formateur,DureeHeures,CapaciteMax,PlacesPrises,Categorie,DateDebut,Organisme,CompetenceVisee,DepartementCible,MetierCible,PosteCible,DomaineCompetence,NiveauDifficulte,EstCertifiante,Plateforme,ExternalUrl,Description,CompetencesRequises,CertificationNom,SupportPdfUrl,MentorEmail,EstStrategique,EstForteDemande")] Formation formation)
        {
            if (id != formation.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(formation);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FormationExists(formation.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            PrepareFormationViewData();
            return View(formation);
        }
        [HttpPost]
        [Authorize(Roles = Roles.ITAdminOrRH)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TerminerFormation(int inscriptionId)
{
    var inscription = await _context.Inscriptions
        .Include(i => i.Formation)
        .FirstOrDefaultAsync(i => i.Id == inscriptionId);
    if (inscription == null) return NotFound();

    if (inscription.Terminee)
    {
        TempData["Error"] = "Cette formation est déjà terminée.";
        return RedirectToAction(nameof(Index), new { collaborateurId = inscription.CollaborateurId });
    }

    inscription.Terminee        = true;
    inscription.Statut          = StatutInscription.Completed;
    inscription.DateCompletion  = DateTime.Now;
    inscription.Progression     = 100;
    inscription.SourceCertification ??= inscription.Formation?.Plateforme ?? "EY Learning";
    await _context.SaveChangesAsync();

    var formation = inscription.Formation;
    var competenceVisee = formation?.CompetenceVisee;

    // Chemin prioritaire : vraie relation FK (Skill). Filet de sécurité inchangé : recherche
    // par nom existante, pour les lignes non backfillées par SkillBridgeSeeder.
    Competence? competence = formation?.SkillId != null
        ? await _context.Competences
            .FirstOrDefaultAsync(c => c.CollaborateurId == inscription.CollaborateurId && c.SkillId == formation.SkillId)
        : null;
    competence ??= !string.IsNullOrEmpty(competenceVisee)
        ? await _context.Competences
            .FirstOrDefaultAsync(c => c.CollaborateurId == inscription.CollaborateurId && c.Nom == competenceVisee)
        : null;

    var collaborateurGrade = !string.IsNullOrEmpty(competenceVisee) && competence == null
        ? (await _context.Collaborateurs.FindAsync(inscription.CollaborateurId))?.Grade ?? "Junior"
        : "Junior";

    var decision = FormationCompletionEngine.ResolveCompetenceUpdate(competence, competenceVisee, collaborateurGrade);

    switch (decision.Outcome)
    {
        case CompetenceCompletionOutcome.Created:
            competence = new Competence
            {
                Nom = decision.CompetenceNom!,
                CategorieCompetenceId = null,
                NiveauActuel = 1,
                NiveauCible = decision.NiveauCible!.Value,
                DateEvaluation = DateTime.Now,
                CollaborateurId = inscription.CollaborateurId,
                SkillId = formation?.SkillId
            };
            _context.Competences.Add(competence);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Formation terminée ! La compétence '{competence.Nom}' a été créée avec un niveau 1/{competence.NiveauCible}.";
            break;
        case CompetenceCompletionOutcome.Incremented:
            var ancienNiveauFormation = competence!.NiveauActuel;
            competence.NiveauActuel = decision.NouveauNiveau!.Value;
            await _competenceLifecycle.RecordLevelChangeAsync(competence, ancienNiveauFormation, competence.NiveauActuel, CompetenceChangeReason.Formation);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Formation terminée ! Niveau de compétence '{competence.Nom}' augmenté à {competence.NiveauActuel}/{competence.NiveauCible}.";
            break;
        case CompetenceCompletionOutcome.AlreadyAtTarget:
            TempData["Success"] = "Formation terminée. La compétence visée a déjà atteint son objectif.";
            break;
        default:
            TempData["Success"] = "Formation terminée (aucune compétence associée).";
            break;
    }

    return RedirectToAction(nameof(EvaluerFormation), new { inscriptionId = inscription.Id,
        returnUrl = Url.Action(nameof(Index), new { collaborateurId = inscription.CollaborateurId }) });
}

        // GET: Formations/EvaluerFormation/5 — évaluation à chaud, déclenchée après complétion
        public async Task<IActionResult> EvaluerFormation(int inscriptionId, string? returnUrl = null)
        {
            var inscription = await _context.Inscriptions
                .Include(i => i.Formation)
                .Include(i => i.EvaluationPostFormation)
                .FirstOrDefaultAsync(i => i.Id == inscriptionId);
            if (inscription == null) return NotFound();

            if (!await _ownership.OwnsInscriptionAsync(User, inscriptionId))
                return Forbid();

            ViewBag.Inscription = inscription;
            ViewBag.ReturnUrl = returnUrl;
            return View(inscription.EvaluationPostFormation ?? new EvaluationPostFormation { InscriptionId = inscriptionId });
        }

        // POST: Formations/EvaluerFormation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EvaluerFormation(EvaluationPostFormation vm, string? returnUrl = null)
        {
            if (!await _ownership.OwnsInscriptionAsync(User, vm.InscriptionId))
                return Forbid();

            var inscription = await _context.Inscriptions.FindAsync(vm.InscriptionId);
            if (inscription == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Inscription = inscription;
                ViewBag.ReturnUrl = returnUrl;
                return View(vm);
            }

            var existing = await _context.EvaluationsPostFormation.FirstOrDefaultAsync(e => e.InscriptionId == vm.InscriptionId);
            if (existing == null)
            {
                vm.DateEvaluation = DateTime.Now;
                _context.EvaluationsPostFormation.Add(vm);
            }
            else
            {
                existing.NoteGlobale = vm.NoteGlobale;
                existing.NoteContenu = vm.NoteContenu;
                existing.NoteFormateur = vm.NoteFormateur;
                existing.Recommande = vm.Recommande;
                existing.Commentaire = vm.Commentaire;
                existing.DateEvaluation = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Merci pour votre évaluation à chaud !";

            return (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                ? LocalRedirect(returnUrl)
                : RedirectToAction(nameof(Index), new { collaborateurId = inscription.CollaborateurId });
        }

        // GET: Formations/EvaluationSuivi/5 — évaluation de suivi (à froid), disponible une fois
        // l'évaluation à chaud complétée. Pas de déclenchement automatique : accessible via un lien
        // optionnel depuis l'onglet Certifications de Formations/Index.
        public async Task<IActionResult> EvaluationSuivi(int inscriptionId, string? returnUrl = null)
        {
            var inscription = await _context.Inscriptions
                .Include(i => i.Formation)
                .Include(i => i.EvaluationPostFormation)
                .Include(i => i.EvaluationSuiviFormation)
                .FirstOrDefaultAsync(i => i.Id == inscriptionId);
            if (inscription == null) return NotFound();

            if (!await _ownership.OwnsInscriptionAsync(User, inscriptionId))
                return Forbid();

            if (inscription.EvaluationPostFormation == null)
            {
                TempData["Error"] = "L'évaluation à chaud doit être complétée avant l'évaluation de suivi.";
                return RedirectToAction(nameof(Index), new { collaborateurId = inscription.CollaborateurId });
            }

            ViewBag.Inscription = inscription;
            ViewBag.ReturnUrl = returnUrl;
            return View(inscription.EvaluationSuiviFormation ?? new EvaluationSuiviFormation { InscriptionId = inscriptionId });
        }

        // POST: Formations/EvaluationSuivi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EvaluationSuivi(EvaluationSuiviFormation vm, string? returnUrl = null)
        {
            if (!await _ownership.OwnsInscriptionAsync(User, vm.InscriptionId))
                return Forbid();

            var inscription = await _context.Inscriptions
                .Include(i => i.EvaluationPostFormation)
                .FirstOrDefaultAsync(i => i.Id == vm.InscriptionId);
            if (inscription == null) return NotFound();
            if (inscription.EvaluationPostFormation == null) return Forbid();

            if (!ModelState.IsValid)
            {
                ViewBag.Inscription = inscription;
                ViewBag.ReturnUrl = returnUrl;
                return View(vm);
            }

            var existing = await _context.EvaluationsSuiviFormation.FirstOrDefaultAsync(e => e.InscriptionId == vm.InscriptionId);
            if (existing == null)
            {
                vm.DateEvaluation = DateTime.Now;
                _context.EvaluationsSuiviFormation.Add(vm);
            }
            else
            {
                existing.NoteApplicationCompetences = vm.NoteApplicationCompetences;
                existing.NoteImpactBusiness = vm.NoteImpactBusiness;
                existing.ExemplesConcrets = vm.ExemplesConcrets;
                existing.Commentaire = vm.Commentaire;
                existing.DateEvaluation = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Merci pour votre évaluation de suivi !";

            return (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                ? LocalRedirect(returnUrl)
                : RedirectToAction(nameof(Index), new { collaborateurId = inscription.CollaborateurId });
        }

        // POST: Formations/ValiderSuivi — manager validates the J+90 follow-up assessment and,
        // on validation, upgrades the collaborateur's skill level. Mirrors
        // CompetencesController.ValidationManager's 4-eyes rule and reuses
        // ICompetenceLifecycleService so both entry points share one audit trail.
        [HttpPost]
        [Authorize(Roles = Roles.ITAdminOrRHOrManager)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValiderSuivi(int inscriptionId, int niveauValide)
        {
            if (niveauValide < 1 || niveauValide > 5)
            {
                TempData["Error"] = "Le niveau validé doit être compris entre 1 et 5.";
                return RedirectToAction(nameof(Index));
            }

            var inscription = await _context.Inscriptions
                .Include(i => i.Formation)
                .Include(i => i.EvaluationSuiviFormation)
                .FirstOrDefaultAsync(i => i.Id == inscriptionId);
            if (inscription == null || inscription.EvaluationSuiviFormation == null) return NotFound();

            if (!await CanDecideOnInscriptionAsync(inscription))
                return Forbid();

            var formation = inscription.Formation;
            Competence? competence = formation?.SkillId != null
                ? await _context.Competences.FirstOrDefaultAsync(c =>
                    c.CollaborateurId == inscription.CollaborateurId && c.SkillId == formation.SkillId)
                : null;
            competence ??= !string.IsNullOrEmpty(formation?.CompetenceVisee)
                ? await _context.Competences.FirstOrDefaultAsync(c =>
                    c.CollaborateurId == inscription.CollaborateurId && c.Nom == formation.CompetenceVisee)
                : null;

            if (competence == null)
            {
                TempData["Error"] = "Aucune compétence associée à cette formation.";
                return RedirectToAction(nameof(Index), new { collaborateurId = inscription.CollaborateurId });
            }

            var suivi = inscription.EvaluationSuiviFormation;
            suivi.NiveauValide = niveauValide;
            suivi.ValidationManager = true;
            suivi.DateValidationManager = DateTime.Now;

            var ancienNiveau = competence.NiveauActuel;
            competence.NiveauActuel = niveauValide;
            competence.DateEvaluation = DateTime.Now;
            await _competenceLifecycle.RecordLevelChangeAsync(
                competence, ancienNiveau, competence.NiveauActuel, CompetenceChangeReason.ValidationManager);

            await _notifications.NotifyAsync(
                inscription.CollaborateurId,
                $"Votre manager a validé le suivi à 90 jours de « {formation?.Titre} » : compétence '{competence.Nom}' à {niveauValide}/5.");

            await _context.SaveChangesAsync();
            TempData["Success"] = "Évaluation de suivi validée, niveau de compétence mis à jour.";
            return RedirectToAction(nameof(Index), new { collaborateurId = inscription.CollaborateurId });
        }

        // GET: Formations/Details/5
        public async Task<IActionResult> Details(int id, int? collaborateurId)
        {
            var formation = await _context.Formations.FindAsync(id);
            if (formation == null) return NotFound();

            if (collaborateurId == null)
                collaborateurId = await _teamAccess.GetCurrentCollaborateurIdAsync(User);

            Collaborateur? collab = null;
            Inscription? inscriptionActuelle = null;
            int score = 50;
            bool dansPlan = false;

            if (collaborateurId.HasValue)
            {
                collab = await _context.Collaborateurs.FindAsync(collaborateurId.Value);

                inscriptionActuelle = await _context.Inscriptions
                    .FirstOrDefaultAsync(i => i.CollaborateurId == collaborateurId.Value && i.FormationId == id);

                dansPlan = await _context.PlansDeveloppement
                    .AnyAsync(p => p.CollaborateurId == collaborateurId.Value && p.FormationId == id);

                // Reuse same compatibility logic as Index view
                score = 45;
                if (!string.IsNullOrEmpty(formation.PosteCible) &&
                    formation.PosteCible.Equals(collab?.Poste, StringComparison.OrdinalIgnoreCase)) score += 35;
                else if (!string.IsNullOrEmpty(formation.MetierCible) &&
                    formation.MetierCible.Equals(collab?.Poste, StringComparison.OrdinalIgnoreCase)) score += 25;
                if (!string.IsNullOrEmpty(formation.DepartementCible) &&
                    formation.DepartementCible.Equals(collab?.Departement, StringComparison.OrdinalIgnoreCase)) score += 15;
                if (dansPlan) score += 10;
                if (score > 98) score = 98;
            }

            var competencesDeveloppees = new List<string>();
            if (!string.IsNullOrWhiteSpace(formation.CompetenceVisee))
                competencesDeveloppees.Add(formation.CompetenceVisee);
            if (!string.IsNullOrWhiteSpace(formation.DomaineCompetence) &&
                formation.DomaineCompetence != formation.CompetenceVisee)
                competencesDeveloppees.Add(formation.DomaineCompetence);

            var prerequisListe = string.IsNullOrWhiteSpace(formation.CompetencesRequises)
                ? new List<string>()
                : formation.CompetencesRequises.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

            var vm = new FormationDetailViewModel
            {
                Formation             = formation,
                InscriptionActuelle   = inscriptionActuelle,
                CollaborateurId       = collaborateurId ?? 0,
                Collaborateur         = collab,
                ScoreAdequation       = score,
                ProchainGrade         = GetProchainGrade(collab?.Grade),
                EstObligatoire        = IsFormationObligatoire(formation, collab),
                EstDansPlan           = dansPlan,
                CompetencesDeveloppees = competencesDeveloppees,
                CompetencesPrerequisListe = prerequisListe
            };

            return View(vm);
        }

        // GET: Formations/Recommandations/5
        public async Task<IActionResult> Recommandations(int collaborateurId)
        {
            if (!await _teamAccess.CanAccessCollaborateurAsync(User, collaborateurId))
                return Forbid();

            var collab = await _context.Collaborateurs.FindAsync(collaborateurId);
            if (collab == null) return NotFound();

            var toutesFormations = await _context.Formations.OrderBy(f => f.Titre).ToListAsync();

            var inscriptionsIds = await _context.Inscriptions
                .Where(i => i.CollaborateurId == collaborateurId)
                .Select(i => i.FormationId)
                .ToListAsync();

            var plansIds = await _context.PlansDeveloppement
                .Where(p => p.CollaborateurId == collaborateurId)
                .Select(p => p.FormationId)
                .ToListAsync();

            var competences = await _context.Competences
                .Where(c => c.CollaborateurId == collaborateurId)
                .ToListAsync();

            var competencesManquantes = competences
                .Where(c => c.NiveauActuel < c.NiveauCible)
                .ToList();

            var prochainGrade = GetProchainGrade(collab.Grade);
            var vm = new RecommandationsPageViewModel
            {
                CollaborateurId = collaborateurId,
                Collaborateur   = collab,
                ProchainGrade   = prochainGrade
            };

            foreach (var f in toutesFormations)
            {
                bool inscrit  = inscriptionsIds.Contains(f.Id);
                bool dansPlan = plansIds.Contains(f.Id);

                // Par plan de développement
                if (dansPlan && !inscrit)
                {
                    vm.ParPlan.Add(new RecommandationFormationViewModel
                    {
                        Formation        = f,
                        Raison           = "Dans votre plan de développement",
                        Type             = "plan",
                        ScoreAdequation  = 90,
                        EstInscrit       = inscrit,
                        EstDansPlan      = dansPlan
                    });
                    continue;
                }

                // Par compétence manquante — bridge SkillId prioritaire, repli sur le texte
                // CompetenceVisee (même convention que SkillGapEngine/FormationsController.Terminer).
                var matchComp = competencesManquantes
                    .FirstOrDefault(c => SkillGapEngine.FormationCombleGap(f, c));
                if (matchComp != null && !inscrit)
                {
                    vm.ParCompetence.Add(new RecommandationFormationViewModel
                    {
                        Formation        = f,
                        Raison           = $"Améliore la compétence : {matchComp.Nom} (niveau {matchComp.NiveauActuel}/{matchComp.NiveauCible})",
                        Type             = "competence",
                        ScoreAdequation  = 85,
                        CompetenceCiblee = matchComp.Nom,
                        EstInscrit       = inscrit,
                        EstDansPlan      = dansPlan
                    });
                    continue;
                }

                // Par grade cible
                if (!string.IsNullOrEmpty(prochainGrade) && !inscrit &&
                    (!string.IsNullOrEmpty(f.PosteCible) || !string.IsNullOrEmpty(f.DepartementCible)))
                {
                    bool matchGrade = (!string.IsNullOrEmpty(f.PosteCible) &&
                                       f.PosteCible.Contains(prochainGrade, StringComparison.OrdinalIgnoreCase)) ||
                                      (!string.IsNullOrEmpty(f.MetierCible) &&
                                       f.MetierCible.Contains(prochainGrade, StringComparison.OrdinalIgnoreCase));
                    if (matchGrade)
                    {
                        vm.ParGrade.Add(new RecommandationFormationViewModel
                        {
                            Formation       = f,
                            Raison          = $"Cette formation vous rapproche du grade {prochainGrade}",
                            Type            = "grade",
                            ScoreAdequation = 75,
                            EstInscrit      = inscrit,
                            EstDansPlan     = dansPlan
                        });
                    }
                }
            }

            // Cap lists to avoid overwhelming the page
            vm.ParCompetence = vm.ParCompetence.Take(6).ToList();
            vm.ParGrade      = vm.ParGrade.Take(4).ToList();
            vm.ParPlan       = vm.ParPlan.Take(4).ToList();

            return View(vm);
        }

        // GET: Formations/ParcoursCarriere/5
        public async Task<IActionResult> ParcoursCarriere(int collaborateurId)
        {
            if (!await _teamAccess.CanAccessCollaborateurAsync(User, collaborateurId))
                return Forbid();

            var collab = await _context.Collaborateurs.FindAsync(collaborateurId);
            if (collab == null) return NotFound();

            var competences = await _context.Competences
                .Where(c => c.CollaborateurId == collaborateurId)
                .OrderBy(c => c.Nom)
                .ToListAsync();

            var inscriptionsIds = await _context.Inscriptions
                .Where(i => i.CollaborateurId == collaborateurId)
                .Select(i => i.FormationId)
                .ToListAsync();

            var toutesFormations = await _context.Formations.ToListAsync();

            var acquises  = competences.Where(c => c.NiveauActuel >= c.NiveauCible).ToList();
            var manquantes = competences.Where(c => c.NiveauActuel < c.NiveauCible).ToList();

            var itemsAcquises = acquises.Select(c => new CompetenceStatutItem
            {
                Nom          = c.Nom,
                NiveauActuel = c.NiveauActuel,
                NiveauCible  = c.NiveauCible
            }).ToList();

            var itemsManquantes = manquantes.Select(c =>
            {
                var formation = toutesFormations
                    .FirstOrDefault(f => !string.IsNullOrEmpty(f.CompetenceVisee) &&
                                         f.CompetenceVisee.Equals(c.Nom, StringComparison.OrdinalIgnoreCase));
                return new CompetenceStatutItem
                {
                    Nom                  = c.Nom,
                    NiveauActuel         = c.NiveauActuel,
                    NiveauCible          = c.NiveauCible,
                    FormationRecommandee = formation
                };
            }).ToList();

            // Formations couvrant les gaps, not yet enrolled
            var formationsRecommandees = toutesFormations
                .Where(f => manquantes.Any(c =>
                    !string.IsNullOrEmpty(f.CompetenceVisee) &&
                    f.CompetenceVisee.Equals(c.Nom, StringComparison.OrdinalIgnoreCase)) &&
                    !inscriptionsIds.Contains(f.Id))
                .Take(6)
                .ToList();

            // Grade progression: % of competences at target level
            int progressionGrade = competences.Count == 0 ? 0
                : (int)((double)acquises.Count / competences.Count * 100);

            var vm = new ParcoursCarriereViewModel
            {
                CollaborateurId       = collaborateurId,
                Collaborateur         = collab,
                GradeActuel           = collab.Grade ?? "Junior",
                ProchainGrade         = GetProchainGrade(collab.Grade),
                ProgressionGrade      = progressionGrade,
                CompetencesAcquises   = itemsAcquises,
                CompetencesManquantes  = itemsManquantes,
                FormationsRecommandees = formationsRecommandees,
                FormationsInscritesIds = inscriptionsIds
            };

            return View(vm);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        // Falls back to "Junior" before matching, mirroring the display-side
        // fallback (GradeActuel = collab.Grade ?? "Junior") — otherwise a
        // collaborator with no Grade set shows "Junior" in the UI while this
        // method silently returns null ("grade maximum atteint"), the two
        // falling out of sync. Ladder matches GradeReferentiel exactly
        // (Junior→Senior→Manager→Senior Manager→Director→Partner).
        private static string? GetProchainGrade(string? grade) => (grade ?? "Junior").Trim().ToLowerInvariant() switch
        {
            "junior"         => "Senior",
            "senior"         => "Manager",
            "manager"        => "Senior Manager",
            "senior manager" => "Director",
            "director"       => "Partner",
            _                => null
        };

        private static bool IsFormationObligatoire(Formation f, Collaborateur? c)
        {
            if (f.Titre.Contains("RGPD", StringComparison.OrdinalIgnoreCase) ||
                f.Titre.Contains("Conformité", StringComparison.OrdinalIgnoreCase))
                return true;
            if (c != null && !string.IsNullOrEmpty(c.FormationsObligatoires) &&
                c.FormationsObligatoires.Contains(f.Titre, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        // GET: Formations/Delete/5
        [Authorize(Roles = Roles.ITAdminOrRH)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var formation = await _context.Formations.FirstOrDefaultAsync(m => m.Id == id);
            if (formation == null) return NotFound();
            return View(formation);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = Roles.ITAdminOrRH)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var formation = await _context.Formations.FindAsync(id);
            if (formation != null) _context.Formations.Remove(formation);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FormationExists(int id)
        {
            return _context.Formations.Any(e => e.Id == id);
        }

        private void PrepareFormationViewData()
        {
            ViewBag.Departements = CompetenceCatalogService.Departements;
            ViewBag.Postes = CompetenceCatalogService.Postes;
            ViewBag.NiveauxDifficulte = new[] { "Fondamental", "Intermediaire", "Avance", "Expert" };
            ViewBag.DomainesCompetence = new[] { "Audit", "Risk", "Leadership", "Management", "Data", "Platforms", "RH", "Consulting", "Compliance" };
        }
    }
}
