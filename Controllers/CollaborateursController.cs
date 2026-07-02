using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Authorization;
using SIRH.EY.Data;
using SIRH.EY.Models;
using SIRH.EY.Services;
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
    private readonly ITeamAccessService _teamAccess;

    private readonly Microsoft.AspNetCore.Identity.UI.Services.IEmailSender _emailSender;

    public CollaborateursController(
        ApplicationDbContext context,
        FlowiseService flowiseService,
        UserManager<ApplicationUser> userManager,
        ITeamAccessService teamAccess,
        Microsoft.AspNetCore.Identity.UI.Services.IEmailSender emailSender)
    {
        _context = context;
        _flowiseService = flowiseService;
        _userManager = userManager;
        _teamAccess = teamAccess;
        _emailSender = emailSender;
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
        return RedirectToAction("Login", "Account");

    // Service applies role-aware data scope (ITAdmin/RH=all, Manager=team, Collaborateur=self)
    IQueryable<Collaborateur> collaborateurs =
        await _teamAccess.ApplyAccessFilterAsync(User, _context.Collaborateurs);

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

    [Authorize(Roles = Roles.ITAdminOrRH)]
    public async Task<IActionResult> ChoisirRemplacant(int id)
    {
        var partant = await _context.Collaborateurs
            .Include(c => c.Competences)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (partant == null) return NotFound();

        // 1. Compétences exigées : référentiel poste → fallback profil partant ≥ niveau 3
        var referentiel = await _context.CompetencesRequisesParPoste
            .AsNoTracking()
            .Where(cr => cr.Poste == partant.Poste)
            .ToListAsync();

        var exigences = SuccessionEngine.BuildExigences(referentiel, partant.Competences);
        var requisesNoms = exigences.Select(e => e.Nom).ToList();

        // 2. Formations disponibles pour les recommandations
        var formations = await _context.Formations.AsNoTracking().ToListAsync();

        // 3. Pool de candidats (hors partant, actifs)
        var deptPartant = (partant.Departement ?? "").Trim();
        var autres = await _context.Collaborateurs
            .Include(c => c.Competences)
            .Where(c => c.Id != id && c.Actif)
            .ToListAsync();

        // 4. Scoring via moteur partagé
        var scores = autres
            .Select(a => SuccessionEngine.Score(a, exigences, deptPartant))
            .ToList();

        // 5. Mapper ResultatScore → CandidatDetail
        CandidatDetail ToDetail(ResultatScore s)
        {
            var titresFormations = new List<string>();
            foreach (var m in s.Manquantes)
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

            return new CandidatDetail
            {
                Id                    = s.Candidat.Id,
                Prenom                = s.Candidat.Prenom ?? "",
                Nom                   = s.Candidat.Nom ?? "",
                Email                 = s.Candidat.Email ?? "",
                Poste                 = s.Candidat.Poste ?? "",
                Departement           = s.Candidat.Departement ?? "",
                Grade                 = s.Candidat.Grade ?? "",
                CompetencesManquantes = s.Manquantes,
                FormationsRecommande  = titresFormations.Distinct().ToList(),
                NbCompetencesCommunes = s.NbCommunes,
                ProfilTransversal     = s.ProfilTransversal,
                ScoreSuccession       = s.ScoreSuccession,
                ScoreCouverture       = s.ScoreCouverture
            };
        }

        // 6. Pool principal : même grade ET éligible ET au moins 1 compétence commune
        var ordre = scores
            .Where(s => string.Equals(s.Candidat.Grade, partant.Grade, StringComparison.OrdinalIgnoreCase)
                     && s.EstEligible
                     && s.NbCommunes > 0)
            .OrderByDescending(s => s.ScoreSuccession)
            .Select(ToDetail)
            .ToList();

        // 7. En attente : grade différent OU non éligible, avec au moins 1 compétence commune, max 3
        var enAttente = scores
            .Where(s => (!string.Equals(s.Candidat.Grade, partant.Grade, StringComparison.OrdinalIgnoreCase)
                      || !s.EstEligible)
                     && s.NbCommunes > 0)
            .OrderByDescending(s => s.ScoreSuccession)
            .Take(3)
            .Select(ToDetail)
            .ToList();

        var vm = new ChoisirRemplacantViewModel
        {
            Partant             = partant,
            CompetencesRequises = requisesNoms,
            Candidats           = ordre,
            CandidatsEnAttente  = enAttente
        };

        return View(vm);
    }



    // GET: Collaborateurs/Details/5

    public async Task<IActionResult> Details(int? id)

    {

        if (id == null) return NotFound();

        var collaborateur = await _context.Collaborateurs.FirstOrDefaultAsync(m => m.Id == id);

        if (collaborateur == null) return NotFound();

        if (!await _teamAccess.CanAccessCollaborateurAsync(User, id.Value))
            return Forbid();



        ViewBag.Competences = await _context.Competences.Where(c => c.CollaborateurId == id).ToListAsync();

        ViewBag.Inscriptions = await _context.Inscriptions.Include(i => i.Formation).Where(i => i.CollaborateurId == id).ToListAsync();

        ViewBag.Manager = collaborateur.ManagerId.HasValue

            ? await _context.Collaborateurs.FirstOrDefaultAsync(c => c.Id == collaborateur.ManagerId.Value)

            : null;

        return View(collaborateur);

    }



    // GET: Collaborateurs/Create

    [Authorize(Roles = Roles.ITAdminOrRH)]
    public async Task<IActionResult> Create()
    {
        ViewBag.Managers = await _context.Collaborateurs
            .Where(c => c.Actif && (c.Grade == "Manager" || (c.Poste ?? "").Contains("Manager")))
            .OrderBy(c => c.Nom)
            .ToListAsync();
        await LoadMasterDataAsync();
        return View();
    }

    [HttpPost]
    [Authorize(Roles = Roles.ITAdminOrRH)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Id,Nom,Prenom,Email,DateNaissance,Genre,Nationalite,EtatCivil,Adresse,Ville,Pays," +
              "TelephonePersonnel,ContactUrgence,Matricule,ManagerId,NiveauHierarchique,DateEmbauche," +
              "DatePrisePoste,FormationsObligatoires,NiveauPreparationSuccession,PotentielCarriere,Actif,Statut," +
              "DepartmentId,SubDepartmentId,PositionId,GradeId,BusinessUnitId,LocationId,ContractTypeId")]
        Collaborateur collaborateur)
    {
        if (ModelState.IsValid)
        {
            await SyncLegacyStringFieldsAsync(collaborateur);
            _context.Add(collaborateur);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Managers = await _context.Collaborateurs
            .Where(c => c.Actif && (c.Grade == "Manager" || (c.Poste ?? "").Contains("Manager")))
            .OrderBy(c => c.Nom)
            .ToListAsync();
        await LoadMasterDataAsync();
        return View(collaborateur);
    }



    // GET: Collaborateurs/Edit/5

    [Authorize(Roles = Roles.ITAdminOrRH)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var collaborateur = await _context.Collaborateurs.FindAsync(id);
        if (collaborateur == null) return NotFound();

        ViewBag.Managers = await _context.Collaborateurs
            .Where(c => c.Actif && c.Id != id && (c.Grade == "Manager" || (c.Poste ?? "").Contains("Manager")))
            .OrderBy(c => c.Nom)
            .ToListAsync();
        await LoadMasterDataAsync();
        return View(collaborateur);
    }



    [HttpPost]
    [Authorize(Roles = Roles.ITAdminOrRH)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("Id,Nom,Prenom,Email,DateNaissance,Genre,Nationalite,EtatCivil,Adresse,Ville,Pays," +
              "TelephonePersonnel,ContactUrgence,Matricule,ManagerId,NiveauHierarchique,DateEmbauche," +
              "DatePrisePoste,FormationsObligatoires,NiveauPreparationSuccession,PotentielCarriere,Actif,Statut," +
              "DepartmentId,SubDepartmentId,PositionId,GradeId,BusinessUnitId,LocationId,ContractTypeId")]
        Collaborateur collaborateur)
    {
        if (id != collaborateur.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                await SyncLegacyStringFieldsAsync(collaborateur);
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

        ViewBag.Managers = await _context.Collaborateurs
            .Where(c => c.Actif && c.Id != id && (c.Grade == "Manager" || (c.Poste ?? "").Contains("Manager")))
            .OrderBy(c => c.Nom)
            .ToListAsync();
        await LoadMasterDataAsync();
        return View(collaborateur);
    }



    [HttpPost]
    [Authorize(Roles = Roles.ITAdminOrRH)]
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

    [Authorize(Roles = Roles.ITAdminOrRH)]
    public async Task<IActionResult> Delete(int? id)

    {

        if (id == null) return NotFound();

        var collaborateur = await _context.Collaborateurs.FirstOrDefaultAsync(m => m.Id == id);

        if (collaborateur == null) return NotFound();

        return View(collaborateur);

    }



    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = Roles.ITAdminOrRH)]
    [ValidateAntiForgeryToken]

    public async Task<IActionResult> DeleteConfirmed(int id)

    {

        var collaborateur = await _context.Collaborateurs.FindAsync(id);

        if (collaborateur != null) _context.Collaborateurs.Remove(collaborateur);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));

    }



    // GET: Collaborateurs/Depart/5

    [Authorize(Roles = Roles.ITAdminOrRH)]
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



        var competencesManquantes = RemplacantMatchingEngine.BuildCompetencesManquantesSimple(competencesPartant, competencesRemplacant);



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
    [Authorize(Roles = Roles.ITAdminOrRH)]
    [ValidateAntiForgeryToken]

    public async Task<IActionResult> ConfirmDepart(int idPartant, int idRemplacant)

    {

        var partant = await _context.Collaborateurs.FindAsync(idPartant);
        var remplacant = await _context.Collaborateurs.FindAsync(idRemplacant);

        if (partant != null)
        {
            partant.Actif = false;
            _context.Update(partant);

            if (remplacant != null)
            {
                remplacant.Poste = partant.Poste;
                remplacant.Departement = partant.Departement;
                _context.Update(remplacant);
            }

            await _context.SaveChangesAsync();

            var nomRemplacant = remplacant != null ? $"{remplacant.Prenom} {remplacant.Nom}" : "le remplaçant désigné";
            TempData["Success"] = $"Départ de {partant.Prenom} {partant.Nom} enregistré. {nomRemplacant} est maintenant affecté au poste.";
        }

        return RedirectToAction(nameof(Index));

    }



    [HttpGet]
    [Authorize(Roles = Roles.ITAdminOrRH)]
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

            TypeContrat = collab.TypeContrat ?? "CDI"

        });

    }



    [HttpPost]
    [Authorize(Roles = Roles.ITAdminOrRH)]
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
        var sujet = $"Demande d'entretiens — remplacement de {partant.Prenom} {partant.Nom}";

        var lignesCandidats = string.Join("\n", candidats.Select(c => $"  • {c.Prenom} {c.Nom} ({c.Poste}, {c.Departement})"));
        var commentaireBloc = !string.IsNullOrEmpty(request.Commentaire)
            ? $"<p><strong>Commentaire du manager :</strong><br/>{System.Net.WebUtility.HtmlEncode(request.Commentaire)}</p>"
            : "";

        var html = $@"<p>Bonjour,</p>
