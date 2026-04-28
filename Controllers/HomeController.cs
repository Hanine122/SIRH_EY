using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Services;
using SIRH.EY.Models;

namespace SIRH.EY.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

public async Task<IActionResult> Index()
{
    // KPI
    var nbActifs = await _context.Collaborateurs.CountAsync(c => c.Actif);
    var nbInscriptions = await _context.Inscriptions.CountAsync();
    var nbTerminees = await _context.Inscriptions.CountAsync(i => i.Terminee);
    var taux = nbInscriptions == 0 ? 0 : (nbTerminees * 100 / nbInscriptions);

    ViewBag.NbCollaborateursActifs = nbActifs;
    ViewBag.NbFormationsSuivies = nbInscriptions;
    ViewBag.TauxCompletion = taux;

    // Collaborateurs actifs récents (les 4 derniers)
    var collaborateurs = await _context.Collaborateurs
        .Where(c => c.Actif)
        .OrderByDescending(c => c.Id)
        .Take(4)
        .Select(c => new { c.Prenom, c.Nom, c.Poste })
        .ToListAsync();
    ViewBag.CollaborateursRecents = collaborateurs;

    // Formations en cours (inscriptions non terminées)
    var inscriptions = await _context.Inscriptions
        .Include(i => i.Formation)
        .Where(i => !i.Terminee)
        .ToListAsync();

    var formationsEnCours = new List<dynamic>();
    foreach (var insc in inscriptions)
    {
        var f = insc.Formation;
        if (f == null) continue;

        int progression = 0;
        int joursRestants = 0;

        if (f.DateDebut <= DateTime.Now && !insc.Terminee)
        {
            var dureeJours = Math.Max(1, f.DureeHeures / 8);
            var joursPasses = (DateTime.Now - f.DateDebut).Days;
            progression = (int)Math.Min(100, (double)joursPasses / dureeJours * 100);
            var dateFin = f.DateDebut.AddDays(dureeJours);
            joursRestants = (int)Math.Max(0, (dateFin - DateTime.Now).Days);
        }
        else if (f.DateDebut > DateTime.Now)
        {
            joursRestants = (int)(f.DateDebut - DateTime.Now).Days;
            progression = 0;
        }

        formationsEnCours.Add(new { Titre = f.Titre, Progression = progression, JoursRestants = joursRestants });
    }
    // Éviter les doublons de titres (si plusieurs inscrits à la même formation)
    ViewBag.FormationsEnCours = formationsEnCours.GroupBy(f => f.Titre).Select(g => g.First()).ToList();

    // Nouvelles formations obligatoires (ex: créées ce mois-ci)
    var nouvelles = await _context.Formations
        .CountAsync(f => f.DateDebut.Month == DateTime.Now.Month && f.DateDebut.Year == DateTime.Now.Year);
    ViewBag.NouvellesFormations = nouvelles;

    return View();
}

 public async Task<IActionResult> TestDataverse()
{
    try
    {
        var service = HttpContext.RequestServices.GetRequiredService<IDataverseService>();
        var collaborateurs = await service.GetCollaborateursAsync();
        return Ok(new { count = collaborateurs.Count, data = collaborateurs.Select(e => e.Attributes) });
    }
    catch (Exception ex)
    {
        return BadRequest(new { error = ex.Message });
    }
}
public IActionResult PortailModerne()
{
    return View();
}

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}