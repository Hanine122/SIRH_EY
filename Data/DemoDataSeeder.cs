using Microsoft.EntityFrameworkCore;
using SIRH.EY.Models;

namespace SIRH.EY.Data;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Ensure schema is up-to-date
        if (context.Database.GetMigrations().Any())
            await context.Database.MigrateAsync();
        else
            await context.Database.EnsureCreatedAsync();

        // Idempotent demo seed (skip if already seeded)
        const string seedVersion = "DEMO_SEED_VERSION_2026_04_28";
        if (await context.Parametres.AnyAsync(p => p.Code == seedVersion))
            return;

        // --- Collaborateurs (réalistes, uniques) ---
        var collaborateurs = new List<Collaborateur>
        {
            new() { Prenom = "Hanine", Nom = "Hammami", Email = "hanine.hammami@ey.com", Departement = "RH", Poste = "HR Director", Grade = "Manager", Actif = true, DateEmbauche = DateTime.Today.AddYears(-9) },
            new() { Prenom = "Smiäi", Nom = "Nour", Email = "smiai.nour@ey.com", Departement = "Tax", Poste = "Data Analyst", Grade = "Senior", Actif = true, DateEmbauche = DateTime.Today.AddYears(-4) },
            new() { Prenom = "Mariem", Nom = "Safri", Email = "mariem.safri@ey.com", Departement = "Audit", Poste = "Senior Auditor", Grade = "Senior", Actif = true, DateEmbauche = DateTime.Today.AddYears(-5) },
            new() { Prenom = "Raed", Nom = "Amri", Email = "raed.amri@ey.com", Departement = "Consulting", Poste = "Consultant", Grade = "Junior", Actif = true, DateEmbauche = DateTime.Today.AddMonths(-10) },
            new() { Prenom = "Ayoub", Nom = "Gomra", Email = "ayoub.gomra@ey.com", Departement = "Tax", Poste = "Consultant", Grade = "Junior", Actif = true, DateEmbauche = DateTime.Today.AddMonths(-11) },
            new() { Prenom = "Chloé", Nom = "Ben Youssef", Email = "chloe.benyoussef@ey.com", Departement = "Audit", Poste = "Audit Manager", Grade = "Manager", Actif = true, DateEmbauche = DateTime.Today.AddYears(-7) },
            new() { Prenom = "Sofien", Nom = "Klaou", Email = "sofien.klaou@ey.com", Departement = "Advisory", Poste = "Senior Consultant", Grade = "Senior", Actif = true, DateEmbauche = DateTime.Today.AddYears(-3) },
            new() { Prenom = "Léa", Nom = "Ben Ali", Email = "lea.benali@ey.com", Departement = "Risk", Poste = "Risk Manager", Grade = "Manager", Actif = true, DateEmbauche = DateTime.Today.AddYears(-6) }
        };
        context.Collaborateurs.AddRange(collaborateurs);
        await context.SaveChangesAsync();

        var collabs = await context.Collaborateurs.ToListAsync();
        var managerAudit = collabs.First(c => c.Poste == "Audit Manager").Id;
        foreach (var c in collabs.Where(c => c.Departement == "Audit" && c.Poste != "Audit Manager"))
            c.ManagerId = managerAudit;
        await context.SaveChangesAsync();

        // --- Référentiel compétences requises par poste (NiveauRequis 1..5) ---
        var referentiel = new List<CompetenceRequiseParPoste>
        {
            // Consultant (générique)
            new() { Poste = "Consultant", Competence = "Communication", NiveauRequis = 4 },
            new() { Poste = "Consultant", Competence = "Excel avancé", NiveauRequis = 4 },
            new() { Poste = "Consultant", Competence = "Gestion de projet", NiveauRequis = 3 },
            new() { Poste = "Consultant", Competence = "Analyse & résolution de problèmes", NiveauRequis = 4 },

            // Senior Auditor
            new() { Poste = "Senior Auditor", Competence = "Audit & contrôle interne", NiveauRequis = 4 },
            new() { Poste = "Senior Auditor", Competence = "IFRS / normes comptables", NiveauRequis = 3 },
            new() { Poste = "Senior Auditor", Competence = "Communication", NiveauRequis = 4 },
            new() { Poste = "Senior Auditor", Competence = "Gestion des risques", NiveauRequis = 3 },

            // Data Analyst
            new() { Poste = "Data Analyst", Competence = "Power BI", NiveauRequis = 4 },
            new() { Poste = "Data Analyst", Competence = "SQL", NiveauRequis = 3 },
            new() { Poste = "Data Analyst", Competence = "Modélisation de données", NiveauRequis = 3 },
            new() { Poste = "Data Analyst", Competence = "Data storytelling", NiveauRequis = 3 },

            // Manager
            new() { Poste = "Audit Manager", Competence = "Leadership", NiveauRequis = 4 },
            new() { Poste = "Audit Manager", Competence = "Gestion de projet", NiveauRequis = 4 },
            new() { Poste = "Audit Manager", Competence = "Stakeholder management", NiveauRequis = 4 },
            new() { Poste = "Audit Manager", Competence = "Quality review (audit)", NiveauRequis = 4 },

            // Risk Manager
            new() { Poste = "Risk Manager", Competence = "Risk assessment", NiveauRequis = 4 },
            new() { Poste = "Risk Manager", Competence = "RGPD & conformité", NiveauRequis = 4 },
            new() { Poste = "Risk Manager", Competence = "Communication", NiveauRequis = 4 },
            new() { Poste = "Risk Manager", Competence = "Change management", NiveauRequis = 3 },

            // HR Director
            new() { Poste = "HR Director", Competence = "Leadership", NiveauRequis = 4 },
            new() { Poste = "HR Director", Competence = "Communication", NiveauRequis = 5 },
            new() { Poste = "HR Director", Competence = "Gestion des talents", NiveauRequis = 4 },
            new() { Poste = "HR Director", Competence = "Conduite du changement", NiveauRequis = 4 }
        };
        context.CompetencesRequisesParPoste.AddRange(referentiel);

        // --- Formations (liées aux compétences) ---
        var formations = new List<Formation>
        {
            new() { Titre = "Communication impactante & feedback", Formateur = "Centre EY Learning", DureeHeures = 6, DateDebut = DateTime.Today.AddDays(7), Categorie = "Soft skills", Organisme = "EY Learning", CompetenceVisee = "Communication", CapaciteMax = 25, PlacesPrises = 3 },
            new() { Titre = "Excel avancé pour le conseil", Formateur = "Centre EY Learning", DureeHeures = 8, DateDebut = DateTime.Today.AddDays(14), Categorie = "Outils", Organisme = "EY Learning", CompetenceVisee = "Excel avancé", CapaciteMax = 20, PlacesPrises = 6 },
            new() { Titre = "Gestion de projet Agile (Scrum)", Formateur = "Centre EY Learning", DureeHeures = 12, DateDebut = DateTime.Today.AddDays(10), Categorie = "Management", Organisme = "EY Learning", CompetenceVisee = "Gestion de projet", CapaciteMax = 18, PlacesPrises = 7 },
            new() { Titre = "Power BI — dashboards & DAX", Formateur = "Centre EY Learning", DureeHeures = 10, DateDebut = DateTime.Today.AddDays(5), Categorie = "Data", Organisme = "EY Learning", CompetenceVisee = "Power BI", CapaciteMax = 18, PlacesPrises = 9 },
            new() { Titre = "SQL — requêtes et optimisation", Formateur = "Centre EY Learning", DureeHeures = 10, DateDebut = DateTime.Today.AddDays(21), Categorie = "Data", Organisme = "EY Learning", CompetenceVisee = "SQL", CapaciteMax = 16, PlacesPrises = 4 },
            new() { Titre = "Audit & contrôle interne — fondamentaux", Formateur = "Centre EY Learning", DureeHeures = 14, DateDebut = DateTime.Today.AddDays(2), Categorie = "Audit", Organisme = "EY Learning", CompetenceVisee = "Audit & contrôle interne", CapaciteMax = 22, PlacesPrises = 8 },
            new() { Titre = "IFRS — principes clés", Formateur = "Centre EY Learning", DureeHeures = 10, DateDebut = DateTime.Today.AddDays(18), Categorie = "Audit", Organisme = "EY Learning", CompetenceVisee = "IFRS / normes comptables", CapaciteMax = 22, PlacesPrises = 5 },
            new() { Titre = "RGPD & conformité — mise en pratique", Formateur = "Centre EY Learning", DureeHeures = 8, DateDebut = DateTime.Today.AddDays(12), Categorie = "Risk", Organisme = "EY Learning", CompetenceVisee = "RGPD & conformité", CapaciteMax = 20, PlacesPrises = 11 },
            new() { Titre = "Leadership — piloter une équipe", Formateur = "Centre EY Learning", DureeHeures = 12, DateDebut = DateTime.Today.AddDays(30), Categorie = "Leadership", Organisme = "EY Learning", CompetenceVisee = "Leadership", CapaciteMax = 16, PlacesPrises = 2 },
            new() { Titre = "Change management — outils & conduite", Formateur = "Centre EY Learning", DureeHeures = 10, DateDebut = DateTime.Today.AddDays(25), Categorie = "Management", Organisme = "EY Learning", CompetenceVisee = "Change management", CapaciteMax = 18, PlacesPrises = 3 }
        };
        context.Formations.AddRange(formations);

        await context.SaveChangesAsync();

        // --- Compétences (variées, propres) ---
        var byEmail = collabs.ToDictionary(c => c.Email);
        var now = DateTime.Today;

        List<Competence> comps =
        [
            // Hanine (RH)
            new() { CollaborateurId = byEmail["hanine.hammami@ey.com"].Id, Nom = "Communication", Categorie = "Soft skills", NiveauActuel = 4, NiveauCible = 5, DateEvaluation = now.AddDays(-12) },
            new() { CollaborateurId = byEmail["hanine.hammami@ey.com"].Id, Nom = "Leadership", Categorie = "Leadership", NiveauActuel = 4, NiveauCible = 5, DateEvaluation = now.AddDays(-20) },
            new() { CollaborateurId = byEmail["hanine.hammami@ey.com"].Id, Nom = "Gestion des talents", Categorie = "RH", NiveauActuel = 4, NiveauCible = 4, DateEvaluation = now.AddDays(-9) },
            new() { CollaborateurId = byEmail["hanine.hammami@ey.com"].Id, Nom = "Conduite du changement", Categorie = "Management", NiveauActuel = 3, NiveauCible = 4, DateEvaluation = now.AddDays(-8) },

            // Smiäi (Data)
            new() { CollaborateurId = byEmail["smiai.nour@ey.com"].Id, Nom = "Power BI", Categorie = "Data", NiveauActuel = 4, NiveauCible = 4, DateEvaluation = now.AddDays(-7) },
            new() { CollaborateurId = byEmail["smiai.nour@ey.com"].Id, Nom = "SQL", Categorie = "Data", NiveauActuel = 3, NiveauCible = 4, DateEvaluation = now.AddDays(-10) },
            new() { CollaborateurId = byEmail["smiai.nour@ey.com"].Id, Nom = "Modélisation de données", Categorie = "Data", NiveauActuel = 3, NiveauCible = 4, DateEvaluation = now.AddDays(-14) },
            new() { CollaborateurId = byEmail["smiai.nour@ey.com"].Id, Nom = "Data storytelling", Categorie = "Soft skills", NiveauActuel = 2, NiveauCible = 3, DateEvaluation = now.AddDays(-6) },

            // Mariem (Audit)
            new() { CollaborateurId = byEmail["mariem.safri@ey.com"].Id, Nom = "Audit & contrôle interne", Categorie = "Audit", NiveauActuel = 4, NiveauCible = 4, DateEvaluation = now.AddDays(-11) },
            new() { CollaborateurId = byEmail["mariem.safri@ey.com"].Id, Nom = "IFRS / normes comptables", Categorie = "Audit", NiveauActuel = 3, NiveauCible = 4, DateEvaluation = now.AddDays(-18) },
            new() { CollaborateurId = byEmail["mariem.safri@ey.com"].Id, Nom = "Communication", Categorie = "Soft skills", NiveauActuel = 3, NiveauCible = 4, DateEvaluation = now.AddDays(-9) },
            new() { CollaborateurId = byEmail["mariem.safri@ey.com"].Id, Nom = "Gestion des risques", Categorie = "Risk", NiveauActuel = 3, NiveauCible = 3, DateEvaluation = now.AddDays(-15) },

            // Raed (Consulting)
            new() { CollaborateurId = byEmail["raed.amri@ey.com"].Id, Nom = "Communication", Categorie = "Soft skills", NiveauActuel = 2, NiveauCible = 4, DateEvaluation = now.AddDays(-5) },
            new() { CollaborateurId = byEmail["raed.amri@ey.com"].Id, Nom = "Excel avancé", Categorie = "Outils", NiveauActuel = 3, NiveauCible = 4, DateEvaluation = now.AddDays(-16) },
            new() { CollaborateurId = byEmail["raed.amri@ey.com"].Id, Nom = "Gestion de projet", Categorie = "Management", NiveauActuel = 2, NiveauCible = 3, DateEvaluation = now.AddDays(-12) },
            new() { CollaborateurId = byEmail["raed.amri@ey.com"].Id, Nom = "Analyse & résolution de problèmes", Categorie = "Méthodes", NiveauActuel = 3, NiveauCible = 4, DateEvaluation = now.AddDays(-7) },

            // Ayoub (Tax)
            new() { CollaborateurId = byEmail["ayoub.gomra@ey.com"].Id, Nom = "Communication", Categorie = "Soft skills", NiveauActuel = 2, NiveauCible = 4, DateEvaluation = now.AddDays(-6) },
            new() { CollaborateurId = byEmail["ayoub.gomra@ey.com"].Id, Nom = "Excel avancé", Categorie = "Outils", NiveauActuel = 2, NiveauCible = 4, DateEvaluation = now.AddDays(-10) },
            new() { CollaborateurId = byEmail["ayoub.gomra@ey.com"].Id, Nom = "Fiscalité (bases)", Categorie = "Fiscalité", NiveauActuel = 2, NiveauCible = 3, DateEvaluation = now.AddDays(-20) },
            new() { CollaborateurId = byEmail["ayoub.gomra@ey.com"].Id, Nom = "Tax compliance", Categorie = "Fiscalité", NiveauActuel = 2, NiveauCible = 3, DateEvaluation = now.AddDays(-15) },

            // Chloé (Audit Manager)
            new() { CollaborateurId = byEmail["chloe.benyoussef@ey.com"].Id, Nom = "Leadership", Categorie = "Leadership", NiveauActuel = 4, NiveauCible = 4, DateEvaluation = now.AddDays(-22) },
            new() { CollaborateurId = byEmail["chloe.benyoussef@ey.com"].Id, Nom = "Gestion de projet", Categorie = "Management", NiveauActuel = 4, NiveauCible = 4, DateEvaluation = now.AddDays(-14) },
            new() { CollaborateurId = byEmail["chloe.benyoussef@ey.com"].Id, Nom = "Stakeholder management", Categorie = "Management", NiveauActuel = 4, NiveauCible = 4, DateEvaluation = now.AddDays(-10) },
            new() { CollaborateurId = byEmail["chloe.benyoussef@ey.com"].Id, Nom = "Quality review (audit)", Categorie = "Audit", NiveauActuel = 4, NiveauCible = 4, DateEvaluation = now.AddDays(-16) },

            // Sofien (Advisory)
            new() { CollaborateurId = byEmail["sofien.klaou@ey.com"].Id, Nom = "Change management", Categorie = "Management", NiveauActuel = 3, NiveauCible = 4, DateEvaluation = now.AddDays(-9) },
            new() { CollaborateurId = byEmail["sofien.klaou@ey.com"].Id, Nom = "Gestion de projet", Categorie = "Management", NiveauActuel = 3, NiveauCible = 4, DateEvaluation = now.AddDays(-13) },
            new() { CollaborateurId = byEmail["sofien.klaou@ey.com"].Id, Nom = "Communication", Categorie = "Soft skills", NiveauActuel = 3, NiveauCible = 4, DateEvaluation = now.AddDays(-8) },
            new() { CollaborateurId = byEmail["sofien.klaou@ey.com"].Id, Nom = "Analyse & résolution de problèmes", Categorie = "Méthodes", NiveauActuel = 4, NiveauCible = 4, DateEvaluation = now.AddDays(-19) },

            // Léa (Risk Manager)
            new() { CollaborateurId = byEmail["lea.benali@ey.com"].Id, Nom = "Risk assessment", Categorie = "Risk", NiveauActuel = 4, NiveauCible = 4, DateEvaluation = now.AddDays(-12) },
            new() { CollaborateurId = byEmail["lea.benali@ey.com"].Id, Nom = "RGPD & conformité", Categorie = "Risk", NiveauActuel = 4, NiveauCible = 4, DateEvaluation = now.AddDays(-7) },
            new() { CollaborateurId = byEmail["lea.benali@ey.com"].Id, Nom = "Communication", Categorie = "Soft skills", NiveauActuel = 3, NiveauCible = 4, DateEvaluation = now.AddDays(-10) },
            new() { CollaborateurId = byEmail["lea.benali@ey.com"].Id, Nom = "Change management", Categorie = "Management", NiveauActuel = 3, NiveauCible = 3, DateEvaluation = now.AddDays(-18) }
        ];

        context.Competences.AddRange(comps);
        await context.SaveChangesAsync();

        // --- Inscriptions (parcours formation) ---
        var fByCompetence = await context.Formations.ToDictionaryAsync(f => f.CompetenceVisee ?? f.Titre);
        var raedId = byEmail["raed.amri@ey.com"].Id;
        var mariemId = byEmail["mariem.safri@ey.com"].Id;
        var smiaiId = byEmail["smiai.nour@ey.com"].Id;

        var inscriptions = new List<Inscription>
        {
            new() { CollaborateurId = raedId, FormationId = fByCompetence["Gestion de projet"].Id, DateInscription = now.AddDays(-9), Terminee = false, Progression = 40 },
            new() { CollaborateurId = raedId, FormationId = fByCompetence["Excel avancé"].Id, DateInscription = now.AddDays(-20), Terminee = true, Progression = 100 },
            new() { CollaborateurId = smiaiId, FormationId = fByCompetence["Power BI"].Id, DateInscription = now.AddDays(-14), Terminee = true, Progression = 100 },
            new() { CollaborateurId = smiaiId, FormationId = fByCompetence["SQL"].Id, DateInscription = now.AddDays(-6), Terminee = false, Progression = 20, DateExamen = DateTime.Today.AddDays(30).AddHours(10) },
            new() { CollaborateurId = mariemId, FormationId = fByCompetence["Audit & contrôle interne"].Id, DateInscription = now.AddDays(-16), Terminee = true, Progression = 100 }
        };
        context.Inscriptions.AddRange(inscriptions);
        await context.SaveChangesAsync();

        // --- Evaluations (quelques exemples : auto-éval + validation manager) ---
        var compList = await context.Competences
            .Include(c => c.Collaborateur)
            .ToListAsync();

        Competence GetComp(string email, string nom) =>
            compList.First(c => c.Collaborateur!.Email == email && c.Nom == nom);

        var evals = new List<EvaluationCompetence>
        {
            new()
            {
                CompetenceId = GetComp("hanine.hammami@ey.com", "Communication").Id,
                SeuilRh = 80,
                AutoEvaluationCollaborateur = 70,
                DateAutoEvaluation = DateTime.Now.AddDays(-2),
                EvaluationManager = 50,
                ValidationManager = true,
                DateValidationManager = DateTime.Now.AddDays(-1),
                CommentaireCollaborateur = "Je suis à l'aise en réunion, à renforcer en comité.",
                CommentaireManager = "Bon potentiel, mais structuration du message à améliorer."
            },
            new()
            {
                CompetenceId = GetComp("raed.amri@ey.com", "Communication").Id,
                SeuilRh = 80,
                AutoEvaluationCollaborateur = 55,
                DateAutoEvaluation = DateTime.Now.AddDays(-3),
                EvaluationManager = null,
                ValidationManager = false,
                CommentaireCollaborateur = "Je progresse mais je manque d'aisance face client."
            },
            new()
            {
                CompetenceId = GetComp("smiai.nour@ey.com", "Data storytelling").Id,
                SeuilRh = 60,
                AutoEvaluationCollaborateur = 50,
                DateAutoEvaluation = DateTime.Now.AddDays(-4),
                EvaluationManager = 55,
                ValidationManager = false,
                DateValidationManager = DateTime.Now.AddDays(-2),
                CommentaireCollaborateur = "Je sais construire des dashboards, mais narration perfectible.",
                CommentaireManager = "Bonne évolution, poursuivre avec cas clients."
            }
        };
        context.EvaluationsCompetences.AddRange(evals);

        // --- Marker de seed version ---
        context.Parametres.Add(new Parametre
        {
            Code = seedVersion,
            TypeValeur = "string",
            Valeur = DateTime.Now.ToString("O"),
            EstModifiable = false,
            Description = "Marqueur interne pour seed démo"
        });

        await context.SaveChangesAsync();
    }
}
