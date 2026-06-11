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
    public string? Page { get; set; }
    public string? ContextId { get; set; }
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


[AllowAnonymous]
[HttpGet("hr-copilot-data")]
public async Task<IActionResult> GetHrCopilotData()
{
    var collaborateurs = await _context.Collaborateurs
        .Include(c => c.Competences)
        .Where(c => c.Actif)
        .ToListAsync();

    var topTalents = collaborateurs
        .Select(c => new
        {
            Nom = $"{c.Prenom} {c.Nom}",
            Poste = c.Poste,
            Grade = c.Grade,
            Score = c.Competences.Any()
                ? Math.Round(c.Competences.Average(x => x.NiveauActuel), 1)
                : 0
        })
        .Where(x => x.Score >= 4)
        .OrderByDescending(x => x.Score)
        .Take(10)
        .ToList();

    var promotionReady = topTalents.Take(5).ToList();

    return Ok(new
    {
        totalTalents = collaborateurs.Count,
        topTalents,
        promotionReady,
        atRisk = new List<object>()
    });
}
[AllowAnonymous]
[HttpGet("promotables")]
public async Task<IActionResult> GetPromotables([FromQuery] string dept = null)
{
    var collaborateurs = await _context.Collaborateurs
        .Include(c => c.Competences)
        .Where(c => c.Actif)
        .ToListAsync();

    var query = collaborateurs
        .Select(c => new
        {
            id          = c.Id,
            nom         = $"{c.Prenom} {c.Nom}",
            poste       = c.Poste,
            grade       = c.Grade,
            departement = c.Departement,
            score       = c.Competences.Any()
                ? Math.Round(c.Competences.Average(x => (double)x.NiveauActuel), 1)
                : 0
        })
        .Where(x => x.score >= 4);

    if (!string.IsNullOrWhiteSpace(dept))
        query = query.Where(x =>
            x.departement != null &&
            x.departement.ToLower().Contains(dept.ToLower()));

    var result = query
        .OrderByDescending(x => x.score)
        .Take(10)
        .ToList();

    return Ok(new { total = result.Count, collaborateurs = result });
}

[AllowAnonymous]
[HttpGet("postes-sans-successeur")]
public async Task<IActionResult> GetPostesSansSuccesseur()
{
    var collaborateurs = await _context.Collaborateurs
        .Include(c => c.Competences)
        .Where(c => c.Actif)
        .ToListAsync();

    var result = new List<object>();

    foreach (var c in collaborateurs)
    {
        var competencesRequises = c.Competences.Select(x => x.Nom).ToList();
        if (!competencesRequises.Any()) continue;

        var candidats = collaborateurs
            .Where(x => x.Id != c.Id)
            .Select(x => {
                var communes = x.Competences
                    .Select(y => y.Nom)
                    .Intersect(competencesRequises)
                    .Count();
                return new { communes, score = Math.Round(communes * 100.0 / competencesRequises.Count, 0) };
            })
            .Where(x => x.score >= 50)
            .ToList();

        if (!candidats.Any())
        {
            result.Add(new
            {
                id          = c.Id,
                nom         = $"{c.Prenom} {c.Nom}",
                poste       = c.Poste,
                grade       = c.Grade,
                departement = c.Departement,
                nbCompetencesRequises = competencesRequises.Count
            });
        }
    }

    return Ok(new
    {
        total          = result.Count,
        collaborateurs = result
                            .OrderBy(x => ((dynamic)x).grade)
                            .Take(10)
                            .ToList()
    });
}
[AllowAnonymous]
[HttpGet("collaborateur/{id}")]
public async Task<IActionResult> GetCollaborateur(int id)
{
    var c = await _context.Collaborateurs
        .Include(x => x.Competences)
        .FirstOrDefaultAsync(x => x.Id == id);

    if (c == null) return NotFound(new { error = "Collaborateur non trouvé." });

    return Ok(new
    {
        id          = c.Id,
        nom         = $"{c.Prenom} {c.Nom}",
        poste       = c.Poste,
        grade       = c.Grade,
        departement = c.Departement,
        competences = c.Competences.Select(x => new { x.Nom, x.NiveauActuel })
    });
}


