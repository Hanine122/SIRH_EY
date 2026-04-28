using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;
// using Rotativa.AspNetCore;

namespace SIRH.EY.Controllers;

public class CertificatsController : Controller
{
    private readonly ApplicationDbContext _context;

    public CertificatsController(ApplicationDbContext context)
    {
        _context = context;
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
        if (collaborateurId == null)
        {
            return View(new List<Inscription>());
        }
        var inscriptions = await _context.Inscriptions
            .Include(i => i.Formation)
            .Where(i => i.CollaborateurId == collaborateurId && i.Terminee)
            .ToListAsync();
        var collaborateur = await _context.Collaborateurs.FindAsync(collaborateurId);
        ViewBag.CollaborateurNom = collaborateur?.Prenom + " " + collaborateur?.Nom;
        return View(inscriptions);
    }

    
}