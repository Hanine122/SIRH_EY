using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

using SIRH.EY.Data;

using SIRH.EY.Models;

using SIRH.EY.Services;
using Microsoft.AspNetCore.Identity;

using System;

using System.Collections.Generic;

using System.Linq;

using System.Threading.Tasks;



namespace SIRH.EY.Controllers;

public class RecommendationRequest

{

    public string UserPrompt { get; set; } = string.Empty;

}



public class CollaborateursController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly FlowiseService _flowiseService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CollaborateursController(
        ApplicationDbContext context,
        FlowiseService flowiseService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _flowiseService = flowiseService;
        _userManager = userManager;
    }

    // public CollaborateursController(ApplicationDbContext context, FlowiseService flowiseService)

    // {

    //     _context = context;

    //     _flowiseService = flowiseService;

    // }

[HttpPost]

public async Task<IActionResult> RecommendFormation([FromBody] RecommendationRequest request)

{

    if (request == null || string.IsNullOrEmpty(request.UserPrompt))

        return BadRequest(new { message = "Le champ 'userPrompt' est requis." });



    var iaResponse = await _flowiseService.GetPredictionAsync(request.UserPrompt);

    

    if (string.IsNullOrEmpty(iaResponse))

        return StatusCode(500, new { message = "L'IA n'a pas pu générer de recommandation." });

    

    return Ok(new { responseIA = iaResponse });

}



[HttpPost]

public async Task<IActionResult> AskIA([FromBody] RecommendationRequest request)