[AllowAnonymous]
[HttpGet("find")]
public async Task<IActionResult> FindCollaborateur(string nom)
{
    var collaborateur = await _context.Collaborateurs
        .FirstOrDefaultAsync(c =>
            (c.Prenom + " " + c.Nom)
            .ToLower()
            .Contains(nom.ToLower()));

    if (collaborateur == null)
        return NotFound();

    return Ok(new
    {
        id = collaborateur.Id,
        nom = collaborateur.Prenom + " " + collaborateur.Nom
    });
}

[AllowAnonymous]
[HttpGet("postes-a-risque")]
public async Task<IActionResult> GetPostesARisque()
{
    var collaborateurs = await _context.Collaborateurs
        .Include(c => c.Competences)
        .Where(c => c.Actif)
        .ToListAsync();

    var seuilsParGrade = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
    {
        { "Junior",         2.0 },
        { "Senior",         3.0 },
        { "Manager",        3.5 },
        { "Senior Manager", 4.0 },
        { "Director",       4.0 },
        { "Partner",        4.5 }
    };

    var postesARisque = collaborateurs
        .Select(c => {
            var scoreActuel = c.Competences.Any()
                ? Math.Round(c.Competences.Average(x => (double)x.NiveauActuel), 1)
                : 0;

            var grade = c.Grade ?? "Junior";
            var seuilAttendu = seuilsParGrade.ContainsKey(grade)
                ? seuilsParGrade[grade]
                : 3.0;

            // DateEmbauche est DateTime non nullable
            var anciennete = Math.Round(
                (DateTime.Now - c.DateEmbauche).TotalDays / 365.25, 1);

            var niveauRisque =
                scoreActuel < seuilAttendu && anciennete > 2 ? "Élevé" :
                scoreActuel < seuilAttendu                   ? "Moyen" :
                anciennete < 1                               ? "Faible" : null;

            return new
            {
                id           = c.Id,
                nom          = $"{c.Prenom} {c.Nom}",
                poste        = c.Poste,
                grade        = grade,
                departement  = c.Departement,
                scoreActuel,
                seuilAttendu,
                anciennete,
                niveauRisque,
                ecart        = Math.Round(seuilAttendu - scoreActuel, 1)
            };
        })
        .Where(x => x.niveauRisque != null)
        .OrderByDescending(x =>
            x.niveauRisque == "Élevé" ? 2 :
            x.niveauRisque == "Moyen" ? 1 : 0)
        .ThenByDescending(x => x.ecart)
        .Take(10)
        .ToList();

    return Ok(new
    {
        total          = postesARisque.Count,
        collaborateurs = postesARisque
    });
}


