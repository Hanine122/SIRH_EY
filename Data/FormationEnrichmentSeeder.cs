using Microsoft.EntityFrameworkCore;
using SIRH.EY.Models;

namespace SIRH.EY.Data;

/// <summary>
/// Enriches existing Formation records with ExternalUrl, Plateforme, Description,
/// EstStrategique and EstForteDemande fields introduced in the FormationLearningEnrichment migration.
/// Idempotent — skips if the version key is already present.
/// </summary>
public static class FormationEnrichmentSeeder
{
    private const string VersionKey = "FORMATION_ENRICHMENT_V1_2026_06";

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Parametres.AnyAsync(p => p.Code == VersionKey))
            return;

        // Map: formation title substring → enrichment data
        var enrichments = new[]
        {
            new FormationEnrichment(
                "Communication",
                Plateforme: "LinkedIn Learning",
                ExternalUrl: "https://www.linkedin.com/learning/topics/communication",
                Description: "Maîtrisez les techniques de communication professionnelle : présentation, négociation, écoute active et feedback constructif. Idéal pour les consultants en contact client.",
                CompetencesRequises: null,
                CertificationNom: null,
                EstStrategique: false,
                EstForteDemande: true),

            new FormationEnrichment(
                "Excel",
                Plateforme: "Microsoft Learn",
                ExternalUrl: "https://learn.microsoft.com/fr-fr/training/paths/excel-advanced/",
                Description: "Perfectionnez votre maîtrise d'Excel : tableaux croisés dynamiques, Power Query, macros VBA et visualisations avancées pour l'analyse financière.",
                CompetencesRequises: "Excel bases",
                CertificationNom: "Microsoft Office Specialist: Excel Expert",
                EstStrategique: false,
                EstForteDemande: true),

            new FormationEnrichment(
                "Gestion de projet",
                Plateforme: "Coursera",
                ExternalUrl: "https://www.coursera.org/professional-certificates/google-project-management",
                Description: "Formation complète en gestion de projet selon les standards PMI. Couvre la planification, le suivi, la gestion des risques et les méthodes agiles.",
                CompetencesRequises: null,
                CertificationNom: "Google Project Management Certificate",
                EstStrategique: true,
                EstForteDemande: true),

            new FormationEnrichment(
                "Power BI",
                Plateforme: "Microsoft Learn",
                ExternalUrl: "https://learn.microsoft.com/fr-fr/training/paths/create-use-analytics-reports-power-bi/",
                Description: "Créez des tableaux de bord interactifs et des rapports analytiques avec Power BI Desktop et Power BI Service. Apprenez le langage DAX et les bonnes pratiques de modélisation.",
                CompetencesRequises: "Excel, SQL de base",
                CertificationNom: "PL-300: Microsoft Power BI Data Analyst",
                EstStrategique: true,
                EstForteDemande: true),

            new FormationEnrichment(
                "SQL",
                Plateforme: "Udemy",
                ExternalUrl: "https://www.udemy.com/course/the-complete-sql-bootcamp/",
                Description: "Maîtrisez SQL depuis les bases jusqu'aux requêtes complexes : JOIN, sous-requêtes, procédures stockées, optimisation et bases de données relationnelles.",
                CompetencesRequises: null,
                CertificationNom: null,
                EstStrategique: false,
                EstForteDemande: true),

            new FormationEnrichment(
                "Audit",
                Plateforme: null,
                ExternalUrl: null,
                Description: "Formation aux normes d'audit internationales (ISA), aux techniques de revue analytique, à la documentation des travaux et à la communication des résultats.",
                CompetencesRequises: "Comptabilité, IFRS bases",
                CertificationNom: "Certificat EY Audit Fondamentaux",
                EstStrategique: true,
                EstForteDemande: false),

            new FormationEnrichment(
                "IFRS",
                Plateforme: null,
                ExternalUrl: null,
                Description: "Normes IFRS appliquées : IFRS 9, 15, 16, 17. Cas pratiques de conversion et de reporting financier international selon les standards IASB.",
                CompetencesRequises: "Comptabilité générale",
                CertificationNom: "Diplôme IFRS — ACCA",
                EstStrategique: true,
                EstForteDemande: false),

            new FormationEnrichment(
                "RGPD",
                Plateforme: null,
                ExternalUrl: null,
                Description: "Règlement Général sur la Protection des Données : obligations légales, analyses d'impact (AIPD), gestion des violations et rôles DPO. Formation obligatoire.",
                CompetencesRequises: null,
                CertificationNom: "Certification DPO — CNIL",
                EstStrategique: false,
                EstForteDemande: false),

            new FormationEnrichment(
                "Leadership",
                Plateforme: "Udemy",
                ExternalUrl: "https://www.udemy.com/course/leadership-and-management-skills/",
                Description: "Développez votre style de leadership, apprenez à mobiliser et motiver vos équipes, gérer les conflits et conduire le changement organisationnel.",
                CompetencesRequises: "Gestion d'équipe",
                CertificationNom: null,
                EstStrategique: true,
                EstForteDemande: false),

            new FormationEnrichment(
                "changement",
                Plateforme: "Coursera",
                ExternalUrl: "https://www.coursera.org/learn/change-management",
                Description: "Méthodologies de conduite du changement (Kotter, Prosci ADKAR). Planification, communication, gestion de la résistance et ancrage des nouvelles pratiques.",
                CompetencesRequises: "Leadership, Communication",
                CertificationNom: "Prosci Change Management Certification",
                EstStrategique: true,
                EstForteDemande: false),

            // Enterprise Demo seeder formations
            new FormationEnrichment(
                "Cloud",
                Plateforme: "AWS Skill Builder",
                ExternalUrl: "https://skillbuilder.aws/",
                Description: "Architecture Cloud moderne : DevOps, CI/CD, conteneurisation (Docker/Kubernetes), Infrastructure as Code (Terraform) et déploiement sur AWS, Azure et GCP.",
                CompetencesRequises: "Linux, Réseaux TCP/IP",
                CertificationNom: "AWS Solutions Architect Associate",
                EstStrategique: true,
                EstForteDemande: true),

            new FormationEnrichment(
                "Machine Learning",
                Plateforme: "Coursera",
                ExternalUrl: "https://www.coursera.org/specializations/machine-learning-introduction",
                Description: "Introduction au Machine Learning appliqué aux services financiers : détection de fraude, scoring crédit, analyse prédictive et NLP. Utilisation de Python et scikit-learn.",
                CompetencesRequises: "Python, Statistiques, SQL",
                CertificationNom: "DeepLearning.AI Machine Learning Specialization",
                EstStrategique: true,
                EstForteDemande: true),

            new FormationEnrichment(
                "ISO 27001",
                Plateforme: null,
                ExternalUrl: null,
                Description: "Implémentation et management d'un SMSI selon ISO/IEC 27001. Analyse de risques, politiques de sécurité, audits internes et préparation à la certification.",
                CompetencesRequises: "Réseaux, Sécurité informatique",
                CertificationNom: "ISO 27001 Lead Implementer — PECB",
                EstStrategique: true,
                EstForteDemande: false),

            new FormationEnrichment(
                "Prosci",
                Plateforme: null,
                ExternalUrl: null,
                Description: "Certification Prosci ADKAR : modèle de changement individuel et organisationnel, plan de conduite du changement, rôles de sponsor et agent du changement.",
                CompetencesRequises: "Management de projet",
                CertificationNom: "Prosci Certified Change Practitioner",
                EstStrategique: true,
                EstForteDemande: false),

            new FormationEnrichment(
                "Tableau",
                Plateforme: "LinkedIn Learning",
                ExternalUrl: "https://www.linkedin.com/learning/topics/tableau",
                Description: "Data Visualization avec Tableau Desktop et Power BI : conception de dashboards executives, storytelling avec les données, bonnes pratiques de visualisation.",
                CompetencesRequises: "Excel, SQL",
                CertificationNom: "Tableau Desktop Specialist",
                EstStrategique: false,
                EstForteDemande: true),

            new FormationEnrichment(
                "transfert",
                Plateforme: null,
                ExternalUrl: null,
                Description: "Fiscalité internationale et prix de transfert : réglementation OCDE, documentation des politiques de prix de transfert, méthodes et contentieux.",
                CompetencesRequises: "Fiscalité, Comptabilité",
                CertificationNom: null,
                EstStrategique: true,
                EstForteDemande: false),

            new FormationEnrichment(
                "Design Thinking",
                Plateforme: "Coursera",
                ExternalUrl: "https://www.coursera.org/learn/design-thinking-innovation",
                Description: "Méthodes d'innovation centrées sur l'utilisateur : empathie, idéation, prototypage rapide et test. Application aux processus RH et à la transformation des services.",
                CompetencesRequises: null,
                CertificationNom: null,
                EstStrategique: false,
                EstForteDemande: false),
        };

        var formations = await context.Formations.ToListAsync();
        bool anyChange = false;

        foreach (var formation in formations)
        {
            var match = enrichments.FirstOrDefault(e =>
                formation.Titre.Contains(e.TitreContient, StringComparison.OrdinalIgnoreCase));

            if (match == null) continue;

            bool changed = false;

            if (string.IsNullOrEmpty(formation.Plateforme) && match.Plateforme != null)
            { formation.Plateforme = match.Plateforme; changed = true; }

            if (string.IsNullOrEmpty(formation.ExternalUrl) && match.ExternalUrl != null)
            { formation.ExternalUrl = match.ExternalUrl; changed = true; }

            if (string.IsNullOrEmpty(formation.Description) && match.Description != null)
            { formation.Description = match.Description; changed = true; }

            if (string.IsNullOrEmpty(formation.CompetencesRequises) && match.CompetencesRequises != null)
            { formation.CompetencesRequises = match.CompetencesRequises; changed = true; }

            if (string.IsNullOrEmpty(formation.CertificationNom) && match.CertificationNom != null)
            { formation.CertificationNom = match.CertificationNom; changed = true; }

            if (!formation.EstStrategique && match.EstStrategique)
            { formation.EstStrategique = true; changed = true; }

            if (!formation.EstForteDemande && match.EstForteDemande)
            { formation.EstForteDemande = true; changed = true; }

            if (changed)
            {
                context.Formations.Update(formation);
                anyChange = true;
            }
        }

        if (anyChange)
            await context.SaveChangesAsync();

        context.Parametres.Add(new Parametre { Code = VersionKey, Valeur = DateTime.UtcNow.ToString("O") });
        await context.SaveChangesAsync();
    }

    private sealed record FormationEnrichment(
        string TitreContient,
        string? Plateforme,
        string? ExternalUrl,
        string? Description,
        string? CompetencesRequises,
        string? CertificationNom,
        bool EstStrategique,
        bool EstForteDemande);
}