<p>Une demande d'entretien a été soumise pour le remplacement de <strong>{partant.Prenom} {partant.Nom}</strong>.</p>
<p><strong>Candidats sélectionnés :</strong></p>
<ul>{string.Join("", candidats.Select(c => $"<li>{c.Prenom} {c.Nom} — {c.Poste}, {c.Departement}</li>"))}</ul>
{commentaireBloc}
<p>Merci de préparer les entretiens physiques.</p>
<p>Cordialement.</p>";

        await _emailSender.SendEmailAsync(rhEmail, sujet, html);

        return Ok(new { success = true });

    }



    [HttpPost]
    [Authorize(Roles = Roles.ITAdminOrRH)]
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
    [Authorize(Roles = Roles.ITAdminOrRH)]
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

        var surPoste = await _context.CompetencesRequisesParPoste

            .AsNoTracking()

            .Where(cr => cr.Poste == partant.Poste)

            .Select(cr => cr.Competence.Trim())

            .Distinct()

            .ToListAsync();

        var competencesRequises = SuccessionEngine.BuildCompetencesRequisesUnion(partant.Competences, surPoste);



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



        var candidats = new List<ComparaisonPdfCandidat>();

        foreach (var c in candidatsOrdonnes)

        {

            var manq = RemplacantMatchingEngine.CompetencesManquantesPourCandidat(competencesRequises, c.Competences);

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

                RemplacantMatchingEngine.CompatibilitePourcent(competencesRequises, c.Competences),

                manq.Count,

                manq.Take(3).ToList(),

                titresFormations.Take(2).ToList()

            ));

        }



        var lignes = competencesRequises.Select(comp =>

        {

            var coverage = candidatsOrdonnes.Select(c =>

            {

                var manq = RemplacantMatchingEngine.CompetencesManquantesPourCandidat(competencesRequises, c.Competences);

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
    [Authorize(Roles = Roles.ITAdminOrRH)]
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



        var manquantes = RemplacantMatchingEngine.CompetencesManquantesParNoms(competencesRequises, competencesCandidat);



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

    // ── Master data ViewBag loader (replaces CompetenceCatalogService + PrepareHrProfileViewData) ──

    private async Task LoadMasterDataAsync()
    {
        // Referential dropdowns from DB
        ViewBag.Departments   = await _context.Departments.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();
        ViewBag.SubDepartments= await _context.SubDepartments.Where(s => s.IsActive).Include(s => s.Department).OrderBy(s => s.Department.Name).ThenBy(s => s.Name).ToListAsync();
        ViewBag.Positions     = await _context.Positions.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();
        ViewBag.GradeEntities = await _context.Grades.Where(g => g.IsActive).OrderBy(g => g.Level).ToListAsync();
        ViewBag.BusinessUnitEntities = await _context.BusinessUnits.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync();
        ViewBag.LocationEntities     = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();
        ViewBag.ContractTypes = await _context.ContractTypes.Where(ct => ct.IsActive).OrderBy(ct => ct.Name).ToListAsync();

        // Static enumerations (not in master data)
        ViewBag.Genres             = new[] { "Femme", "Homme", "Non renseigne" };
        ViewBag.EtatsCivils        = new[] { "Celibataire", "Marie(e)", "Divorce(e)", "Veuf/Veuve" };
        ViewBag.NiveauxHierarchiques = new[] { "Junior", "Senior", "Manager", "Senior Manager", "Director", "Partner" };
        ViewBag.PotentielsCarriere = new[] { "Emergent", "Solide", "Haut potentiel", "Succession prioritaire" };
    }

    /// <summary>
    /// After saving a collaborateur with FK fields, sync the legacy string fields
    /// so existing views/queries that read the strings still work.
    /// </summary>
    private async Task SyncLegacyStringFieldsAsync(Collaborateur collaborateur)
    {
        if (collaborateur.DepartmentId.HasValue)
        {
            var dept = await _context.Departments.FindAsync(collaborateur.DepartmentId);
            collaborateur.Departement = dept?.Name;
        }

        if (collaborateur.PositionId.HasValue)
        {
            var pos = await _context.Positions.FindAsync(collaborateur.PositionId);
            collaborateur.Poste = pos?.Name;
        }

        if (collaborateur.GradeId.HasValue)
        {
            var grade = await _context.Grades.FindAsync(collaborateur.GradeId);
            collaborateur.Grade = grade?.Name;
        }

        if (collaborateur.BusinessUnitId.HasValue)
        {
            var bu = await _context.BusinessUnits.FindAsync(collaborateur.BusinessUnitId);
            collaborateur.BusinessUnit = bu?.Name;
        }

        if (collaborateur.LocationId.HasValue)
        {
            var loc = await _context.Locations.FindAsync(collaborateur.LocationId);
            collaborateur.Localisation = loc?.Name;
        }

        if (collaborateur.ContractTypeId.HasValue)
        {
            var ct = await _context.ContractTypes.FindAsync(collaborateur.ContractTypeId);
            collaborateur.TypeContrat = ct?.Name;
        }
    }

}
