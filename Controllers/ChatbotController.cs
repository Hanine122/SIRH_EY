using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Models;
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
            public string Message { get; set; } = "";
            public string? Page { get; set; }
            public string? ContextId { get; set; }
            public System.Text.Json.JsonElement? Context { get; set; }
            public System.Text.Json.JsonElement? SessionMemory { get; set; }
            public System.Text.Json.JsonElement? SessionHistory { get; set; }
        }

        public class ChatReply
        {
            public string? Reply { get; set; }
        }

        // ════════════════════════════════════════════════════════════
        // HELPER PARTAGÉ — même formule que la vue Razor ChoisirRemplacant
        // Utilisé par GetSuccessionData ET SimulateFormation pour garantir
        // que les deux donnent toujours le même score pour les mêmes données.
        // ════════════════════════════════════════════════════════════
        private static int CalculerScoreMatch(List<string> competencesRequises, IEnumerable<string> nomsCompetencesCandidat)
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            var nomsCandidat = nomsCompetencesCandidat
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .Distinct(comparer)
                .ToList();

            var communes = competencesRequises.Count(r => nomsCandidat.Any(n => comparer.Equals(n, r)));
            var manquantesCount = competencesRequises.Count - communes;
            var possedes = competencesRequises.Count > 0 ? (competencesRequises.Count - manquantesCount) : communes;

            return competencesRequises.Count == 0
                ? Math.Min(100, communes * 25)
                : (int)Math.Round(100.0 * Math.Max(0, possedes) / competencesRequises.Count);
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

        [AllowAnonymous]
        [HttpGet("simulate-departs")]
        public async Task<IActionResult> SimulateDeparts([FromQuery] string ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return BadRequest(new { error = "Paramètre ids requis, ex: ?ids=1,3,5" });

            var idsDepart = ids.Split(',')
                .Select(s => int.TryParse(s.Trim(), out var n) ? n : -1)
                .Where(n => n > 0)
                .ToList();

            if (!idsDepart.Any())
                return BadRequest(new { error = "Aucun ID valide fourni." });

            var partants = await _context.Collaborateurs
                .Where(c => idsDepart.Contains(c.Id))
                .ToListAsync();

            if (!partants.Any())
                return NotFound(new { error = "Aucun collaborateur trouvé pour ces IDs." });

            var poolRestant = await _context.Collaborateurs
                .Include(c => c.Competences)
                .Where(c => c.Actif && !idsDepart.Contains(c.Id))
                .ToListAsync();

            var departementsImpactes = partants
                .Select(p => p.Departement)
                .Distinct()
                .ToList();

            var impactParDepartement = new List<object>();

            foreach (var dept in departementsImpactes)
            {
                var partantsDept = partants.Where(p => p.Departement == dept).ToList();
                var restantsDept = poolRestant.Where(c => c.Departement == dept).ToList();

                var competencesRequisesDept = await _context.CompetencesRequisesParPoste
                    .Where(cr => partantsDept.Select(p => p.Poste).Contains(cr.Poste))
                    .Select(cr => cr.Competence)
                    .Distinct()
                    .ToListAsync();

                var competencesCouvertes = restantsDept
                    .SelectMany(c => c.Competences.Select(x => x.Nom))
                    .Distinct()
                    .ToList();

                var competencesPerdues = competencesRequisesDept
                    .Where(cr => !competencesCouvertes.Contains(cr, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                impactParDepartement.Add(new
                {
                    departement = dept,
                    collaborateursPartants = partantsDept.Select(p => $"{p.Prenom} {p.Nom}").ToList(),
                    effectifRestant = restantsDept.Count,
                    competencesRequises = competencesRequisesDept.Count,
                    competencesPerdues = competencesPerdues,
                    niveauAlerte = competencesPerdues.Count == 0 ? "Aucun risque"
                        : competencesPerdues.Count <= 2 ? "Risque modéré"
                        : "Risque critique"
                });
            }

            return Ok(new
            {
                scenario = $"Départ simultané de {partants.Count} collaborateur(s)",
                collaborateursAnalyses = partants.Select(p => new { id = p.Id, nom = $"{p.Prenom} {p.Nom}", departement = p.Departement, poste = p.Poste }),
                impactParDepartement
            });
        }

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
                    id = c.Id,
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
                    id = c.Id,
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
                    id = c.Id,
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
        public async Task<IActionResult> GetPromotables([FromQuery] string? dept = null)
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
            var partant = await _context.Collaborateurs
                .Include(c => c.Competences)
                .FirstOrDefaultAsync(c => c.Id == collaborateurId);

            if (partant == null)
                return NotFound(new { error = "Collaborateur non trouvé." });

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

            var requises = surProfil
                .Union(surPoste, comparer)
                .Distinct(comparer)
                .ToList();

            var autres = await _context.Collaborateurs
                .Include(c => c.Competences)
                .Where(c => c.Id != collaborateurId && c.Actif)
                .ToListAsync();

            var deptPartant = (partant.Departement ?? "").Trim();

            var candidats = autres.Select(autre => {
                var nomsAutre = autre.Competences?
                    .Where(c => !string.IsNullOrWhiteSpace(c.Nom))
                    .Select(c => c.Nom.Trim())
                    .Distinct(comparer)
                    .ToList() ?? new List<string>();

                var communes   = requises.Count(r => nomsAutre.Any(a => comparer.Equals(a, r)));
                var manquantes = requises.Where(r => !nomsAutre.Any(a => comparer.Equals(a, r))).ToList();

                var deptAutre = (autre.Departement ?? "").Trim();
                var autreDept = deptPartant.Length == 0 || deptAutre.Length == 0
                    ? !string.Equals(deptPartant, deptAutre, StringComparison.OrdinalIgnoreCase)
                    : !deptPartant.Equals(deptAutre, StringComparison.OrdinalIgnoreCase);

                var profilTransversal = autreDept && communes > 0;

                var possedes    = requises.Count > 0 ? (requises.Count - manquantes.Count) : communes;
                var scoreMatch  = requises.Count == 0
                    ? Math.Min(100, communes * 25)
                    : (int)Math.Round(100.0 * Math.Max(0, possedes) / requises.Count);

                return new
                {
                    id                    = autre.Id,
                    nom                   = $"{autre.Prenom} {autre.Nom}",
                    poste                 = autre.Poste ?? "",
                    grade                 = autre.Grade ?? "",
                    departement           = autre.Departement ?? "",
                    scoreMatch,
                    competencesCommunes   = communes,
                    competencesManquantes = manquantes,
                    profilTransversal
                };
            })
            .OrderByDescending(c => c.profilTransversal)
            .ThenByDescending(c => c.competencesCommunes)
            .ThenBy(c => c.competencesManquantes.Count)
            .Take(3)
            .ToList();

            return Ok(new
            {
                collaborateurNom    = $"{partant.Prenom} {partant.Nom}",
                poste               = partant.Poste,
                competencesRequises = requises,
                top3                = candidats
            });
        }

        [AllowAnonymous]
[HttpGet("simulate-departs-by-name")]
public async Task<IActionResult> SimulateDepartsByName([FromQuery] string noms)
{
    if (string.IsNullOrWhiteSpace(noms))
        return BadRequest(new { error = "Paramètre noms requis, ex: ?noms=Karim,Sami" });

    var listeNoms = noms.Split(',').Select(n => n.Trim()).Where(n => n.Length > 0).ToList();

    var idsResolus = new List<int>();
    var nomsIntrouvables = new List<string>();

    foreach (var nom in listeNoms)
    {
        var collaborateur = await _context.Collaborateurs
            .FirstOrDefaultAsync(c => (c.Prenom + " " + c.Nom).ToLower().Contains(nom.ToLower()));

        if (collaborateur != null)
            idsResolus.Add(collaborateur.Id);
        else
            nomsIntrouvables.Add(nom);
    }

    if (!idsResolus.Any())
        return NotFound(new { error = "Aucun des collaborateurs mentionnés n'a été trouvé.", nomsIntrouvables });

    // Réutilise directement la logique de SimulateDeparts existante
    var idsString = string.Join(",", idsResolus);
    var resultat = await SimulateDeparts(idsString);

    if (resultat is OkObjectResult ok && nomsIntrouvables.Any())
    {
        // Injecter un avertissement si certains noms n'ont pas été résolus
        var json = JsonSerializer.Serialize(ok.Value);
        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        dict["nomsIntrouvables"] = nomsIntrouvables;
        return Ok(dict);
    }

    return resultat;
}

        [AllowAnonymous]
        [HttpGet("simulate-formation")]
        public async Task<IActionResult> SimulateFormation(
            int collaborateurId,
            string posteCible,
            string competence,
            int niveauSimule)
        {
            var candidat = await _context.Collaborateurs
                .Include(c => c.Competences)
                .FirstOrDefaultAsync(c => c.Id == collaborateurId);

            if (candidat == null)
                return NotFound(new { error = "Collaborateur introuvable." });

           var competencesRequisesRaw = await _context.CompetencesRequisesParPoste
    .Where(cr => cr.Poste == posteCible)
    .Select(cr => cr.Competence.Trim())
    .ToListAsync();

var competencesRequises = competencesRequisesRaw
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToList();

            if (!competencesRequises.Any())
                return NotFound(new { error = $"Aucune compétence requise définie pour le poste '{posteCible}'." });

            var nomsActuels = candidat.Competences
                .Where(c => !string.IsNullOrWhiteSpace(c.Nom))
                .Select(c => c.Nom.Trim())
                .ToList();

            // Score AVANT — compétences réelles actuelles
            var scoreAvant = CalculerScoreMatch(competencesRequises, nomsActuels);

            // Score APRÈS — on ajoute hypothétiquement la compétence simulée
            // (niveauSimule n'affecte pas le calcul de couverture binaire ici,
            //  il sert uniquement d'indicateur informatif sur le niveau atteint)
            var nomsSimules = nomsActuels
                .Where(n => !n.Equals(competence, StringComparison.OrdinalIgnoreCase))
                .ToList();
            nomsSimules.Add(competence.Trim());

            var scoreApres = CalculerScoreMatch(competencesRequises, nomsSimules);
            var gain = scoreApres - scoreAvant;

            return Ok(new
            {
                collaborateurNom = $"{candidat.Prenom} {candidat.Nom}",
                posteCible,
                competenceSimulee = competence,
                niveauSimule,
                scoreAvant,
                scoreApres,
                gain,
                impact = gain >= 20 ? "Impact significatif — accélère fortement la succession"
                    : gain >= 5 ? "Impact modéré"
                    : gain <= 0 ? "Aucun impact — compétence déjà couverte ou non requise"
                    : "Impact limité — d'autres compétences manquent davantage",
                recommandation = gain >= 20
                    ? $"Prioriser cette formation : elle seule rapproche {candidat.Prenom} du seuil de succession."
                    : "Considérer cette formation en complément d'autres axes de développement."
            });
        }

        [AllowAnonymous]
        [HttpGet("evolution/{collaborateurId}")]
        public async Task<IActionResult> GetEvolution(int collaborateurId)
        {
            var collaborateur = await _context.Collaborateurs.FindAsync(collaborateurId);
            if (collaborateur == null) return NotFound(new { error = "Collaborateur introuvable." });

            var historique = await _context.EvaluationsHistoriques
                .Include(h => h.Competence)
                .Where(h => h.Competence!.CollaborateurId == collaborateurId)
                .OrderBy(h => h.DateChangement)
                .ToListAsync();

            if (!historique.Any())
                return Ok(new {
                    collaborateurNom = $"{collaborateur.Prenom} {collaborateur.Nom}",
                    message = "Aucun historique disponible pour ce collaborateur.",
                    timeline = new List<object>()
                });

            var timeline = historique
                .GroupBy(h => h.DateChangement.ToString("yyyy-MM"))
                .Select(g => new
                {
                    periode = g.Key,
                    evolutions = g.Select(h => new
                    {
                        competence = h.Competence?.Nom,
                        avant = h.NiveauAncien,
                        apres = h.NiveauNouveau,
                        raison = h.Raison
                    })
                })
                .ToList();

            var tendance = historique.Last().NiveauNouveau - historique.First().NiveauAncien;

            return Ok(new
            {
                collaborateurNom = $"{collaborateur.Prenom} {collaborateur.Nom}",
                periodeAnalysee = $"{historique.First().DateChangement:MMM yyyy} → {historique.Last().DateChangement:MMM yyyy}",
                tendance = tendance > 0 ? $"Progression de +{tendance} niveau(x)" : "Stable",
                timeline
            });
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("Message cannot be empty.");

            const string webhookUrl = "http://localhost:5678/webhook/hr-copilot";

            try
            {
                var client = _httpClientFactory.CreateClient();

                var payload = JsonSerializer.Serialize(new
                {
                    message        = request.Message,
                    page           = request.Page ?? "general",
                    contextId      = request.ContextId,
                    sessionMemory  = request.SessionMemory,
                    sessionHistory = request.SessionHistory,
                    context        = request.SessionMemory   // bridge : n8n lit body.context
                });

                var response = await client.PostAsync(
                    webhookUrl,
                    new StringContent(payload, Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, BuildFallback("Erreur de connexion avec le service IA."));

                var body = await response.Content.ReadAsStringAsync();

                try
                {
                    JsonSerializer.Deserialize<JsonElement>(body);
                    return Content(body, "application/json", Encoding.UTF8);
                }
                catch
                {
                    return Ok(BuildFallback(body));
                }
            }
            catch
            {
                return StatusCode(500, BuildFallback("Service IA temporairement indisponible. Veuillez réessayer dans quelques instants."));
            }
        }

        private static object BuildFallback(string message) => new
        {
            answer           = message,
            analysis         = (string?)null,
            reasoning        = Array.Empty<string>(),
            actions          = Array.Empty<string>(),
            suggestions      = new[] { "Quels sont les hauts potentiels ?", "Qui est prêt pour une promotion ?", "Répartition des effectifs ?" },
            sources          = Array.Empty<object>(),
            cards            = Array.Empty<object>(),
            executionHistory = Array.Empty<object>(),
            context          = new { }
        };
    }
}