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

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message cannot be empty.");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var webhookUrl = "http://localhost:5678/webhook/hr-chatbot";

                var jsonContent = JsonSerializer.Serialize(new { message = request.Message });
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(webhookUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    // Attempt to parse the response as JSON. If the n8n webhook returns {"reply": "..."}
                    // we can just forward the JSON.
                    
                    try 
                    {
                        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseString);
                        // Check if the reply field exists
                        if (jsonResponse.TryGetProperty("reply", out var replyElement))
                        {
                            return Ok(new { reply = replyElement.GetString() });
                        }
                        return Ok(new { reply = responseString }); // Fallback if no specific "reply" field
                    }
                    catch (JsonException)
                    {
                        // Fallback if the response is not JSON
                        return Ok(new { reply = responseString });
                    }
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { reply = "Erreur de connexion avec l'assistant IA." });
                }
            }
            catch (System.Exception ex)
            {
                // In production, log the exception.
                return StatusCode(500, new { reply = "Le service est temporairement indisponible." });
            }
        }
    }
}
