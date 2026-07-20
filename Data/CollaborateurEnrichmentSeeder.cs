using Microsoft.EntityFrameworkCore;
using SIRH.EY.Models;

namespace SIRH.EY.Data;

/// <summary>
/// Comble les profils collaborateurs sans compétences et/ou sans formations, en
/// s'appuyant sur des règles simples basées sur le poste. Vérification faite contre la
/// base réelle avant écriture de ce fichier : Formations/Certifications ci-dessous
/// existent déjà dans le catalogue (voir FormationsCRMSeeder, PostesReferentielSeeder,
/// CertificationsSeeder) — ce seeder les RÉUTILISE (recherche par CompetenceVisee /
/// Nom), il n'en recrée aucune, sauf "CISA" qui n'a pas d'équivalent dans le catalogue
/// actuel et est ajoutée une seule fois, de façon idempotente.
///
/// Chaque collaborateur ne reçoit que ce qui lui manque réellement : les compétences et
/// les formations sont vérifiées indépendamment (un collaborateur qui a déjà des
/// compétences mais aucune formation ne reçoit que des formations, pas de doublon de
/// compétences).
/// </summary>
public static class CollaborateurEnrichmentSeeder
{
    private const string SeedVersion = "COLLAB_ENRICHMENT_V1_2026_07";

    private record CompetencePlan(string Nom, int NiveauCible, string? Categorie);

