using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SIRH.EY.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _context;

        public ChatbotController(IHttpClientFactory httpClientFactory, ApplicationDbContext context)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
        }

        public class ChatRequest
        {
            public string Message { get; set; }
        }

        public class ChatReply
        {
            public string Reply { get; set; }
        }


[AllowAnonymous]
[HttpGet("hr-talent")]
public async Task<IActionResult> GetHighPotentials()
{
    var collaborateurs = await _context.Collaborateurs
        .Include(c => c.Competences)
        .Include(c => c.Inscriptions)
        .Where(c => c.Actif)
        .ToListAsync();

    var highPotentials = collaborateurs
        .Select(c => new
        {
            Nom = $"{c.Prenom} {c.Nom}",
            Grade = c.Grade,
            Departement = c.Departement,
            MoyenneCompetences = c.Competences.Any()
                ? Math.Round(c.Competences.Average(x => x.NiveauActuel), 1)
                : 0
        })
        .Where(x => x.MoyenneCompetences >= 4)
        .OrderByDescending(x => x.MoyenneCompetences)
        .Take(10)
        .ToList();

    return Ok(highPotentials);
}

        // Called by n8n server-side — no browser session available.
        [AllowAnonymous]
        [HttpGet("stats")]
        public async Task<IActionResult> GetRhStats()
        {
            var collaborateursActifs = await _context.Collaborateurs
                .CountAsync(c => c.Actif);

            var formationsEnCours = await _context.Inscriptions
                .CountAsync(i => !i.Terminee);

            var totalInscriptions = await _context.Inscriptions.CountAsync();
            var terminees         = await _context.Inscriptions.CountAsync(i => i.Terminee);
            var tauxCompletion    = totalInscriptions > 0
                ? Math.Round(terminees * 100.0 / totalInscriptions, 1)
                : 0.0;

            var repartitionDept = await _context.Collaborateurs
                .Where(c => c.Actif && c.Departement != null)
                .GroupBy(c => c.Departement)
                .Select(g => new { departement = g.Key, total = g.Count() })
                .OrderByDescending(x => x.total)
                .ToListAsync();

            var topCompetences = await _context.Competences
                .Where(c => c.Collaborateur != null && c.Collaborateur.Actif)
                .GroupBy(c => c.Nom)
                .Select(g => new
                {
                    nom              = g.Key,
                    nbCollaborateurs = g.Count(),
                    niveauMoyen      = Math.Round(g.Average(c => (double)c.NiveauActuel), 1)
                })
                .OrderByDescending(x => x.nbCollaborateurs)
                .Take(5)
                .ToListAsync();

            return Ok(new
            {
                collaborateursActifs,
                formationsEnCours,
                totalInscriptions,
                terminees,
                tauxCompletion,
                repartitionDept,
                topCompetences
            });
        }

[AllowAnonymous]
[HttpGet("ai/talent-summary")]
public async Task<IActionResult> GetTalentSummary()
{
    var collaborateurs = await _context.Collaborateurs
        .Include(c => c.Competences)
        .Include(c => c.Inscriptions)
        .Where(c => c.Actif)
        .ToListAsync();

    var topTalents = collaborateurs
        .Select(c => new
        {
            Nom = $"{c.Prenom} {c.Nom}",
            Grade = c.Grade,
            Departement = c.Departement,
            Score = c.Competences.Any()
                ? Math.Round(c.Competences.Average(x => x.NiveauActuel), 1)
                : 0
        })
        .Where(x => x.Score >= 4)
        .OrderByDescending(x => x.Score)
        .Take(10)
        .ToList();

    var atRisk = collaborateurs
        .Select(c => new
        {
            Nom = $"{c.Prenom} {c.Nom}",
            Score = c.Competences.Any()
                ? Math.Round(c.Competences.Average(x => x.NiveauActuel), 1)
                : 0
        })
        .Where(x => x.Score > 0 && x.Score < 2)
        .ToList();

    var successionReady = collaborateurs.Count(c =>
        c.Grade == "Senior" ||
        c.Grade == "Manager");

    var departmentDistribution = collaborateurs
        .GroupBy(c => c.Departement)
        .Select(g => new
        {
            Departement = g.Key,
            Total = g.Count()
        })
        .OrderByDescending(x => x.Total)
        .ToList();

    return Ok(new
    {
        totalCollaborateurs = collaborateurs.Count,
        successionReady,
        topTalents,
        atRisk,
        departmentDistribution
    });
}




       [HttpPost("ask")]
public async Task<IActionResult> Ask([FromBody] ChatRequest request)
{
    if (request == null || string.IsNullOrWhiteSpace(request.Message))
        return BadRequest("Message cannot be empty.");

    try
    {
        var client = _httpClientFactory.CreateClient();
        
        // Détection d'intent côté MVC
        var msg = request.Message.ToLower();
        
        var isTalent =
            msg.Contains("haut potentiel") ||
            msg.Contains("hauts potentiels") ||
            msg.Contains("top talent") ||
            msg.Contains("talent") ||
            msg.Contains("potentiel") ||
            msg.Contains("succession") ||
            msg.Contains("risque") ||
            msg.Contains("promotion") ||
            msg.Contains("meilleur") ||
            msg.Contains("qui peut");

        var webhookUrl = isTalent
            ? "http://localhost:5678/webhook/hr-talent"
            : "http://localhost:5678/webhook/hr-stats";

        var jsonContent = JsonSerializer.Serialize(new { message = request.Message });
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(webhookUrl, content);

        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            try
            {
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseString);
                if (jsonResponse.TryGetProperty("reply", out var replyElement))
                    return Ok(new { reply = replyElement.GetString() });
                return Ok(new { reply = responseString });
            }
            catch (JsonException)
            {
                return Ok(new { reply = responseString });
            }
        }
        else
        {
            return StatusCode((int)response.StatusCode, 
                new { reply = "Erreur de connexion avec l'assistant IA." });
        }
    }
    catch (Exception ex)
    {
        return StatusCode(500, 
            new { reply = "Le service est temporairement indisponible." });
    }
}
    }
}
