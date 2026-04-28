using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;
using SIRH.EY.Services;

namespace SIRH.EY.Controllers
{
    public class FormationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IParametreService _parametreService;

       public FormationsController(ApplicationDbContext context, IParametreService parametreService)
        {
            _context = context;
            _parametreService = parametreService;
        }

        // GET: Formations (version simplifiée : catalogue complet)
        public async Task<IActionResult> Index(int? collaborateurId)
        {
            if (collaborateurId == null)
            {
                var premier = await _context.Collaborateurs.FirstOrDefaultAsync();
                if (premier == null) return RedirectToAction("Create", "Collaborateurs");
                collaborateurId = premier.Id;
            }

            ViewBag.CollaborateurId = collaborateurId;

            var inscriptions = await _context.Inscriptions
                .Include(i => i.Formation)
                .Where(i => i.CollaborateurId == collaborateurId)
                .ToListAsync();
            ViewBag.Inscriptions = inscriptions;

            var toutesFormations = await _context.Formations.OrderBy(f => f.Titre).ToListAsync();
            var formationsInscritesIds = inscriptions.Select(i => i.FormationId).Distinct().ToList();
            ViewBag.Catalogue = toutesFormations.Where(f => !formationsInscritesIds.Contains(f.Id)).ToList();
            ViewBag.ToutesFormations = toutesFormations;
            ViewBag.FormationsInscritesIds = formationsInscritesIds;

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
            var inscription = await _context.Inscriptions
                .Include(i => i.Formation)
                .Include(i => i.Collaborateur)
                .FirstOrDefaultAsync(i => i.Id == inscriptionId);
            if (inscription == null || inscription.Terminee)
                return NotFound();

            return View("ModuleFormation", inscription);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AvancerModule(int inscriptionId, int deltaPourcent = 20)
        {
            var inscription = await _context.Inscriptions
                .Include(i => i.Formation)
                .FirstOrDefaultAsync(i => i.Id == inscriptionId);
            if (inscription == null || inscription.Terminee)
                return NotFound();

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
            var formation = await _context.Formations.FindAsync(formationId);
            if (formation != null && formation.PlacesPrises < formation.CapaciteMax)
            {
                var inscription = new Inscription
                {
                    FormationId = formationId,
                    CollaborateurId = collaborateurId,
                    DateInscription = DateTime.Now,
                    Terminee = false
                };
                _context.Inscriptions.Add(inscription);
                formation.PlacesPrises++;
                await _context.SaveChangesAsync();
                TempData["Message"] = "Inscription réussie !";
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
            var inscription = await _context.Inscriptions
                .Include(i => i.Formation)
                .FirstOrDefaultAsync(i => i.Id == inscriptionId);
            if (inscription == null) return NotFound();

            var formation = inscription.Formation;
            if (formation != null)
            {
                formation.PlacesPrises--;
                _context.Update(formation);
            }
            _context.Inscriptions.Remove(inscription);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Inscription annulée.";
            return RedirectToAction(nameof(Index), new { collaborateurId = inscription.CollaborateurId });
        }

        // GET: Formations/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Titre,Formateur,DureeHeures,CapaciteMax,PlacesPrises,Categorie,DateDebut,Organisme,CompetenceVisee")] Formation formation)
        {
            if (ModelState.IsValid)
            {
                _context.Add(formation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(formation);
        }

        // GET: Formations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var formation = await _context.Formations.FindAsync(id);
            if (formation == null) return NotFound();
            return View(formation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Titre,Formateur,DureeHeures,CapaciteMax,PlacesPrises,Categorie,DateDebut,Organisme,CompetenceVisee")] Formation formation)
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
            return View(formation);
        }
        [HttpPost]
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

    inscription.Terminee = true;
    await _context.SaveChangesAsync();

    var formation = inscription.Formation;
    if (formation != null && !string.IsNullOrEmpty(formation.CompetenceVisee))
    {
        // Chercher la compétence existante
        var competence = await _context.Competences
            .FirstOrDefaultAsync(c => c.CollaborateurId == inscription.CollaborateurId && c.Nom == formation.CompetenceVisee);
        
        if (competence == null)
        {
            // Créer la compétence automatiquement
            var collaborateur = await _context.Collaborateurs.FindAsync(inscription.CollaborateurId);
            int niveauCible = CompetenceRules.GetNiveauCibleParGrade(collaborateur?.Grade ?? "Junior");
            competence = new Competence
            {
                Nom = formation.CompetenceVisee,
                Categorie = "À définir",
                NiveauActuel = 1,
                NiveauCible = niveauCible,
                DateEvaluation = DateTime.Now,
                CollaborateurId = inscription.CollaborateurId
            };
            _context.Competences.Add(competence);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Formation terminée ! La compétence '{competence.Nom}' a été créée avec un niveau 1/{competence.NiveauCible}.";
        }
        else if (competence.NiveauActuel < competence.NiveauCible)
        {
            competence.NiveauActuel = Math.Min(competence.NiveauActuel + 1, competence.NiveauCible);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Formation terminée ! Niveau de compétence '{competence.Nom}' augmenté à {competence.NiveauActuel}/{competence.NiveauCible}.";
        }
        else
        {
            TempData["Success"] = "Formation terminée. La compétence visée a déjà atteint son objectif.";
        }
    }
    else
    {
        TempData["Success"] = "Formation terminée (aucune compétence associée).";
    }

    return RedirectToAction(nameof(Index), new { collaborateurId = inscription.CollaborateurId });
}
        // GET: Formations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var formation = await _context.Formations.FirstOrDefaultAsync(m => m.Id == id);
            if (formation == null) return NotFound();
            return View(formation);
        }

        [HttpPost, ActionName("Delete")]
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
    }
}