{

    if (request == null || string.IsNullOrWhiteSpace(request.UserPrompt))

        return BadRequest(new { message = "La question est vide." });



    var reponse = await _flowiseService.GetPredictionAsync(request.UserPrompt);

    

    if (reponse == null)

        return StatusCode(500, new { message = "Flowise n'a pas répondu." });



    return Ok(new { reponse = reponse });

}

    public async Task<IActionResult> Index(
    string searchString = null,
    string sortOrder = null,
    string departement = null)
{
    ViewBag.Search = searchString;
    ViewBag.CurrentSort = sortOrder;
    ViewBag.NameSortParam = sortOrder == "name_asc"
        ? "name_desc"
        : "name_asc";

    ViewBag.DepartementFilter = departement;

    var user = await _userManager.GetUserAsync(User);

    if (user == null)
    {
        return RedirectToAction("Login", "Account");
    }

    IQueryable<Collaborateur> collaborateurs =
        _context.Collaborateurs;

    if (User.IsInRole("Collaborateur"))
    {
        var collab = await _context.Collaborateurs
            .FirstOrDefaultAsync(c => c.UserId == user.Id);

        if (collab != null)
        {
            collaborateurs = collaborateurs
                .Where(c => c.Id == collab.Id);
        }
    }

    else if (User.IsInRole("Manager"))
    {
        var manager = await _context.Collaborateurs
            .FirstOrDefaultAsync(c => c.UserId == user.Id);

        if (manager != null)
        {
            collaborateurs = collaborateurs
                .Where(c => c.ManagerId == manager.Id);
        }
    }

    // =========================
    // FILTRES
    // =========================
    if (!string.IsNullOrEmpty(departement))
    {
        collaborateurs = collaborateurs
            .Where(c => c.Departement == departement);
    }

    if (!string.IsNullOrEmpty(searchString))
    {
        collaborateurs = collaborateurs.Where(c =>
            c.Nom.Contains(searchString) ||
            c.Prenom.Contains(searchString) ||
            c.Email.Contains(searchString));
    }

    // =========================
    // TRI
    // =========================
    collaborateurs = sortOrder == "name_desc"
        ? collaborateurs.OrderByDescending(c => c.Nom)
        : collaborateurs.OrderBy(c => c.Nom);

    // =========================
    // VIEWBAGS
    // =========================
    ViewBag.Departements = await _context.Collaborateurs
        .Select(c => c.Departement)
        .Where(d => d != null)
        .Distinct()
        .ToListAsync();

    ViewBag.Managers = await _context.Collaborateurs
        .Where(c =>
            c.Actif &&
            (c.Grade == "Manager" ||
            (c.Poste ?? "").Contains("Manager")))
        .OrderBy(c => c.Nom)
        .ToListAsync();

    return View(await collaborateurs.ToListAsync());
}



    // GET: Collaborateurs/ChoisirRemplacant/

    public async Task<IActionResult> ChoisirRemplacant(int id)

    {

        var partant = await _context.Collaborateurs

            .Include(c => c.Competences)

            .FirstOrDefaultAsync(c => c.Id == id);

        if (partant == null) return NotFound();



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

        var requises = surProfil

            .Union(surPoste, comparer)

            .Distinct(comparer)

            .ToList();



        var formations = await _context.Formations.AsNoTracking().ToListAsync();



        var autres = await _context.Collaborateurs

            .Include(c => c.Competences)

            .Where(c => c.Id != id && c.Actif)

            .ToListAsync();



        var deptPartant = (partant.Departement ?? "").Trim();



        var candidats = new List<CandidatDetail>();

        foreach (var autre in autres)

        {

            var nomsAutre = autre.Competences?

                .Where(c => !string.IsNullOrWhiteSpace(c.Nom))

                .Select(c => c.Nom.Trim())

                .Distinct(comparer)

                .ToList() ?? new List<string>();



            var communes = requises.Count(r => nomsAutre.Any(a => comparer.Equals(a, r)));

            var manquantes = requises.Where(r => !nomsAutre.Any(a => comparer.Equals(a, r))).ToList();

            var deptAutre = (autre.Departement ?? "").Trim();

            var autreDept = deptPartant.Length == 0 || deptAutre.Length == 0

                ? !string.Equals(deptPartant, deptAutre, StringComparison.OrdinalIgnoreCase)

                : !deptPartant.Equals(deptAutre, StringComparison.OrdinalIgnoreCase);



            var profilTransversal = autreDept && communes > 0;



            var titresFormations = new List<string>();

            foreach (var m in manquantes)

            {

                var f = formations.FirstOrDefault(x =>

                    !string.IsNullOrEmpty(x.CompetenceVisee) &&

                    x.CompetenceVisee.Trim().Equals(m, StringComparison.OrdinalIgnoreCase));

                f ??= formations.FirstOrDefault(x =>

                    (x.Titre ?? "").Contains(m, StringComparison.OrdinalIgnoreCase));

                if (f != null && !titresFormations.Contains(f.Titre))

                    titresFormations.Add(f.Titre);

                else if (f == null)

                    titresFormations.Add($"Parcours recommandé — {m}");

            }



            candidats.Add(new CandidatDetail

            {

                Id = autre.Id,

                Prenom = autre.Prenom ?? "",

                Nom = autre.Nom ?? "",

                Email = autre.Email ?? "",

                Poste = autre.Poste ?? "",

                Departement = autre.Departement ?? "",

                Grade = autre.Grade ?? "",

                CompetencesManquantes = manquantes,

                FormationsRecommande = titresFormations.Distinct().ToList(),

                NbCompetencesCommunes = communes,

                ProfilTransversal = profilTransversal

            });

        }



        var ordre = candidats

            .OrderByDescending(c => c.ProfilTransversal)

            .ThenByDescending(c => c.NbCompetencesCommunes)

            .ThenBy(c => c.CompetencesManquantes.Count)

            .ThenBy(c => c.Nom)

            .ToList();



        var vm = new ChoisirRemplacantViewModel

        {

            Partant = partant,

            CompetencesRequises = requises,

            Candidats = ordre

        };



        return View(vm);

    }



    // GET: Collaborateurs/Details/5

    public async Task<IActionResult> Details(int? id)

    {

        if (id == null) return NotFound();

        var collaborateur = await _context.Collaborateurs.FirstOrDefaultAsync(m => m.Id == id);

        if (collaborateur == null) return NotFound();



        ViewBag.Competences = await _context.Competences.Where(c => c.CollaborateurId == id).ToListAsync();

        ViewBag.Inscriptions = await _context.Inscriptions.Include(i => i.Formation).Where(i => i.CollaborateurId == id).ToListAsync();

        ViewBag.Manager = collaborateur.ManagerId.HasValue

            ? await _context.Collaborateurs.FirstOrDefaultAsync(c => c.Id == collaborateur.ManagerId.Value)

            : null;

        return View(collaborateur);

    }



    // GET: Collaborateurs/Create

    public IActionResult Create()

    {

        ViewBag.Managers = _context.Collaborateurs

            .Where(c => c.Actif && (c.Grade == "Manager" || (c.Poste ?? "").Contains("Manager")))

            .ToList();



        // Ajouter les listes déroulantes depuis le service

        ViewBag.Departements = CompetenceCatalogService.Departements;

        ViewBag.Postes = CompetenceCatalogService.Postes;

        ViewBag.Grades = CompetenceCatalogService.Grades;



        return View();

    }



    [HttpPost]

    [ValidateAntiForgeryToken]

    public async Task<IActionResult> Create([Bind("Id,Nom,Prenom,Email,Departement,Grade,Poste,ManagerId,DateEmbauche,Actif")] Collaborateur collaborateur)

    {

        if (ModelState.IsValid)

        {

            _context.Add(collaborateur);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));

        }

        // Recharger les listes en cas d'erreur

        ViewBag.Managers = await _context.Collaborateurs

            .Where(c => c.Actif && (c.Grade == "Manager" || (c.Poste ?? "").Contains("Manager")))

            .OrderBy(c => c.Nom)

            .ToListAsync();

        ViewBag.Departements = CompetenceCatalogService.Departements;

        ViewBag.Postes = CompetenceCatalogService.Postes;

        ViewBag.Grades = CompetenceCatalogService.Grades;

        return View(collaborateur);

    }



    // GET: Collaborateurs/Edit/5

    public async Task<IActionResult> Edit(int? id)

    {

        if (id == null) return NotFound();

        var collaborateur = await _context.Collaborateurs.FindAsync(id);

        if (collaborateur == null) return NotFound();

        ViewBag.Managers = await _context.Collaborateurs

            .Where(c => c.Actif && c.Id != id && (c.Grade == "Manager" || (c.Poste ?? "").Contains("Manager")))

            .OrderBy(c => c.Nom)

            .ToListAsync();

        ViewBag.Departements = CompetenceCatalogService.Departements;

        ViewBag.Postes = CompetenceCatalogService.Postes;

        ViewBag.Grades = CompetenceCatalogService.Grades;

        return View(collaborateur);

    }



    [HttpPost]

    [ValidateAntiForgeryToken]

    public async Task<IActionResult> Edit(int id, [Bind("Id,Nom,Prenom,Email,Departement,Grade,Poste,ManagerId,DateEmbauche,Actif")] Collaborateur collaborateur)

    {

        if (id != collaborateur.Id) return NotFound();

        if (ModelState.IsValid)

        {

            try

            {

                _context.Update(collaborateur);

                await _context.SaveChangesAsync();

            }

            catch (DbUpdateConcurrencyException)

            {

                if (!_context.Collaborateurs.Any(e => e.Id == id)) return NotFound();

                throw;

            }

            return RedirectToAction(nameof(Index));

        }

        // Recharger les listes en cas d'erreur

        ViewBag.Managers = await _context.Collaborateurs

            .Where(c => c.Actif && c.Id != id && (c.Grade == "Manager" || (c.Poste ?? "").Contains("Manager")))

            .OrderBy(c => c.Nom)

            .ToListAsync();

        ViewBag.Departements = CompetenceCatalogService.Departements;

        ViewBag.Postes = CompetenceCatalogService.Postes;

        ViewBag.Grades = CompetenceCatalogService.Grades;

        return View(collaborateur);

    }



    [HttpPost]

    [ValidateAntiForgeryToken]

    public async Task<IActionResult> AssignerManager(int managerId, List<int> selectedCollaborateurIds)

    {

        if (managerId <= 0 || selectedCollaborateurIds == null || !selectedCollaborateurIds.Any())

        {

            TempData["Error"] = "Sélectionnez au moins un collaborateur et un manager.";

            return RedirectToAction(nameof(Index));

        }



        var manager = await _context.Collaborateurs.FindAsync(managerId);

        if (manager == null)

        {

            TempData["Error"] = "Manager introuvable.";

            return RedirectToAction(nameof(Index));

        }



        var collaborateurs = await _context.Collaborateurs

            .Where(c => selectedCollaborateurIds.Contains(c.Id) && c.Id != managerId)

            .ToListAsync();



        foreach (var collaborateur in collaborateurs)

            collaborateur.ManagerId = managerId;



        await _context.SaveChangesAsync();

        TempData["Success"] = $"Manager {manager.Prenom} {manager.Nom} assigné à {collaborateurs.Count} collaborateur(s).";

        return RedirectToAction(nameof(Index));

    }



    // GET: Collaborateurs/Delete/5

    public async Task<IActionResult> Delete(int? id)

    {

        if (id == null) return NotFound();

        var collaborateur = await _context.Collaborateurs.FirstOrDefaultAsync(m => m.Id == id);

        if (collaborateur == null) return NotFound();

        return View(collaborateur);

    }



    [HttpPost, ActionName("Delete")]

    [ValidateAntiForgeryToken]

    public async Task<IActionResult> DeleteConfirmed(int id)

    {

        var collaborateur = await _context.Collaborateurs.FindAsync(id);

        if (collaborateur != null) _context.Collaborateurs.Remove(collaborateur);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));

    }



    // GET: Collaborateurs/Depart/5

    public async Task<IActionResult> Depart(int id)

    {

        var collaborateur = await _context.Collaborateurs.FindAsync(id);

        if (collaborateur == null) return NotFound();



        var remplacant = await _context.Collaborateurs

            .Where(c => c.Actif && c.Departement == collaborateur.Departement && c.Poste == collaborateur.Poste && c.Id != id)

            .FirstOrDefaultAsync();



        if (remplacant == null)

        {

            TempData["Error"] = $"Aucun remplaçant disponible dans le département {collaborateur.Departement} avec le poste {collaborateur.Poste}.";

            return RedirectToAction(nameof(Index));

        }



        var competencesPartant = await _context.Competences

            .Where(c => c.CollaborateurId == id && c.NiveauCible >= 4)

            .ToListAsync();



        var competencesRemplacant = await _context.Competences

            .Where(c => c.CollaborateurId == remplacant.Id)

            .ToListAsync();



        var competencesManquantes = competencesPartant

            .Where(cp => !competencesRemplacant.Any(cr => cr.Nom == cp.Nom && cr.NiveauActuel >= cp.NiveauCible))

            .Select(cp => cp.Nom)

            .ToList();



        var formationsRecommande = new List<string>();

        foreach (var comp in competencesManquantes)

        {

            var formation = await _context.Formations

                .Where(f => f.Titre.Contains(comp) || f.Categorie.Contains(comp))

                .Select(f => f.Titre)

                .FirstOrDefaultAsync();

            if (formation != null)

                formationsRecommande.Add(formation);

        }



        var model = new DepartViewModel

        {

            CollaborateurPartant = collaborateur,

            CollaborateurRemplacant = remplacant,

            CompetencesManquantes = competencesManquantes,

            FormationsRecommande = formationsRecommande

        };

        return View(model);

    }



    [HttpPost]

    [ValidateAntiForgeryToken]

    public async Task<IActionResult> ConfirmDepart(int idPartant, int idRemplacant)

    {

        var partant = await _context.Collaborateurs.FindAsync(idPartant);

        if (partant != null)

        {

            partant.Actif = false;

            _context.Update(partant);

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Départ de {partant.Prenom} {partant.Nom} enregistré. Le remplaçant est maintenant actif.";

        }

        return RedirectToAction(nameof(Index));

    }



    [HttpGet]

    public async Task<IActionResult> GetProfilCandidat(int id)

    {

        var collab = await _context.Collaborateurs.FindAsync(id);

        if (collab == null) return NotFound();

        return Ok(new {

            collab.Prenom,

            collab.Nom,

            collab.Email,

            collab.Poste,

            collab.Departement,

            collab.Grade,

            collab.DateEmbauche,

            TypeContrat = "CDI"

        });

    }



    [HttpPost]

    public async Task<IActionResult> EnvoyerDemandeEntretiens([FromBody] DemandeEntretienRequest request)

    {

        if (request == null || request.CandidatsIds == null || !request.CandidatsIds.Any())

            return Ok(new { success = false, message = "Aucun candidat sélectionné." });



        var partant = await _context.Collaborateurs.FindAsync(request.PartantId);

        if (partant == null) return Ok(new { success = false, message = "Partant introuvable." });



        var candidats = await _context.Collaborateurs

            .Where(c => request.CandidatsIds.Contains(c.Id))

            .ToListAsync();



        var rhEmail = "rh@ey.com";

        var sujet = $"Demande d'entretiens pour remplacement de {partant.Prenom} {partant.Nom}";

        var corps = $"Bonjour,\n\nUne demande d'entretien a été soumise pour le remplacement de {partant.Prenom} {partant.Nom}.\n\n";

        corps += "Candidats sélectionnés :\n";

        foreach (var c in candidats)

            corps += $"- {c.Prenom} {c.Nom} ({c.Poste}, {c.Departement})\n";

        if (!string.IsNullOrEmpty(request.Commentaire))

            corps += $"\nCommentaire du manager : {request.Commentaire}\n";

        corps += "\nMerci de préparer les entretiens physiques.\n\nCordialement.";



        System.Diagnostics.Debug.WriteLine($"Email à {rhEmail} : {sujet}\n{corps}");

        return Ok(new { success = true });

    }



    [HttpPost]

    [ValidateAntiForgeryToken]

    public async Task<IActionResult> ConfirmerRemplacement(int partantId, int remplacantId)

    {

        var partant = await _context.Collaborateurs.FindAsync(partantId);

        var remplacant = await _context.Collaborateurs.FindAsync(remplacantId);

        if (partant == null || remplacant == null) return NotFound();



        partant.Actif = false;

        remplacant.Poste = partant.Poste;

        remplacant.Departement = partant.Departement;



        await _context.SaveChangesAsync();



        TempData["Success"] = $"Le départ de {partant.Prenom} {partant.Nom} a été enregistré. {remplacant.Prenom} {remplacant.Nom} est désormais le nouveau collaborateur sur ce poste.";



        return RedirectToAction(nameof(Index));

    }



    [HttpPost]

    [ValidateAntiForgeryToken]

    public async Task<IActionResult> ExportComparaisonRemplacantsPdf(int partantId, string candidatIds)

    {

        var ids = (candidatIds ?? "")

            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)

            .Select(s => int.TryParse(s, out var id) ? id : 0)

            .Where(id => id > 0)

            .Distinct()

            .Take(3)

            .ToList();



        var partant = await _context.Collaborateurs

            .Include(c => c.Competences)

            .FirstOrDefaultAsync(c => c.Id == partantId);

        if (partant == null) return NotFound();



        var comparer = StringComparer.OrdinalIgnoreCase;



        var surProfil = partant.Competences?

            .Where(c => !string.IsNullOrWhiteSpace(c.Nom))

            .Select(c => c.Nom.Trim())

            .Distinct(comparer)

            .ToList() ?? new List<string>();



        var surPoste = await _context.CompetencesRequisesParPoste

            .AsNoTracking()

            .Where(cr => cr.Poste == partant.Poste)

            .Select(cr => cr.Competence.Trim())

            .Distinct()

            .ToListAsync();



        var competencesRequises = surProfil

            .Union(surPoste, comparer)

            .Distinct(comparer)

            .ToList();



        var formations = await _context.Formations.AsNoTracking().ToListAsync();



        var candidatsDb = await _context.Collaborateurs

            .Include(c => c.Competences)

            .Where(c => ids.Contains(c.Id))

            .ToListAsync();



        // garder l'ordre de sélection

        var candidatsOrdonnes = ids

            .Select(id => candidatsDb.FirstOrDefault(c => c.Id == id))

            .Where(c => c != null)

            .Cast<Collaborateur>()

            .ToList();



        int CompatPour(Collaborateur c)

        {

            if (competencesRequises.Count == 0) return 0;

            var noms = c.Competences?

                .Where(x => !string.IsNullOrWhiteSpace(x.Nom))

                .Select(x => x.Nom.Trim())

                .Distinct(comparer)

                .ToList() ?? new List<string>();

            var manq = competencesRequises.Count(r => !noms.Any(a => comparer.Equals(a, r)));

            var poss = Math.Max(0, competencesRequises.Count - manq);

            return (int)Math.Round(100.0 * poss / competencesRequises.Count);

        }



        List<string> Manquantes(Collaborateur c)

        {

            var noms = c.Competences?

                .Where(x => !string.IsNullOrWhiteSpace(x.Nom))

                .Select(x => x.Nom.Trim())

                .Distinct(comparer)

                .ToList() ?? new List<string>();

            return competencesRequises.Where(r => !noms.Any(a => comparer.Equals(a, r))).ToList();

        }



        var candidats = new List<ComparaisonPdfCandidat>();

        foreach (var c in candidatsOrdonnes)

        {

            var manq = Manquantes(c);

            var titresFormations = new List<string>();

            foreach (var m in manq)

            {

                var f = formations.FirstOrDefault(x =>

                    !string.IsNullOrEmpty(x.CompetenceVisee) &&

                    x.CompetenceVisee.Trim().Equals(m, StringComparison.OrdinalIgnoreCase));

                f ??= formations.FirstOrDefault(x => (x.Titre ?? "").Contains(m, StringComparison.OrdinalIgnoreCase));

                if (f != null && !string.IsNullOrWhiteSpace(f.Titre) && !titresFormations.Contains(f.Titre))

                    titresFormations.Add(f.Titre);

            }



            candidats.Add(new ComparaisonPdfCandidat(

                c.Id,

                $"{c.Prenom} {c.Nom}".Trim(),

                c.Departement ?? "-",

                CompatPour(c),

                manq.Count,

                manq.Take(3).ToList(),

                titresFormations.Take(2).ToList()

            ));

        }



        var lignes = competencesRequises.Select(comp =>

        {

            var coverage = candidatsOrdonnes.Select(c =>

            {

                var manq = Manquantes(c);

                return !manq.Any(m => comparer.Equals(m, comp));

            }).ToList();

            return new ComparaisonPdfRow(comp, coverage);

        }).ToList();



        var titre = "Comparaison des remplaçants (succession)";

        var sousTitre = $"Poste : {partant.Poste ?? "-"} · Partant : {partant.Prenom} {partant.Nom} · Département : {partant.Departement ?? "-"}";

        var pdf = ComparaisonRemplacantsPdf.Generer(titre, sousTitre, competencesRequises, candidats, lignes);



        return File(pdf, "application/pdf", "Comparaison_remplacants.pdf");

    }



    [HttpGet]

    public async Task<IActionResult> GetRemplacants(int id)