[AllowAnonymous]
[HttpGet("succession/{collaborateurId}")]
public async Task<IActionResult> GetSuccessionData(int collaborateurId)
{
    var collaborateur = await _context.Collaborateurs
        .Include(c => c.Competences)
        .FirstOrDefaultAsync(c => c.Id == collaborateurId);

    if (collaborateur == null)
        return NotFound(new { error = "Collaborateur non trouvé." });

    var competencesRequises = collaborateur.Competences
        .Select(c => c.Nom)
        .ToList();

    var candidats = await _context.Collaborateurs
        .Include(c => c.Competences)
        .Where(c => c.Actif && c.Id != collaborateurId)
        .ToListAsync();

    var top3 = candidats
        .Select(c => {
            var communes = c.Competences
                .Select(x => x.Nom)
                .Intersect(competencesRequises)
                .Count();
            var manquantes = competencesRequises
                .Except(c.Competences.Select(x => x.Nom))
                .ToList();
            var score = competencesRequises.Count > 0
                ? Math.Round(communes * 100.0 / competencesRequises.Count, 0)
                : 0;
            return new {
                nom            = $"{c.Prenom} {c.Nom}",
                poste          = c.Poste,
                grade          = c.Grade,
                departement    = c.Departement,
                scoreMatch     = score,
                competencesCommunes  = communes,
                competencesManquantes = manquantes
            };
        })
        .Where(x => x.scoreMatch > 0)
        .OrderByDescending(x => x.scoreMatch)
        .Take(3)
        .ToList();

    return Ok(new {
        collaborateurNom = $"{collaborateur.Prenom} {collaborateur.Nom}",
        poste            = collaborateur.Poste,
        competencesRequises,
        top3
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

       var msg  = request.Message.ToLower();
var page = request.Page?.ToLower() ?? "general";

var isCopilot =
    msg.Contains("haut potentiel")  || msg.Contains("hauts potentiels") ||
    msg.Contains("top talent")      || msg.Contains("top talents") ||
    msg.Contains("high potential")  || msg.Contains("hipo") ||
    msg.Contains("9 box")           || msg.Contains("nine box") ||
    msg.Contains("fort potentiel")  || msg.Contains("talent prêt") ||
    msg.Contains("promotion")       || msg.Contains("promouv") ||
    msg.Contains("mérite")          || msg.Contains("évolution") ||
    msg.Contains("évoluer")         || msg.Contains("carrière") ||
    msg.Contains("remplacer")       || msg.Contains("remplaçant") ||
    msg.Contains("succession")      || msg.Contains("successeur") ||
    msg.Contains("risque")          || msg.Contains("poste critique") ||
    msg.Contains("sans successeur") || msg.Contains("poste vacant") ||
    msg.Contains("profil")          || msg.Contains("voir") ||
    msg.Contains("départ")          || msg.Contains("développement");

var isTalent =
    msg.Contains("talent") ||
    msg.Contains("potentiel") ||
    msg.Contains("meilleur");

string webhookUrl;

if (page == "succession" || isCopilot)
    webhookUrl = "http://localhost:5678/webhook/hr-copilot";
else if (isTalent)
    webhookUrl = "http://localhost:5678/webhook/hr-talent";
else
    webhookUrl = "http://localhost:5678/webhook/hr-stats";

        var jsonContent = JsonSerializer.Serialize(new
        {
            message = request.Message
        });

        var content = new StringContent(
            jsonContent,
            Encoding.UTF8,
            "application/json"
        );

        var response = await client.PostAsync(webhookUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, new
            {
                answer = "Erreur de connexion.",
                analysis = "",
                actions = new List<string>(),
                suggestions = new List<string>()
            });
        }

        var responseString = await response.Content.ReadAsStringAsync();

        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(responseString);

            var answer = json.TryGetProperty("answer", out var a)
                ? a.GetString()
                : responseString;

            var analysis = json.TryGetProperty("analysis", out var an)
                ? an.GetString()
                : "";

            var actions = json.TryGetProperty("actions", out var ac)
                ? ac.EnumerateArray()
                    .Select(x => x.GetString()!)
                    .ToList()
                : new List<string>();

            var suggestions = json.TryGetProperty("suggestions", out var s)
                ? s.EnumerateArray()
                    .Select(x => x.GetString()!)
                    .ToList()
                : new List<string>();

            return Ok(new
            {
                answer,
                analysis,
                actions,
                suggestions
            });
        }
        catch
        {
            return Ok(new
            {
                answer = responseString,
                analysis = "",
                actions = new List<string>(),
                suggestions = new List<string>()
            });
        }
    }
    catch
    {
        return StatusCode(500, new
        {
            answer = "Service temporairement indisponible.",
            analysis = "",
            actions = new List<string>(),
            suggestions = new List<string>()
        });
    }
}
    }
}
