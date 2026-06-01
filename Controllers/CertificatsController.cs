using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Authorization;
using SIRH.EY.Data;
using SIRH.EY.Models;
using SIRH.EY.Services;
// using Rotativa.AspNetCore;

namespace SIRH.EY.Controllers;

public class CertificatsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ITeamAccessService _teamAccess;

    public CertificatsController(ApplicationDbContext context, ITeamAccessService teamAccess)
    {
        _context = context;
        _teamAccess = teamAccess;
    }
    [HttpGet]
// public async Task<IActionResult> DownloadCertificat(int inscriptionId)
// {
//     var inscription = await _context.Inscriptions
//         .Include(i => i.Formation)
//         .Include(i => i.Collaborateur)
//         .FirstOrDefaultAsync(i => i.Id == inscriptionId);
//     if (inscription == null) return NotFound();

//     // Préparer le modèle pour la vue PDF
//     var model = new CertificatPdfModel
//     {
//         CollaborateurNom = inscription.Collaborateur.Prenom + " " + inscription.Collaborateur.Nom,
//         FormationTitre = inscription.Formation.Titre,
//         DateObtention = inscription.DateInscription,
//         Duree = inscription.Formation.DureeHeures,
//         Organisme = inscription.Formation.Organisme ?? "EY Academy"
//     };

//     // Générer le PDF à partir d'une vue partielle
//     return new ViewAsPdf("CertificatPdf", model)
//     {
//         FileName = $"Certificat_{inscription.Collaborateur.Nom}_{inscription.Formation.Titre}.pdf",
//         PageSize = Rotativa.AspNetCore.Options.Size.A4,
//         PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait
//     };
// }

    public async Task<IActionResult> Index(int? collaborateurId)
    {
        // Default to current user's own profile
        if (collaborateurId == null)
            collaborateurId = await _teamAccess.GetCurrentCollaborateurIdAsync(User);

        if (collaborateurId == null)
            return View(new List<Inscription>());

        // IDOR guard: verify the caller may access this collaborateur's certificates
        if (!await _teamAccess.CanAccessCollaborateurAsync(User, collaborateurId.Value))
            return Forbid();

        var inscriptions = await _context.Inscriptions
            .Include(i => i.Formation)
            .Where(i => i.CollaborateurId == collaborateurId && i.Terminee)
            .ToListAsync();

        var collaborateur = await _context.Collaborateurs.FindAsync(collaborateurId);
        ViewBag.CollaborateurNom = collaborateur?.Prenom + " " + collaborateur?.Nom;
        return View(inscriptions);
    }

    
}