    public static async Task SeedCollaborateurDataAsync(ApplicationDbContext context)
    {
        if (await context.Parametres.AnyAsync(p => p.Code == SeedVersion))
            return;

        var collaborateurs = await context.Collaborateurs
            .Include(c => c.Competences)
            .Include(c => c.Inscriptions)
            .Where(c => c.Actif)
            .ToListAsync();

        var formations   = await context.Formations.ToListAsync();
        var certifications = await context.Certifications.ToListAsync();
        var categories   = await context.CategoriesCompetences.ToListAsync();
        var skills       = await context.Skills.ToListAsync();
        var existingCertLinks = await context.CollaborateurCertifications.ToListAsync();

        var skillsByKey = skills
            .GroupBy(s => s.Nom.Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var seededRandom = new Random(42); // reproductibilité — même convention que EvaluationHistoriqueSeeder

        // ── Helpers ──────────────────────────────────────────────────────────
        Skill GetOrCreateSkill(string nom)
        {
            var key = nom.Trim().ToLowerInvariant();
            if (skillsByKey.TryGetValue(key, out var found)) return found;
            var created = new Skill { Nom = nom.Trim(), Actif = true, DateCreation = DateTime.Now };
            context.Skills.Add(created);
            skillsByKey[key] = created;
            return created;
        }

        Formation? FindFormationParCompetenceVisee(string competenceVisee) =>
            formations.FirstOrDefault(f =>
                !string.IsNullOrWhiteSpace(f.CompetenceVisee) &&
                f.CompetenceVisee.Trim().Equals(competenceVisee.Trim(), StringComparison.OrdinalIgnoreCase));

        int? FindCategorieId(string? nom) => nom == null
            ? null
            : categories.FirstOrDefault(c => c.Nom.Equals(nom, StringComparison.OrdinalIgnoreCase))?.Id;

        Certification GetOrCreateCertification(string nom, string domaine)
        {
            var found = certifications.FirstOrDefault(c => c.Nom.Trim().Equals(nom.Trim(), StringComparison.OrdinalIgnoreCase));
            if (found != null) return found;
            var created = new Certification { Nom = nom.Trim(), Domaine = domaine, EstReconnue = true };
            context.Certifications.Add(created);
            certifications.Add(created);
            return created;
        }

        // ── Règles par poste (correspondance sur mots-clés, insensible à la casse) ──
        // Chaque règle renvoie : compétences à injecter, formations à rattacher (via
        // CompetenceVisee, réutilisation pure du catalogue existant), certifications à
        // rattacher.
        (List<CompetencePlan> Competences, List<string> FormationsVisees, List<(string Nom, string Domaine)> Certifs)
            ResoudrePlanPourPoste(string? poste, string? grade)
        {
            var p = (poste ?? "").ToLowerInvariant();
            var g = (grade ?? "").ToLowerInvariant();

            if (p.Contains("audit") || p.Contains("assurance"))
            {
                return (
                    new List<CompetencePlan> {
                        new("Audit & contrôle interne", 4, "Audit"),
                        new("Communication", 3, "Soft skills"),
                        new("Excel avancé", 3, "Outils")
                    },
                    new List<string> { "Audit & contrôle interne", "Communication", "Excel avancé" },
                    new List<(string, string)> {
                        ("CISA", "Audit / SI"),                 // n'existe pas encore dans le catalogue → créée une fois
                        ("Diplôme IFRS — ACCA", "Finance / Audit") // existe déjà (CertificationsSeeder) → réutilisée
                    }
                );
            }

            if (p.Contains("cybersecurity") || p.Contains("analyst"))
            {
                return (
                    new List<CompetencePlan> {
                        new("Sécurité des SI", 4, "Risk"),
                        new("ISO 27001", 3, "Risk"),
                        new("Analyse de risques", 3, "Risk")
                    },
                    new List<string> { "ISO 27001" },
                    new List<(string, string)> {
                        ("ISO 27001 Lead Implementer", "Cybersécurité") // existe déjà → réutilisée
                    }
                );
            }

            if (p.Contains("manager") || p.Contains("director") ||
                g is "manager" or "director" or "senior manager" or "partner")
            {
                return (
                    new List<CompetencePlan> {
                        new("Leadership", 4, "Leadership"),
                        new("Gestion de projet", 4, "Management"),
                        new("Stakeholder management", 3, "Management")
                    },
                    new List<string> { "Leadership", "Gestion de projet" },
                    new List<(string, string)>()
                );
            }

            // Filet générique — garantit qu'aucun profil ne reste totalement vide,
            // conformément à l'objectif ("aucun collaborateur ne se retrouve avec des
            // sections vides"), même pour un intitulé de poste non couvert ci-dessus.
            return (
                new List<CompetencePlan> {
                    new("Communication", 3, "Soft skills"),
                    new("Excel avancé", 3, "Outils")
                },
                new List<string> { "Communication", "Excel avancé" },
                new List<(string, string)>()
            );
        }

        int competencesAjoutees = 0, formationsAjoutees = 0, certificationsAjoutees = 0;

        foreach (var collab in collaborateurs)
        {
            var hasCompetences = collab.Competences?.Any() == true;
            var hasFormations  = collab.Inscriptions?.Any() == true;

            if (hasCompetences && hasFormations)
                continue; // déjà synchronisé sur les deux dimensions — on n'y touche pas

            var plan = ResoudrePlanPourPoste(collab.Poste, collab.Grade);

            if (!hasCompetences)
            {
                foreach (var cp in plan.Competences)
                {
                    var competence = new Competence
                    {
                        Nom                   = cp.Nom,
                        NiveauActuel          = Math.Max(1, cp.NiveauCible - 2),
                        NiveauCible           = cp.NiveauCible,
                        CategorieCompetenceId = FindCategorieId(cp.Categorie),
                        CollaborateurId       = collab.Id,
                        DateEvaluation        = DateTime.Now,
                        SkillId               = GetOrCreateSkill(cp.Nom).Id
                    };
                    context.Competences.Add(competence);
                    competencesAjoutees++;
                }
            }

            if (!hasFormations)
            {
                foreach (var competenceVisee in plan.FormationsVisees.Distinct())
                {
                    var formation = FindFormationParCompetenceVisee(competenceVisee);
                    if (formation == null) continue; // aucune formation réelle pour ce sujet — on ne fabrique rien

                    context.Inscriptions.Add(new Inscription
                    {
                        CollaborateurId = collab.Id,
                        FormationId     = formation.Id,
                        DateInscription = DateTime.Now.AddMonths(-seededRandom.Next(1, 10)),
                        Terminee        = true,
                        Progression     = 100,
                        DateCompletion  = DateTime.Now.AddMonths(-seededRandom.Next(0, 6))
                    });
                    formationsAjoutees++;
                }

                foreach (var (nom, domaine) in plan.Certifs)
                {
                    var certification = GetOrCreateCertification(nom, domaine);
                    var dejaLiee = existingCertLinks.Any(cc =>
                        cc.CollaborateurId == collab.Id && cc.CertificationId == certification.Id);
                    if (dejaLiee) continue;

                    context.CollaborateurCertifications.Add(new CollaborateurCertification
                    {
                        CollaborateurId = collab.Id,
                        CertificationId = certification.Id,
                        DateObtention   = DateTime.Now.AddMonths(-seededRandom.Next(1, 24)),
                        Statut          = "Active"
                    });
                    certificationsAjoutees++;
                }
            }
        }

        context.Parametres.Add(new Parametre
        {
            Code                 = SeedVersion,
            Valeur               = $"{competencesAjoutees} competences, {formationsAjoutees} formations, {certificationsAjoutees} certifications",
            TypeValeur           = "string",
            Description          = "Enrichissement des profils collaborateurs sans compétences/formations, basé sur le poste.",
            EstModifiable        = false,
            DerniereModification = DateTime.Now
        });

        await context.SaveChangesAsync();
    }
}
