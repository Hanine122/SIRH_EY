using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIRH.EY.Authorization;
using SIRH.EY.Data;
using SIRH.EY.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SIRH.EY.Services;
using SIRH.EY.Services.PowerAutomate;

namespace SIRH.EY.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _context;
        private readonly IPowerAutomateService _powerAutomate;
        private readonly ITeamAccessService _teamAccess;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(
            IHttpClientFactory httpClientFactory,
            ApplicationDbContext context,
            IPowerAutomateService powerAutomate,
            ITeamAccessService teamAccess,
            ILogger<ChatbotController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
            _powerAutomate = powerAutomate;
            _teamAccess = teamAccess;
            _logger = logger;
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

            var talentEvals = await _context.TalentEvaluations
                .Where(t => t.Actif)
                .ToListAsync();

            var gradeRefs = await _context.GradeReferentiels.ToListAsync();

            var query = collaborateurs
                .Select(c =>
                {
                    var eval       = talentEvals.FirstOrDefault(t => t.CollaborateurId == c.Id);
                    var gradeRef   = gradeRefs.FirstOrDefault(g => string.Equals(g.Grade, c.Grade, StringComparison.OrdinalIgnoreCase));
                    var gradeNext  = gradeRef?.GradeSuivant != null
                        ? gradeRefs.FirstOrDefault(g => string.Equals(g.Grade, gradeRef.GradeSuivant, StringComparison.OrdinalIgnoreCase))
                        : null;
                    var anciennete = (DateTime.Today - c.DateEmbauche).TotalDays / 365.25;

                    // 40 % compétences — NiveauPreparationSuccession (0-100) ou moyenne normalisée
                    var scoreComp = c.NiveauPreparationSuccession.HasValue
                        ? (double)c.NiveauPreparationSuccession.Value
                        : (c.Competences?.Any() == true
                            ? Math.Min(100.0, c.Competences.Average(x => (double)x.NiveauActuel) / 5.0 * 100)
                            : 50.0);

                    // 25 % performance — TalentEvaluation (1-5 → 20-100), neutre 50 si absent
                    var scorePerf = eval != null ? eval.PerformanceScore * 20.0 : 50.0;

                    // 20 % potentiel — TalentEvaluation ou PotentielCarriere string corrigé
                    var scorePot = eval != null
                        ? eval.PotentielScore * 20.0
                        : (c.PotentielCarriere?.Trim().ToLowerInvariant()) switch
                        {
                            "high"   => 100.0,
                            "medium" => 60.0,
                            "low"    => 20.0,
                            _        => 40.0
                        };

                    // 15 % ancienneté — relative au seuil du grade suivant
                    var scoreAnc = gradeNext is { AncienneteMinAns: > 0 }
                        ? Math.Min(100.0, anciennete * 100.0 / gradeNext.AncienneteMinAns)
                        : Math.Min(100.0, anciennete * 25.0);

                    var scorePromotion = Math.Round(0.40 * scoreComp + 0.25 * scorePerf + 0.20 * scorePot + 0.15 * scoreAnc, 1);

                    return new
                    {
                        id             = c.Id,
                        nom            = $"{c.Prenom} {c.Nom}",
                        poste          = c.Poste,
                        gradeActuel    = c.Grade,
                        gradeCible     = gradeRef?.GradeSuivant,
                        departement    = c.Departement,
                        scorePromotion,
                        ancienneteAns  = Math.Round(anciennete, 1)
                    };
                })
                .Where(x => x.scorePromotion >= 60);

            if (!string.IsNullOrWhiteSpace(dept))
                query = query.Where(x =>
                    x.departement != null &&
                    x.departement.Contains(dept, StringComparison.OrdinalIgnoreCase));

            var result = query
                .OrderByDescending(x => x.scorePromotion)
                .Take(10)
                .ToList();

            return Ok(new { total = result.Count, collaborateurs = result });
        }

        // ── Score promotion individuel — JSON compatible n8n ─────────────────────
        [AllowAnonymous]
        [HttpGet("promotion/{id:int}")]
        public async Task<IActionResult> GetPromotion(int id, [FromQuery] string? gradeCible = null)
        {
            var collaborateur = await _context.Collaborateurs
                .Include(c => c.Competences)
                .FirstOrDefaultAsync(c => c.Id == id && c.Actif);

            if (collaborateur == null)
                return NotFound(new { error = "Collaborateur introuvable." });

            var gradeRefs   = await _context.GradeReferentiels.ToListAsync();
            var gradeActuel = collaborateur.Grade ?? "Junior";

            var gradeActuelRef = gradeRefs.FirstOrDefault(g =>
                string.Equals(g.Grade, gradeActuel, StringComparison.OrdinalIgnoreCase));

            var gradeCibleNom = !string.IsNullOrWhiteSpace(gradeCible)
                ? gradeCible.Trim()
                : gradeActuelRef?.GradeSuivant ?? "Senior";

            var gradeCibleRef = gradeRefs.FirstOrDefault(g =>
                string.Equals(g.Grade, gradeCibleNom, StringComparison.OrdinalIgnoreCase));

            // Compétences requises : d'abord par poste actuel, sinon par grade cible générique
            var poste = collaborateur.Poste ?? gradeActuel;
            var competencesRequises = await _context.CompetencesRequisesParPoste
                .Where(c => string.Equals(c.Poste, poste, StringComparison.Ordinal))
                .ToListAsync();

            if (!competencesRequises.Any())
                competencesRequises = await _context.CompetencesRequisesParPoste
                    .Where(c => string.Equals(c.Poste, gradeCibleNom, StringComparison.OrdinalIgnoreCase))
                    .ToListAsync();

            var currentSkills = (collaborateur.Competences ?? [])
                .Where(c => !string.IsNullOrWhiteSpace(c.Nom))
                .ToDictionary(c => c.Nom.Trim(), c => c.NiveauActuel, StringComparer.OrdinalIgnoreCase);

            // Acquises = NiveauActuel >= NiveauRequis × 0.6 (seuil SuccessionEngine)
            var acquises   = new List<string>();
            var manquantes = new List<string>();
            foreach (var req in competencesRequises)
            {
                if (currentSkills.TryGetValue(req.Competence, out var niv) && niv >= (int)(req.NiveauRequis * 0.6))
                    acquises.Add(req.Competence);
                else
                    manquantes.Add(req.Competence);
            }

            // TalentEvaluation — source principale de performance et potentiel
            var talentEval = await _context.TalentEvaluations
                .Where(t => t.CollaborateurId == id && t.Actif)
                .OrderByDescending(t => t.DateEvaluation)
                .FirstOrDefaultAsync();

            var anciennete = (DateTime.Today - collaborateur.DateEmbauche).TotalDays / 365.25;

            // 40 % compétences — couverture pondérée (somme min(1, actuel/requis) / nb compétences)
            var scoreComp = competencesRequises.Any()
                ? competencesRequises.Sum(req =>
                {
                    currentSkills.TryGetValue(req.Competence, out var niv);
                    return Math.Min(1.0, (double)niv / req.NiveauRequis);
                }) / competencesRequises.Count * 100.0
                : (collaborateur.NiveauPreparationSuccession ?? 50);

            // 25 % performance
            var scorePerformance = talentEval != null
                ? Math.Min(100.0, talentEval.PerformanceScore * 20.0)
                : (collaborateur.NiveauPreparationSuccession ?? 50);

            // 20 % potentiel
            var scorePotentiel = talentEval != null
                ? Math.Min(100.0, talentEval.PotentielScore * 20.0)
                : (collaborateur.PotentielCarriere?.Trim().ToLowerInvariant()) switch
                {
                    "high"   => 100.0,
                    "medium" => 60.0,
                    "low"    => 20.0,
                    _        => 40.0
                };

            // 15 % ancienneté
            var scoreAnciennete = gradeCibleRef is { AncienneteMinAns: > 0 }
                ? Math.Min(100.0, anciennete * 100.0 / gradeCibleRef.AncienneteMinAns)
                : Math.Min(100.0, anciennete * 25.0);

            var scorePromotion = (int)Math.Round(
                0.40 * scoreComp + 0.25 * scorePerformance + 0.20 * scorePotentiel + 0.15 * scoreAnciennete);

            // Délai estimé
            var gapTotal    = manquantes.Count;
            var moisMin     = Math.Max(3, gapTotal * 3);
            var delaiEstime = gapTotal == 0 ? "Prêt maintenant" : $"{moisMin}-{moisMin + 3} mois";

            // Justification RH — texte narratif lisible par n8n et les RH
            var perfLabel = talentEval != null
                ? $"performance {talentEval.PerformanceScore}/5"
                : "performance non évaluée";

            var potLabel = talentEval != null
                ? $"potentiel {talentEval.PotentielScore}/5"
                : (collaborateur.PotentielCarriere?.ToLowerInvariant()) switch
                {
                    "high"   => "haut potentiel",
                    "medium" => "potentiel solide",
                    "low"    => "développement requis",
                    _        => "potentiel non évalué"
                };

            var gapText = manquantes.Any()
                ? $"À développer : {string.Join(", ", manquantes.Take(3))}{(manquantes.Count > 3 ? "…" : ".")} "
                : "Toutes les compétences requises sont couvertes. ";

            var justificationRH =
                $"{collaborateur.Prenom} {collaborateur.Nom} — ancienneté {Math.Round(anciennete, 1)} an(s), " +
                $"{perfLabel}, {potLabel}. " +
                $"Couverture compétences {gradeCibleNom} : {acquises.Count}/{competencesRequises.Count}. " +
                gapText +
                $"Score promotion global : {scorePromotion}/100.";

            return Ok(new
            {
                scorePromotion,
                gradeActuel,
                gradeCible        = gradeCibleNom,
                competencesAcquises   = acquises,
                competencesManquantes = manquantes,
                delaiEstime,
                justificationRH,
                scoreDetail = new
                {
                    competences = Math.Round(scoreComp, 1),
                    performance = Math.Round(scorePerformance, 1),
                    potentiel   = Math.Round(scorePotentiel, 1),
                    anciennete  = Math.Round(scoreAnciennete, 1),
                    poids       = "40% comp + 25% perf + 20% pot + 15% anc"
                }
            });
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
                .Include(x => x.CollaborateurCertifications!)
                    .ThenInclude(cc => cc.Certification)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (c == null) return NotFound(new { error = "Collaborateur non trouvé." });

            return Ok(new
            {
                id                     = c.Id,
                nom                    = $"{c.Prenom} {c.Nom}",
                poste                  = c.Poste,
                grade                  = c.Grade,
                departement            = c.Departement,
                competences            = c.Competences?.Select(x => new { x.Nom, x.NiveauActuel }),
                // Phase 2 — certifications
                certifications         = c.CollaborateurCertifications?.Select(cc => new
                {
                    nom            = cc.Certification?.Nom,
                    organisme      = cc.Certification?.Organisme,
                    domaine        = cc.Certification?.Domaine,
                    codeExamen     = cc.Certification?.CodeExamen,
                    dateObtention  = cc.DateObtention?.ToString("yyyy-MM-dd"),
                    dateExpiration = cc.DateExpiration?.ToString("yyyy-MM-dd"),
                    statut         = cc.Statut,
                }) ?? Enumerable.Empty<object>(),
                // Phase 3 — champs CRM / Promotion Readiness
                nombreImplementations  = c.NombreImplementations,
                experienceDomainAnnees = c.ExperienceDomainAnnees,
                modeDeploiement        = c.ModeDeploiement?.ToString(),
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

            // Mêmes exigences que la vue Razor (moteur partagé)
            var referentiel = await _context.CompetencesRequisesParPoste
                .AsNoTracking()
                .Where(cr => cr.Poste == partant.Poste)
                .ToListAsync();

            var exigences = SuccessionEngine.BuildExigences(referentiel, partant.Competences);

            var deptPartant = (partant.Departement ?? "").Trim();

            var autres = await _context.Collaborateurs
                .Include(c => c.Competences)
                .Where(c => c.Id != collaborateurId && c.Actif)
                .ToListAsync();

            var scores = autres
                .Select(a => SuccessionEngine.Score(a, exigences, deptPartant))
                .ToList();

            object ToApiItem(ResultatScore s) => new
            {
                id                    = s.Candidat.Id,
                nom                   = $"{s.Candidat.Prenom} {s.Candidat.Nom}",
                poste                 = s.Candidat.Poste ?? "",
                grade                 = s.Candidat.Grade ?? "",
                departement           = s.Candidat.Departement ?? "",
                scoreSuccession       = s.ScoreSuccession,
                scoreCouverture       = s.ScoreCouverture,
                competencesCommunes   = s.NbCommunes,
                competencesManquantes = s.Manquantes,
                profilTransversal     = s.ProfilTransversal,
                ancienneteAns         = s.AnciennetéAns,
                estEligible           = s.EstEligible
            };

            var top3 = scores
                .Where(s => string.Equals(s.Candidat.Grade, partant.Grade, StringComparison.OrdinalIgnoreCase)
                         && s.EstEligible
                         && s.NbCommunes > 0)
                .OrderByDescending(s => s.ScoreSuccession)
                .Take(3)
                .Select(ToApiItem)
                .ToList();

            var enAttente = scores
                .Where(s => (!string.Equals(s.Candidat.Grade, partant.Grade, StringComparison.OrdinalIgnoreCase)
                          || !s.EstEligible)
                         && s.NbCommunes > 0)
                .OrderByDescending(s => s.ScoreSuccession)
                .Take(3)
                .Select(ToApiItem)
                .ToList();

            return Ok(new
            {
                collaborateurNom    = $"{partant.Prenom} {partant.Nom}",
                poste               = partant.Poste,
                grade               = partant.Grade,
                competencesRequises = exigences.Select(e => new { e.Nom, e.NiveauRequis }),
                top3,
                candidatsEnAttente  = enAttente,
                avertissement       = enAttente.Any()
                    ? $"{enAttente.Count} candidat(s) non éligible(s) au remplacement direct — grade différent ou ancienneté < 2 ans."
                    : null
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

        // ── US-001 — Conversation talent employé (9-box, OKRs, recommandation) ──
        [AllowAnonymous]
        [HttpGet("talent-evaluation/{collaborateurId:int}")]
        public async Task<IActionResult> GetTalentEvaluation(int collaborateurId)
        {
            var collaborateur = await _context.Collaborateurs
                .Include(c => c.Competences)
                .Include(c => c.Inscriptions)
                .FirstOrDefaultAsync(c => c.Id == collaborateurId);

            if (collaborateur == null)
                return NotFound(new { error = "Collaborateur introuvable." });

            // Dernière évaluation manuelle (même source que la matrice 9-box / TalentController)
            var manualEval = await _context.TalentEvaluations
                .Where(t => t.CollaborateurId == collaborateurId && t.Actif)
                .OrderByDescending(t => t.DateEvaluation)
                .FirstOrDefaultAsync();

            string source;
            int performanceScore;
            int potentielScore;
            NineBoxCategory category;
            string? managerComments;
            string? employeeComments;
            DateTime dateEvaluation;

            if (manualEval != null)
            {
                source = "manual";
                performanceScore = manualEval.PerformanceScore;
                potentielScore = manualEval.PotentielScore;
                category = manualEval.Category;
                // TalentEvaluation ne distingue pas "manager"/"employé" : les deux champs sont
                // renseignés par l'évaluateur (manager/RH) — Performance = angle manager, Potentiel = angle développement/employé.
                managerComments = manualEval.CommentairesPerformance;
                employeeComments = manualEval.CommentairesPotentiel;
                dateEvaluation = manualEval.DateEvaluation;
            }
            else
            {
                // Pas d'évaluation manuelle → réutilise le moteur de scoring partagé (TalentController)
                source = "computed";
                performanceScore = TalentScoringEngine.CalculatePerformanceScore(collaborateur);
                potentielScore = TalentScoringEngine.CalculatePotentielScore(collaborateur);
                category = TalentScoringEngine.Calculate9BoxCategory(performanceScore, potentielScore);
                managerComments = null;
                employeeComments = null;
                dateEvaluation = DateTime.Today;
            }

            var okrs = await _context.OKRs
                .Include(o => o.KeyResults)
                .Where(o => o.CollaborateurId == collaborateurId)
                .OrderByDescending(o => o.Annee)
                .ThenByDescending(o => (int)o.Trimestre)
                .ToListAsync();

            var dernierCycle = okrs.FirstOrDefault();
            var latestOkrs = dernierCycle == null
                ? new List<OKR>()
                : okrs.Where(o => o.Annee == dernierCycle.Annee && o.Trimestre == dernierCycle.Trimestre).ToList();

            var recommendation =
                $"{category.GetDisplayName()} — performance {performanceScore}/5, potentiel {potentielScore}/5" +
                (source == "computed" ? " (score calculé automatiquement, aucune évaluation manuelle enregistrée)." : ".");

            return Ok(new
            {
                employee = new
                {
                    id = collaborateur.Id,
                    nom = $"{collaborateur.Prenom} {collaborateur.Nom}",
                    poste = collaborateur.Poste,
                    grade = collaborateur.Grade,
                    departement = collaborateur.Departement
                },
                evaluation = new
                {
                    source,
                    performanceScore,
                    potentielScore,
                    category = category.ToString(),
                    categoryLabel = category.GetDisplayName(),
                    managerComments,
                    employeeComments,
                    dateEvaluation = dateEvaluation.ToString("yyyy-MM-dd")
                },
                latestOkrs = latestOkrs.Select(o => new
                {
                    id = o.Id,
                    objectif = o.Objectif,
                    annee = o.Annee,
                    trimestre = o.Trimestre.ToString(),
                    statut = o.Statut.ToString(),
                    progressionGlobale = o.ProgressionGlobale,
                    dateFinCible = o.DateFinCible.ToString("yyyy-MM-dd"),
                    keyResults = o.KeyResults.Select(k => new
                    {
                        description = k.Description,
                        progression = k.Progression,
                        statut = k.Statut.ToString()
                    })
                }),
                recommendation
            });
        }

        // ── US-002 — Comparaison auto-évaluation (collaborateur) vs évaluation manager, par compétence ──
        [AllowAnonymous]
        [HttpGet("self-manager-comparison/{collaborateurId:int}")]
        public async Task<IActionResult> GetSelfManagerComparison(int collaborateurId)
        {
            var collaborateur = await _context.Collaborateurs.FindAsync(collaborateurId);
            if (collaborateur == null)
                return NotFound(new { error = "Collaborateur introuvable." });

            // Seules les compétences déjà auto-évaluées ont un EvaluationCompetence à comparer
            var competences = await _context.Competences
                .Include(c => c.CategorieCompetence)
                .Include(c => c.EvaluationCompetence)
                .Where(c => c.CollaborateurId == collaborateurId && c.EvaluationCompetence != null)
                .ToListAsync();

            var comparisons = competences
                .Select(c =>
                {
                    var eval = c.EvaluationCompetence!;
                    int? gap = eval.EvaluationManager.HasValue
                        ? eval.EvaluationManager.Value - eval.AutoEvaluationCollaborateur
                        : null;

                    var alignment = gap == null
                        ? "En attente de validation manager"
                        : Math.Abs(gap.Value) <= 10 ? "Aligné"
                        : gap.Value > 0 ? "Manager plus indulgent"
                        : "Manager plus sévère";

                    return new
                    {
                        competenceId = c.Id,
                        competence = c.Nom,
                        categorie = c.CategorieCompetence?.Nom,
                        selfEvaluation = new
                        {
                            score = eval.AutoEvaluationCollaborateur,
                            comment = eval.CommentaireCollaborateur,
                            date = eval.DateAutoEvaluation?.ToString("yyyy-MM-dd")
                        },
                        managerEvaluation = new
                        {
                            score = eval.EvaluationManager,
                            comment = eval.CommentaireManager,
                            date = eval.DateValidationManager?.ToString("yyyy-MM-dd"),
                            validated = eval.ValidationManager
                        },
                        gap,
                        alignment
                    };
                })
                .OrderBy(x => x.competence)
                .ToList();

            var evaluesParManager = comparisons.Where(x => x.gap.HasValue).ToList();
            var ecartMoyen = evaluesParManager.Any()
                ? Math.Round(evaluesParManager.Average(x => x.gap!.Value), 1)
                : (double?)null;

            return Ok(new
            {
                employee = new
                {
                    id = collaborateur.Id,
                    nom = $"{collaborateur.Prenom} {collaborateur.Nom}",
                    poste = collaborateur.Poste,
                    grade = collaborateur.Grade
                },
                totalCompetences = comparisons.Count,
                competencesValideesParManager = evaluesParManager.Count,
                ecartMoyen,
                comparisons
            });
        }

        // ── US-003 — Évolution du score talent (dernière évaluation vs précédente) ──
        [AllowAnonymous]
        [HttpGet("talent-score-evolution/{collaborateurId:int}")]
        public async Task<IActionResult> GetTalentScoreEvolution(int collaborateurId)
        {
            var collaborateur = await _context.Collaborateurs
                .Include(c => c.Competences)
                .Include(c => c.Inscriptions)
                .FirstOrDefaultAsync(c => c.Id == collaborateurId);

            if (collaborateur == null)
                return NotFound(new { error = "Collaborateur introuvable." });

            var evals = await _context.TalentEvaluations
                .Where(t => t.CollaborateurId == collaborateurId && t.Actif)
                .OrderByDescending(t => t.DateEvaluation)
                .Take(2)
                .ToListAsync();

            var currentEval = evals.ElementAtOrDefault(0);
            var previousEval = evals.ElementAtOrDefault(1);

            string currentSource;
            int currentPerf, currentPot;
            NineBoxCategory currentCat;
            DateTime currentDate;

            if (currentEval != null)
            {
                currentSource = "manual";
                currentPerf = currentEval.PerformanceScore;
                currentPot = currentEval.PotentielScore;
                currentCat = currentEval.Category;
                currentDate = currentEval.DateEvaluation;
            }
            else
            {
                // Pas d'évaluation manuelle → réutilise le moteur de scoring partagé (US-001)
                currentSource = "computed";
                currentPerf = TalentScoringEngine.CalculatePerformanceScore(collaborateur);
                currentPot = TalentScoringEngine.CalculatePotentielScore(collaborateur);
                currentCat = TalentScoringEngine.Calculate9BoxCategory(currentPerf, currentPot);
                currentDate = DateTime.Today;
            }

            static object BuildSnapshot(string source, int perf, int pot, NineBoxCategory cat, DateTime date) => new
            {
                source,
                performanceScore = perf,
                potentielScore = pot,
                category = cat.ToString(),
                categoryLabel = cat.GetDisplayName(),
                dateEvaluation = date.ToString("yyyy-MM-dd")
            };

            var currentSnapshot = BuildSnapshot(currentSource, currentPerf, currentPot, currentCat, currentDate);

            object? previousSnapshot = null;
            object? delta = null;
            var changes = new List<string>();
            string summary;

            if (previousEval != null)
            {
                previousSnapshot = BuildSnapshot("manual", previousEval.PerformanceScore, previousEval.PotentielScore, previousEval.Category, previousEval.DateEvaluation);

                var perfDelta = currentPerf - previousEval.PerformanceScore;
                var potDelta = currentPot - previousEval.PotentielScore;
                var categoryChanged = currentCat != previousEval.Category;

                delta = new { performance = perfDelta, potentiel = potDelta, categoryChanged };

                if (perfDelta != 0)
                    changes.Add($"Performance : {previousEval.PerformanceScore}/5 → {currentPerf}/5 ({(perfDelta > 0 ? "+" : "")}{perfDelta})");
                if (potDelta != 0)
                    changes.Add($"Potentiel : {previousEval.PotentielScore}/5 → {currentPot}/5 ({(potDelta > 0 ? "+" : "")}{potDelta})");
                if (categoryChanged)
                    changes.Add($"Catégorie 9-box : {previousEval.Category.GetDisplayName()} → {currentCat.GetDisplayName()}");

                if (!changes.Any())
                    changes.Add("Aucun changement détecté depuis la dernière évaluation.");

                var trend = perfDelta > 0 || potDelta > 0 ? "progression"
                    : perfDelta < 0 || potDelta < 0 ? "régression"
                    : "stabilité";

                summary = $"{collaborateur.Prenom} {collaborateur.Nom} : {trend} entre le {previousEval.DateEvaluation:dd/MM/yyyy} et le {currentDate:dd/MM/yyyy}. {string.Join(" ", changes)}";
            }
            else
            {
                changes.Add("Aucune évaluation précédente disponible pour comparaison.");
                summary = $"{collaborateur.Prenom} {collaborateur.Nom} : {currentCat.GetDisplayName()} (performance {currentPerf}/5, potentiel {currentPot}/5)" +
                    (currentSource == "computed"
                        ? " — score calculé automatiquement, aucun historique d'évaluation manuelle."
                        : " — première évaluation enregistrée, pas d'historique antérieur.");
            }

            return Ok(new
            {
                employee = new
                {
                    id = collaborateur.Id,
                    nom = $"{collaborateur.Prenom} {collaborateur.Nom}",
                    poste = collaborateur.Poste,
                    grade = collaborateur.Grade
                },
                previousEvaluation = previousSnapshot,
                currentEvaluation = currentSnapshot,
                delta,
                changes,
                summary
            });
        }

        // ── US-004 — Talent reviews en attente de validation manager ─────────────
        // Source unique : EvaluationCompetence.ValidationManager (seul statut "en attente" du schéma).
        [AllowAnonymous]
        [HttpGet("pending-talent-reviews")]
        public async Task<IActionResult> GetPendingTalentReviews([FromQuery] int? managerId = null)
        {
            var collaborateursQuery = _context.Collaborateurs
                .Include(c => c.Competences!)
                    .ThenInclude(comp => comp.EvaluationCompetence)
                .Where(c => c.Actif)
                .AsQueryable();

            if (managerId.HasValue)
                collaborateursQuery = collaborateursQuery.Where(c => c.ManagerId == managerId.Value);

            var collaborateurs = await collaborateursQuery.ToListAsync();

            var employeesAwaitingValidation = collaborateurs
                .Select(c =>
                {
                    var evaluated = (c.Competences ?? Enumerable.Empty<Competence>())
                        .Select(comp => comp.EvaluationCompetence)
                        .Where(e => e != null && e.DateAutoEvaluation != null)
                        .Select(e => e!)
                        .ToList();

                    var pending = evaluated.Where(e => !e.ValidationManager).ToList();

                    return new { Collaborateur = c, Evaluated = evaluated, Pending = pending };
                })
                .Where(x => x.Pending.Any())
                .Select(x => new
                {
                    id = x.Collaborateur.Id,
                    name = $"{x.Collaborateur.Prenom} {x.Collaborateur.Nom}",
                    status = "En attente de validation manager",
                    progress = $"{x.Evaluated.Count - x.Pending.Count}/{x.Evaluated.Count}",
                    lastEvaluationDate = x.Pending.Max(e => e.DateAutoEvaluation!.Value).ToString("yyyy-MM-dd")
                })
                .OrderBy(x => x.lastEvaluationDate)
                .ToList();

            return Ok(new { employeesAwaitingValidation });
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

                // US-006 — rôle résolu côté serveur à partir de l'identité authentifiée (jamais
                // fait confiance à une valeur envoyée par le client). Réutilise ITeamAccessService,
                // déjà utilisé par TalentController/CompetencesController pour ce même besoin.
                var role = _teamAccess.IsPrivileged(User) ? "HR"
                    : User.IsInRole(Roles.Manager) ? "Manager"
                    : "Employee";
                var selfCollaborateurId = await _teamAccess.GetCurrentCollaborateurIdAsync(User);

                var payload = JsonSerializer.Serialize(new
                {
                    message             = request.Message,
                    page                = request.Page ?? "general",
                    contextId           = request.ContextId,
                    sessionMemory       = request.SessionMemory,
                    sessionHistory      = request.SessionHistory,
                    context             = request.SessionMemory,   // bridge : n8n lit body.context
                    role,
                    selfCollaborateurId
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

        // ── Certifications expirantes ─────────────────────────────────────────────
        [AllowAnonymous]
        [HttpGet("certifications-expirantes")]
        public async Task<IActionResult> GetCertificationsExpirantes([FromQuery] int jours = 90)
        {
            var limite = DateTime.Today.AddDays(jours);

            var expirantes = await _context.CollaborateurCertifications
                .Include(cc => cc.Collaborateur)
                .Include(cc => cc.Certification)
                .Where(cc =>
                    cc.Statut == "Active" &&
                    cc.DateExpiration != null &&
                    cc.DateExpiration >= DateTime.Today &&
                    cc.DateExpiration <= limite)
                .OrderBy(cc => cc.DateExpiration)
                .ToListAsync();

            // Trigger Power Automate for certifications expiring within 30 days (Critique urgency)
            var critiques = expirantes
                .Where(cc => (cc.DateExpiration!.Value.Date - DateTime.Today).TotalDays <= 30)
                .ToList();

            foreach (var cc in critiques)
            {
                var notification = CertificationExpirationNotification.Create(
                    cc.CollaborateurId,
                    cc.Collaborateur != null ? $"{cc.Collaborateur.Prenom} {cc.Collaborateur.Nom}".Trim() : "?",
                    cc.Collaborateur?.Email,
                    cc.Collaborateur?.Poste,
                    cc.Collaborateur?.Grade,
                    cc.Collaborateur?.Departement,
                    cc.Certification?.Nom ?? "Certification",
                    cc.Certification?.Organisme,
                    cc.Certification?.Domaine,
                    cc.Certification?.CodeExamen,
                    cc.DateExpiration!.Value);

                var paResult = await _powerAutomate.NotifyCertificationExpirationAsync(notification, HttpContext.RequestAborted);
                if (!paResult.Success)
                    _logger.LogWarning(
                        "Power Automate CertificationExpiration notification failed for collaborateur {CollabId}, certification '{Cert}': {Error}",
                        cc.CollaborateurId, cc.Certification?.Nom, paResult.ErrorMessage);
            }

            if (critiques.Count > 0)
                _logger.LogInformation(
                    "{Count} critical certification expiration notification(s) triggered.", critiques.Count);

            return Ok(new
            {
                periode          = $"Dans les {jours} prochains jours",
                total            = expirantes.Count,
                certifications   = expirantes.Select(cc => new
                {
                    collaborateurId  = cc.CollaborateurId,
                    collaborateur    = cc.Collaborateur != null ? $"{cc.Collaborateur.Prenom} {cc.Collaborateur.Nom}" : "?",
                    poste            = cc.Collaborateur?.Poste,
                    grade            = cc.Collaborateur?.Grade,
                    certification    = cc.Certification?.Nom,
                    organisme        = cc.Certification?.Organisme,
                    domaine          = cc.Certification?.Domaine,
                    dateExpiration   = cc.DateExpiration!.Value.ToString("yyyy-MM-dd"),
                    joursRestants    = (int)(cc.DateExpiration!.Value - DateTime.Today).TotalDays,
                    urgence          = DecisionEngine.ClassifyCertificationUrgency((int)(cc.DateExpiration.Value - DateTime.Today).TotalDays),
                })
            });
        }

        // ── Staffing CRM — qui peut prendre quel rôle projet ─────────────────────
        [AllowAnonymous]
        [HttpGet("staffing-crm")]
        public async Task<IActionResult> GetStaffingCRM([FromQuery] string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return BadRequest(new { error = "Paramètre role requis, ex: ?role=Architect CRM" });

            var competencesRequises = await _context.CompetencesRequisesParPoste
                .Where(c => c.Poste == role)
                .ToListAsync();

            if (!competencesRequises.Any())
                return NotFound(new { error = $"Rôle '{role}' inconnu dans le référentiel CRM." });

            var collaborateurs = await _context.Collaborateurs
                .Include(c => c.Competences)
                .Include(c => c.CollaborateurCertifications!)
                    .ThenInclude(cc => cc.Certification)
                .Where(c => c.Actif)
                .ToListAsync();

            var nomsRequis = competencesRequises.Select(c => c.Competence).ToList();
            var comparer   = StringComparer.OrdinalIgnoreCase;

            var candidats = collaborateurs.Select(c =>
            {
                var comps = c.Competences?
                    .Where(x => !string.IsNullOrWhiteSpace(x.Nom))
                    .ToList() ?? new List<Competence>();

                var matchDetails = nomsRequis.Select(req =>
                {
                    var found     = comps.FirstOrDefault(x => comparer.Equals(x.Nom.Trim(), req));
                    var reqNiveau = competencesRequises.First(x => comparer.Equals(x.Competence, req)).NiveauRequis;
                    return new { req, niveau = found?.NiveauActuel ?? 0, reqNiveau, gap = Math.Max(0, reqNiveau - (found?.NiveauActuel ?? 0)) };
                }).ToList();

                var covered   = matchDetails.Count(x => x.gap == 0);
                var scoreComp = nomsRequis.Count > 0 ? Math.Round(covered * 100.0 / nomsRequis.Count, 1) : 0;
                var certsActives = c.CollaborateurCertifications?
                    .Count(cc => cc.Statut == "Active" && (cc.DateExpiration == null || cc.DateExpiration > DateTime.Today)) ?? 0;
                var scoreCerts = Math.Min(100.0, certsActives * 25.0);
                var scoreImpl  = Math.Min(100.0, (c.NombreImplementations ?? 0) * 20.0);
                var scoreTotal = Math.Round(scoreComp * 0.55 + scoreCerts * 0.25 + scoreImpl * 0.20, 1);

                return new
                {
                    id                    = c.Id,
                    nom                   = $"{c.Prenom} {c.Nom}",
                    poste                 = c.Poste,
                    grade                 = c.Grade,
                    departement           = c.Departement,
                    scoreTotal,
                    scoreCompetences      = scoreComp,
                    scoreCertifications   = Math.Round(scoreCerts, 1),
                    nombreImplementations = c.NombreImplementations ?? 0,
                    nombreCertifications  = certsActives,
                    competencesCouvertes  = covered,
                    competencesRequises   = nomsRequis.Count,
                    readiness             = scoreTotal >= 80 ? "Prêt" : scoreTotal >= 60 ? "Quasi-prêt" : scoreTotal >= 40 ? "En développement" : "Non éligible",
                };
            })
            .Where(x => x.scoreTotal > 0)
            .OrderByDescending(x => x.scoreTotal)
            .Take(8)
            .ToList();

            return Ok(new
            {
                role,
                competencesRequises = nomsRequis,
                totalCandidats      = candidats.Count,
                top                 = candidats,
            });
        }

        // ── KPIs CRM — tableau de bord compétences CRM de l'équipe ───────────────
        [AllowAnonymous]
        [HttpGet("kpi-crm")]
        public async Task<IActionResult> GetKpiCRM()
        {
            var collaborateurs = await _context.Collaborateurs
                .Include(c => c.Competences)
                .Include(c => c.CollaborateurCertifications!)
                    .ThenInclude(cc => cc.Certification)
                .Where(c => c.Actif)
                .ToListAsync();

            var rolesCRM = new[]
            {
                "Technical Consultant CRM", "Functional Consultant CRM", "Techno Functional CRM",
                "Business Analyst CRM", "Quality Analyst CRM", "Project Manager CRM", "Architect CRM"
            };
            var competencesCRMCles = new[]
            {
                "Microsoft Dynamics 365 CRM", "Power Platform", "Intégration ERP/CRM",
                "D365 Sales & Customer Service", "Architecture solution"
            };

            var totalCollabs = collaborateurs.Count;

            var avecCertsCRM = collaborateurs.Count(c =>
                c.CollaborateurCertifications?.Any(cc =>
                    cc.Statut == "Active" &&
                    (cc.DateExpiration == null || cc.DateExpiration > DateTime.Today) &&
                    cc.Certification?.Domaine != null &&
                    cc.Certification.Domaine.Contains("CRM", StringComparison.OrdinalIgnoreCase)
                ) == true);

            var avgImpl = collaborateurs.Any(c => c.NombreImplementations.HasValue)
                ? Math.Round(collaborateurs.Where(c => c.NombreImplementations.HasValue).Average(c => (double)c.NombreImplementations!.Value), 1)
                : 0.0;

            var distModes = collaborateurs
                .Where(c => c.ModeDeploiement.HasValue)
                .GroupBy(c => c.ModeDeploiement!.Value.ToString())
                .Select(g => new { mode = g.Key, count = g.Count() })
                .ToList();

            var referentielCRM = await _context.CompetencesRequisesParPoste
                .Where(c => rolesCRM.Contains(c.Poste))
                .ToListAsync();

            var couvertureRoles = rolesCRM.Select(role =>
            {
                var reqRole = referentielCRM.Where(c => c.Poste == role).Select(c => c.Competence).ToList();
                var eligible = collaborateurs.Count(c =>
                {
                    var noms = c.Competences?.Select(x => x.Nom?.Trim()).Where(n => n != null).ToList() ?? new List<string?>();
                    var covered = reqRole.Count(req => noms.Any(n => string.Equals(n, req, StringComparison.OrdinalIgnoreCase)));
                    return reqRole.Count > 0 && covered * 100.0 / reqRole.Count >= 50;
                });
                return new { role, eligible, couverturePct = totalCollabs > 0 ? Math.Round(eligible * 100.0 / totalCollabs, 0) : 0.0 };
            }).ToList();

            var niveauxCles = competencesCRMCles.Select(comp =>
            {
                var vals = collaborateurs
                    .SelectMany(c => c.Competences ?? Enumerable.Empty<Competence>())
                    .Where(c => string.Equals(c.Nom?.Trim(), comp, StringComparison.OrdinalIgnoreCase))
                    .Select(c => (double)c.NiveauActuel)
                    .ToList();
                return new { competence = comp, nCollaborateurs = vals.Count, niveauMoyen = vals.Any() ? Math.Round(vals.Average(), 1) : 0.0 };
            }).ToList();

            return Ok(new
            {
                totalCollaborateurs             = totalCollabs,
                tauxCertificationCRM            = totalCollabs > 0 ? Math.Round(avecCertsCRM * 100.0 / totalCollabs, 1) : 0.0,
                nombreCollabsCertifiesCRM       = avecCertsCRM,
                moyenneImplementations          = avgImpl,
                distributionModeDeploiement     = distModes,
                couvertureRolesCRM              = couvertureRoles,
                niveauxCompetencesCRMCles       = niveauxCles,
            });
        }

        // ── Plan de développement intelligent — gap rôle CRM → formations ─────────
        [AllowAnonymous]
        [HttpGet("plan-developpement/{collaborateurId}")]
        public async Task<IActionResult> GetPlanDeveloppement(int collaborateurId, [FromQuery] string posteCible)
        {
            if (string.IsNullOrWhiteSpace(posteCible))
                return BadRequest(new { error = "Paramètre posteCible requis, ex: ?posteCible=Architect CRM" });

            var collab = await _context.Collaborateurs
                .Include(c => c.Competences)
                .Include(c => c.CollaborateurCertifications!)
                    .ThenInclude(cc => cc.Certification)
                .Include(c => c.Manager)
                .FirstOrDefaultAsync(c => c.Id == collaborateurId);

            if (collab == null) return NotFound(new { error = "Collaborateur introuvable." });

            var reqs = await _context.CompetencesRequisesParPoste
                .Where(c => c.Poste == posteCible)
                .ToListAsync();

            if (!reqs.Any())
                return NotFound(new { error = $"Rôle cible '{posteCible}' introuvable dans le référentiel." });

            var nomsActuels = (collab.Competences ?? Enumerable.Empty<Competence>())
                .Where(c => !string.IsNullOrWhiteSpace(c.Nom))
                .ToDictionary(c => c.Nom.Trim(), c => c.NiveauActuel, StringComparer.OrdinalIgnoreCase);

            var gaps = reqs.Select(req =>
            {
                nomsActuels.TryGetValue(req.Competence, out var niveau);
                var gap = Math.Max(0, req.NiveauRequis - niveau);
                return new { competence = req.Competence, niveauActuel = niveau, niveauRequis = req.NiveauRequis, gap, statut = niveau >= req.NiveauRequis ? "Couvert" : niveau > 0 ? "À renforcer" : "Manquant" };
            })
            .OrderByDescending(g => g.gap)
            .ToList();

            var formations = await _context.Formations
                .Where(f => f.Categorie == "CRM" || (f.DomaineCompetence != null && f.DomaineCompetence.Contains("CRM")))
                .ToListAsync();

            var plan = gaps.Where(g => g.gap > 0).Select(g =>
            {
                var f = formations.FirstOrDefault(x =>
                    (!string.IsNullOrEmpty(x.CompetenceVisee) && x.CompetenceVisee.Contains(g.competence, StringComparison.OrdinalIgnoreCase))
                    || x.Titre.Contains(g.competence, StringComparison.OrdinalIgnoreCase));

                return new
                {
                    competence = g.competence,
                    gap        = g.gap,
                    statut     = g.statut,
                    priorite   = g.gap >= 3 ? "Critique" : g.gap == 2 ? "Haute" : "Normale",
                    formation  = f == null ? null : (object)new
                    {
                        id              = f.Id,
                        titre           = f.Titre,
                        dureeHeures     = f.DureeHeures,
                        plateforme      = f.Plateforme,
                        certificationVisee = f.CertificationNom,
                        estCertifiante  = f.EstCertifiante,
                        niveauDifficulte = f.NiveauDifficulte,
                    },
                };
            }).ToList();

            var certsActives = collab.CollaborateurCertifications?
                .Where(cc => cc.Statut == "Active" && (cc.DateExpiration == null || cc.DateExpiration > DateTime.Today))
                .Select(cc => cc.Certification?.Nom).Where(n => n != null).ToList() ?? new List<string?>();

            var scoreActuel = reqs.Count > 0 ? Math.Round(gaps.Count(g => g.gap == 0) * 100.0 / reqs.Count, 1) : 0.0;

            // Build typed formation list for notification before anonymous projection
            var notifFormations = gaps
                .Where(g => g.gap > 0)
                .Take(5)
                .Select(g =>
                {
                    var f = formations.FirstOrDefault(x =>
                        (!string.IsNullOrEmpty(x.CompetenceVisee)
                            && x.CompetenceVisee.Contains(g.competence, StringComparison.OrdinalIgnoreCase))
                        || x.Titre.Contains(g.competence, StringComparison.OrdinalIgnoreCase));

                    return new DevelopmentPlanFormation(
                        f?.Titre ?? $"Parcours — {g.competence}",
                        g.competence,
                        g.gap,
                        g.gap >= 3 ? "Critique" : g.gap == 2 ? "Haute" : "Normale",
                        f?.DureeHeures);
                })
                .ToList();

            var planNotification = DevelopmentPlanNotification.Create(
                collaborateurId,
                $"{collab.Prenom} {collab.Nom}".Trim(),
                collab.Email,
                managerEmail: collab.Manager?.Email,
                gradeActuel: collab.Grade ?? "N/A",
                posteCible: posteCible,
                departement: collab.Departement,
                scoreCouvertureActuel: scoreActuel,
                nombreCompetencesADevelopper: gaps.Count(g => g.gap > 0),
                dureeEstimeeSemaines: gaps.Where(g => g.gap > 0).Sum(g => Math.Max(2, g.gap * 3)),
                formations: notifFormations);

            var paDevResult = await _powerAutomate.NotifyDevelopmentPlanCreatedAsync(planNotification, HttpContext.RequestAborted);
            if (!paDevResult.Success)
                _logger.LogWarning(
                    "Power Automate DevelopmentPlanCreated notification failed for collaborateur {Id}: {Error}",
                    collaborateurId, paDevResult.ErrorMessage);

            return Ok(new
            {
                collaborateurNom         = $"{collab.Prenom} {collab.Nom}",
                gradeActuel              = collab.Grade,
                posteCible,
                scoreCouverte            = scoreActuel,
                totalCompetences         = reqs.Count,
                competencesCouvertes     = gaps.Count(g => g.gap == 0),
                competencesADevelopper   = gaps.Count(g => g.gap > 0),
                certificationsDejaPossedees = certsActives,
                nombreImplementations    = collab.NombreImplementations ?? 0,
                planDeveloppement        = plan,
                dureeEstimeeSemaines     = plan.Sum(p => Math.Max(2, p.gap * 3)),
            });
        }

        // ── Phase 4 — Critères de promotion par grade ────────────────────────────
        [AllowAnonymous]
        [HttpGet("criteres-promotion")]
        public async Task<IActionResult> GetCriteresPromotion([FromQuery] string? grade = null)
        {
            var tous = await _context.GradeReferentiels
                .OrderBy(g => g.Niveau)
                .ToListAsync();

            if (!tous.Any())
                return NotFound(new { error = "Référentiel de grades non initialisé." });

            if (string.IsNullOrWhiteSpace(grade))
            {
                return Ok(new
                {
                    referentiel = tous.Select(g => new
                    {
                        grade                    = g.Grade,
                        niveau                   = g.Niveau,
                        niveauMinCompetences     = g.NiveauMinCompetences,
                        ancienneteMinAns         = g.AncienneteMinAns,
                        nombreImplementationsMin = g.NombreImplementationsMin,
                        experienceDomainMinAns   = g.ExperienceDomainMinAns,
                        gradeSuivant             = g.GradeSuivant,
                        description              = g.Description,
                    })
                });
            }

            var actuel = tous.FirstOrDefault(g =>
                string.Equals(g.Grade, grade.Trim(), StringComparison.OrdinalIgnoreCase));

            if (actuel == null)
                return NotFound(new { error = $"Grade '{grade}' non trouvé dans le référentiel." });

            var suivant = string.IsNullOrEmpty(actuel.GradeSuivant)
                ? null
                : tous.FirstOrDefault(g =>
                    string.Equals(g.Grade, actuel.GradeSuivant, StringComparison.OrdinalIgnoreCase));

            return Ok(new
            {
                grade   = actuel.Grade,
                niveau  = actuel.Niveau,
                criteresPourMaintenir = new
                {
                    niveauMinCompetences     = actuel.NiveauMinCompetences,
                    ancienneteMinAns         = actuel.AncienneteMinAns,
                    nombreImplementationsMin = actuel.NombreImplementationsMin,
                    experienceDomainMinAns   = actuel.ExperienceDomainMinAns,
                    description              = actuel.Description,
                },
                gradeSuivant = suivant == null ? null : (object)new
                {
                    grade                    = suivant.Grade,
                    niveauMinCompetences     = suivant.NiveauMinCompetences,
                    ancienneteMinAns         = suivant.AncienneteMinAns,
                    nombreImplementationsMin = suivant.NombreImplementationsMin,
                    experienceDomainMinAns   = suivant.ExperienceDomainMinAns,
                    description              = suivant.Description,
                },
            });
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