{

    var partant = await _context.Collaborateurs.FindAsync(id);

    if (partant == null) return NotFound();



    var competencesRequises = await _context.Competences

        .Where(c => c.CollaborateurId == id && c.NiveauCible >= 4)

        .Select(c => c.Nom)

        .ToListAsync();



    var candidats = await _context.Collaborateurs

        .Where(c => c.Actif && c.Id != id && c.Grade == partant.Grade)

        .ToListAsync();



    if (!candidats.Any())

        return Ok(new { message = $"Aucun autre collaborateur de grade {partant.Grade} trouvé." });



    var resultats = new List<object>();

    foreach (var candidat in candidats)

    {

        var competencesCandidat = await _context.Competences

            .Where(c => c.CollaborateurId == candidat.Id)

            .Select(c => c.Nom)

            .ToListAsync();



        var manquantes = competencesRequises.Except(competencesCandidat).ToList();



        var formations = new List<string>();

        foreach (var comp in manquantes)

        {

            var formation = await _context.Formations

                .Where(f => f.CompetenceVisee == comp)

                .Select(f => f.Titre)

                .FirstOrDefaultAsync();

            formations.Add(formation ?? $"Formation générique en {comp}");

        }



        resultats.Add(new

        {

            id = candidat.Id,

            prenom = candidat.Prenom,

            nom = candidat.Nom,

            email = candidat.Email,

            poste = candidat.Poste ?? "Non défini",

            departement = candidat.Departement ?? "Non défini",

            competencesManquantes = manquantes,

            formationsRecommande = formations,

            nbManquantes = manquantes.Count

        });

    }



    // Tri direct sur la propriété nbManquantes via une liste typée dynamiquement

    var ordered = resultats.OrderBy(r => ((dynamic)r).nbManquantes).ToList();

    return Ok(ordered);

}



    [HttpGet]
    public IActionResult GetPostesParDepartement(string departement)
    {
        var postes = new List<string>();

        if (string.IsNullOrWhiteSpace(departement))
            return Json(new List<object>());

        var normalizedDept = departement.Trim().ToLower();

        switch (normalizedDept)
        {
            case "assurance":
                postes = new List<string> { "Audit", "Financial Accounting Advisory Services & Risk", "Climate Change and Sustainability Services", "Forensic & Integrity Services", "Managed Services", "Technology Risk" };
                break;
            case "consulting":
                postes = new List<string> { "Business Transformation", "Supply chain & operations", "Financial Services transformation", "Actuarial Services", "People Consulting", "Innovation & Experience Design", "Technology Strategy & Transformation", "AI and DATA", "Digital Engineering", "Platforms-Microsoft", "Cyber Security" };
                break;
            case "strategy & transactions":
                postes = new List<string> { "Transaction Diligence", "Valuation Modeling & Economics", "Lead Advisory", "Corporate and Growth Strategy", "Turnaround and Restructuring Strategy", "Transaction Strategy and Execution" };
                break;
            case "tax":
                postes = new List<string> { "Global Compliance and Reporting", "Business Tax Services and Advisory", "International Tax Advisory", "Transaction Tax Services", "People Advisory Services", "Entity Compliance and Governance Services", "Labor & Employment Law Advise" };
                break;
            case "talent team":
                postes = new List<string> { "Recrutement", "Suivi spécifique d'intégration", "Administration du personnel et paie", "Gestion de stages", "EY Academy : Formation et développement des compétences", "Gestion de carrière", "Communication interne et bien-être" };
                break;
            case "service it":
                postes = new List<string> { "Support IT" };
                break;
            case "finances et contrôle":
                postes = new List<string> { "Comptabilité analytique et facturation" };
                break;
            case "facilities":
                postes = new List<string> { "Voyages", "Bâtiment", "Hospitalité", "Achats et moyens généraux" };
                break;
            case "mbd":
                postes = new List<string> { "Projets de marketing", "Communication numérique", "Reporting", "Soutien aux appels d'offres" };
                break;
            case "risk management":
                postes = new List<string> { "Gestion des risques liés aux affaires, au client, aux missions" };
                break;
        }

        var result = postes.Select(p => new { value = p, label = p });
        return Json(result);
    }

    // Classe interne pour la demande d'entretien

    public class DemandeEntretienRequest

    {

        public int PartantId { get; set; }

        public List<int> CandidatsIds { get; set; }

        public string Commentaire { get; set; }

    }

}