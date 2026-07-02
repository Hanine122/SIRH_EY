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
    // Raw inscription management is an administrative operation — RH and ITAdmin only.
    // Collaborateurs manage their own inscriptions through FormationsController.
    [Authorize(Roles = Roles.ITAdminOrRH)]
    public class InscriptionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InscriptionsController(ApplicationDbContext context)
        {
            _context = context;
        }
        public DateTime? DateExamen { get; set; }

        // GET: Inscriptions
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Inscriptions.Include(i => i.Collaborateur).Include(i => i.Formation);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Inscriptions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var inscription = await _context.Inscriptions
                .Include(i => i.Collaborateur)
                .Include(i => i.Formation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (inscription == null) return NotFound();
            return View(inscription);
        }

        // GET: Inscriptions/Create
        public IActionResult Create()
        {
            ViewData["CollaborateurId"] = new SelectList(_context.Collaborateurs, "Id", "Nom");
            ViewData["FormationId"] = new SelectList(_context.Formations, "Id", "Titre");
            return View();
        }

        // POST: Inscriptions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DateInscription,Terminee,CollaborateurId,FormationId")] Inscription inscription)
        {
            if (ModelState.IsValid)
            {
                _context.Add(inscription);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CollaborateurId"] = new SelectList(_context.Collaborateurs, "Id", "Nom", inscription.CollaborateurId);
            ViewData["FormationId"] = new SelectList(_context.Formations, "Id", "Titre", inscription.FormationId);
            return View(inscription);
        }

        // GET: Inscriptions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var inscription = await _context.Inscriptions.FindAsync(id);
            if (inscription == null) return NotFound();
            ViewData["CollaborateurId"] = new SelectList(_context.Collaborateurs, "Id", "Nom", inscription.CollaborateurId);
            ViewData["FormationId"] = new SelectList(_context.Formations, "Id", "Titre", inscription.FormationId);
            return View(inscription);
        }

        // POST: Inscriptions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DateInscription,Terminee,CollaborateurId,FormationId")] Inscription inscription)
        {
            if (id != inscription.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(inscription);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InscriptionExists(inscription.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CollaborateurId"] = new SelectList(_context.Collaborateurs, "Id", "Nom", inscription.CollaborateurId);
            ViewData["FormationId"] = new SelectList(_context.Formations, "Id", "Titre", inscription.FormationId);
            return View(inscription);
        }

        // GET: Inscriptions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var inscription = await _context.Inscriptions
                .Include(i => i.Collaborateur)
                .Include(i => i.Formation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (inscription == null) return NotFound();
            return View(inscription);
        }

        // POST: Inscriptions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inscription = await _context.Inscriptions.FindAsync(id);
            if (inscription != null) _context.Inscriptions.Remove(inscription);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Inscriptions/Terminer/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Terminer(int id)
        {
            var inscription = await _context.Inscriptions
                .Include(i => i.Formation)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (inscription == null) return NotFound();

            inscription.Terminee = true;
            await _context.SaveChangesAsync();

            var formation = inscription.Formation;
            var competenceVisee = formation?.CompetenceVisee;

            Competence? competence = !string.IsNullOrEmpty(competenceVisee)
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
                        CollaborateurId = inscription.CollaborateurId
                    };
                    _context.Competences.Add(competence);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Formation terminée ! La compétence '{competence.Nom}' a été créée (niveau 1/{competence.NiveauCible}).";
                    break;
                case CompetenceCompletionOutcome.Incremented:
                    competence!.NiveauActuel = decision.NouveauNiveau!.Value;
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Félicitations ! Niveau {competence.Nom} augmenté à {competence.NiveauActuel}/5.";
                    break;
                case CompetenceCompletionOutcome.AlreadyAtTarget:
                    TempData["Success"] = "Formation terminée. La compétence visée a déjà atteint son objectif.";
                    break;
                default:
                    TempData["Success"] = "Formation terminée (aucune compétence associée).";
                    break;
            }
            return RedirectToAction("Index", "Collaborateurs");
        }
[HttpPost]
public async Task<IActionResult> PlanifierExamen(int inscriptionId, DateTime dateExamen)
{
    var inscription = await _context.Inscriptions.FindAsync(inscriptionId);
    if (inscription == null) return NotFound();
    inscription.DateExamen = dateExamen;
    await _context.SaveChangesAsync();
    TempData["Success"] = "Examen planifié.";
    return RedirectToAction("Index", "Formations", new { collaborateurId = inscription.CollaborateurId });
}
        private bool InscriptionExists(int id)
        {
            return _context.Inscriptions.Any(e => e.Id == id);
        }
    }
}