using Microsoft.EntityFrameworkCore;
using SIRH.EY.Models;

namespace SIRH.EY.Data;

/// <summary>
/// Enterprise demo seeder — Big Four consulting firm profile (Tunisia office).
/// Runs once per database: idempotent, safe to re-deploy.
/// Execution order: Partners → Directors → Senior Managers → Managers → Consultants.
/// </summary>
public static class EnterpriseDemoSeeder
{
    private const string SeedVersionKey = "ENTERPRISE_DEMO_V1_2026_06";

    // ═══════════════════════════════════════════════════════════════════════════
    //  ENTRY POINT
    // ═══════════════════════════════════════════════════════════════════════════

    public static async Task SeedAsync(ApplicationDbContext ctx)
    {
        if (await ctx.Parametres.AnyAsync(p => p.Code == SeedVersionKey))
            return;

        // ── Step 1 · Fetch existing master data ───────────────────────────────
        var gradePartner  = await ctx.Grades.FirstOrDefaultAsync(g => g.Name == "Partner");
        var gradeDirector = await ctx.Grades.FirstOrDefaultAsync(g => g.Name == "Director");
        var gradeSrMgr    = await ctx.Grades.FirstOrDefaultAsync(g => g.Name == "Senior Manager");
        var gradeManager  = await ctx.Grades.FirstOrDefaultAsync(g => g.Name == "Manager");
        var gradeSenior   = await ctx.Grades.FirstOrDefaultAsync(g => g.Name == "Senior");
        var gradeJunior   = await ctx.Grades.FirstOrDefaultAsync(g => g.Name == "Junior");

        var ctCDI         = await ctx.ContractTypes.FirstOrDefaultAsync(c => c.Code == "CDI");

        var locLac1       = await ctx.Locations.FirstOrDefaultAsync(l => l.Name == "Tunis — Lac 1");
        var locLac2       = await ctx.Locations.FirstOrDefaultAsync(l => l.Name == "Tunis — Lac 2");
        var locSfax       = await ctx.Locations.FirstOrDefaultAsync(l => l.Name == "Sfax");
        var locRemote     = await ctx.Locations.FirstOrDefaultAsync(l => l.Name == "Remote");

        var buConsulting  = await ctx.BusinessUnits.FirstOrDefaultAsync(b => b.Name == "Consulting");
        var buAssurance   = await ctx.BusinessUnits.FirstOrDefaultAsync(b => b.Name == "Assurance");
        var buTax         = await ctx.BusinessUnits.FirstOrDefaultAsync(b => b.Name == "Tax");
        var buCBS         = await ctx.BusinessUnits.FirstOrDefaultAsync(b => b.Name == "CBS");
        var buStrategy    = await ctx.BusinessUnits.FirstOrDefaultAsync(b => b.Name == "Strategy & Transactions");

        // ── Step 2 · New departments (reuse existing Assurance, Consulting, Tax) ─
        var dCons  = await GetOrCreateDept(ctx, "Consulting",            "CON",   "Conseil stratégique et opérationnel");
        var dAss   = await GetOrCreateDept(ctx, "Assurance",             "ASS",   "Audit financier et assurance");
        var dTax   = await GetOrCreateDept(ctx, "Tax",                   "TAX",   "Fiscalité et conformité");
        var dTech  = await GetOrCreateDept(ctx, "Technology",            "TECH",  "Conseil en transformation technologique");
        var dData  = await GetOrCreateDept(ctx, "Data & Analytics",      "DATA",  "Data science, BI et analytics avancés");
        var dCyber = await GetOrCreateDept(ctx, "Cybersecurity",         "CYBER", "Sécurité de l'information et cyber-risques");
        var dPeo   = await GetOrCreateDept(ctx, "People Consulting",     "PEO",   "Capital humain, RH et gestion des talents");
        var dBT    = await GetOrCreateDept(ctx, "Business Transformation","BT",   "Transformation des organisations et conduite du changement");

        // ── Step 3 · SubDepartments ───────────────────────────────────────────
        var sdConsDigital = await GetOrCreateSubDept(ctx, "Digital Consulting",       "CON-DIG",  dCons.Id);
        var sdConsOps     = await GetOrCreateSubDept(ctx, "Operations & Strategy",    "CON-OPS",  dCons.Id);
        var sdAssAudit    = await GetOrCreateSubDept(ctx, "Audit Financier EY",       "ASS-AUD",  dAss.Id);
        var sdAssRisk     = await GetOrCreateSubDept(ctx, "Risk Advisory EY",         "ASS-RISK", dAss.Id);
        var sdTaxGlobal   = await GetOrCreateSubDept(ctx, "Global Compliance",        "TAX-GC",   dTax.Id);
        var sdTaxTrans    = await GetOrCreateSubDept(ctx, "Tax Transactions",         "TAX-TRX",  dTax.Id);
        var sdTechCloud   = await GetOrCreateSubDept(ctx, "Cloud & Infrastructure",   "TCH-CLD",  dTech.Id);
        var sdTechERP     = await GetOrCreateSubDept(ctx, "ERP & Enterprise Apps",    "TCH-ERP",  dTech.Id);
        var sdDataSci     = await GetOrCreateSubDept(ctx, "Data Science & AI",        "DAT-SCI",  dData.Id);
        var sdDataBI      = await GetOrCreateSubDept(ctx, "Business Intelligence",    "DAT-BI",   dData.Id);
        var sdCyberSOC    = await GetOrCreateSubDept(ctx, "Security Operations",      "CYB-SOC",  dCyber.Id);
        var sdCyberGov    = await GetOrCreateSubDept(ctx, "Governance & Compliance",  "CYB-GOV",  dCyber.Id);
        var sdPeoTalent   = await GetOrCreateSubDept(ctx, "Talent & Learning",        "PEO-TAL",  dPeo.Id);
        var sdPeoOrg      = await GetOrCreateSubDept(ctx, "Org. Design & Change",     "PEO-ORG",  dPeo.Id);
        var sdBTPMO       = await GetOrCreateSubDept(ctx, "Program Management (PMO)", "BT-PMO",   dBT.Id);
        var sdBTChange    = await GetOrCreateSubDept(ctx, "Change Management",        "BT-CHG",   dBT.Id);

        // ── Step 4 · Positions ────────────────────────────────────────────────
        var posPartner    = await GetOrCreatePosition(ctx, "Partner",                  "PTN",  null);
        var posDirector   = await GetOrCreatePosition(ctx, "Director",                 "DIR",  null);
        var posSrMgr      = await GetOrCreatePosition(ctx, "Senior Manager",           "SMG",  null);
        var posManager    = await GetOrCreatePosition(ctx, "Manager",                  "MGR",  null);
        var posSrCons     = await GetOrCreatePosition(ctx, "Senior Consultant",        "SRC",  null);
        var posConsultant = await GetOrCreatePosition(ctx, "Consultant",               "CNS",  null);
        var posSrAnalyst  = await GetOrCreatePosition(ctx, "Senior Data Analyst",      "SDA",  sdDataBI.Id);
        var posCyberAna   = await GetOrCreatePosition(ctx, "Cybersecurity Analyst",    "CYA",  sdCyberSOC.Id);
        var posAuditMgr   = await GetOrCreatePosition(ctx, "Audit Manager",            "AUM",  sdAssAudit.Id);
        var posTaxMgr     = await GetOrCreatePosition(ctx, "Tax Manager",              "TXM",  sdTaxGlobal.Id);

        await ctx.SaveChangesAsync();

        // ── Step 5 · Partners ─────────────────────────────────────────────────
        await AddCollab(ctx, new Collaborateur
        {
            Nom = "Ben Youssef", Prenom = "Karim",
            Email = "karim.benyoussef@ey.com",
            Adresse = "Les Berges du Lac, Immeuble Carthage, Apt 12",
            Ville = "Tunis", Pays = "Tunisie",
            TelephonePersonnel = "+216 22 100 001",
            Matricule = "EY-PTN-001",
            Grade = "Partner", Departement = "Consulting", Poste = "Partner",
            BusinessUnit = "Consulting", Localisation = "Tunis — Lac 1",
            TypeContrat = "CDI", NiveauHierarchique = "Partner",
            DateEmbauche = new DateTime(2006, 3, 1),
            DatePrisePoste = new DateTime(2014, 9, 1),
            NiveauPreparationSuccession = 98, PotentielCarriere = "Haut potentiel",
            RoleRH = "Partner", Actif = true, Statut = StatutCollaborateur.Actif,
            DepartmentId = dCons.Id, GradeId = gradePartner?.Id,
            BusinessUnitId = buConsulting?.Id, LocationId = locLac1?.Id,
            ContractTypeId = ctCDI?.Id, PositionId = posPartner.Id
        });
        await AddCollab(ctx, new Collaborateur
        {
            Nom = "Trabelsi", Prenom = "Sami",
            Email = "sami.trabelsi@ey.com",
            Adresse = "Résidence Les Pins, Rue des Jasmins 5",
            Ville = "Tunis", Pays = "Tunisie",
            TelephonePersonnel = "+216 22 100 002",
            Matricule = "EY-PTN-002",
            Grade = "Partner", Departement = "Assurance", Poste = "Partner",
            BusinessUnit = "Assurance", Localisation = "Tunis — Lac 1",
            TypeContrat = "CDI", NiveauHierarchique = "Partner",
            DateEmbauche = new DateTime(2005, 9, 15),
            DatePrisePoste = new DateTime(2013, 1, 1),
            NiveauPreparationSuccession = 97, PotentielCarriere = "Haut potentiel",
            RoleRH = "Partner", Actif = true, Statut = StatutCollaborateur.Actif,
            DepartmentId = dAss.Id, GradeId = gradePartner?.Id,
            BusinessUnitId = buAssurance?.Id, LocationId = locLac1?.Id,
            ContractTypeId = ctCDI?.Id, PositionId = posPartner.Id
        });
        await AddCollab(ctx, new Collaborateur
        {
            Nom = "Gharbi", Prenom = "Hatem",
            Email = "hatem.gharbi@ey.com",
            Adresse = "Avenue Habib Bourguiba 78, Bloc C",
            Ville = "Tunis", Pays = "Tunisie",
            TelephonePersonnel = "+216 22 100 003",
            Matricule = "EY-PTN-003",
            Grade = "Partner", Departement = "Tax", Poste = "Partner",
            BusinessUnit = "Tax", Localisation = "Tunis — Lac 1",
            TypeContrat = "CDI", NiveauHierarchique = "Partner",
            DateEmbauche = new DateTime(2007, 6, 1),
            DatePrisePoste = new DateTime(2015, 3, 1),
            NiveauPreparationSuccession = 96, PotentielCarriere = "Haut potentiel",
            RoleRH = "Partner", Actif = true, Statut = StatutCollaborateur.Actif,
            DepartmentId = dTax.Id, GradeId = gradePartner?.Id,
            BusinessUnitId = buTax?.Id, LocationId = locLac1?.Id,
            ContractTypeId = ctCDI?.Id, PositionId = posPartner.Id
        });
        await ctx.SaveChangesAsync();

        var pKarim = await ctx.Collaborateurs.FirstAsync(c => c.Email == "karim.benyoussef@ey.com");
        var pSami  = await ctx.Collaborateurs.FirstAsync(c => c.Email == "sami.trabelsi@ey.com");
        var pHatem = await ctx.Collaborateurs.FirstAsync(c => c.Email == "hatem.gharbi@ey.com");

        // ── Step 6 · Directors ────────────────────────────────────────────────
        await AddCollab(ctx, new Collaborateur
        {
            Nom = "Hammami", Prenom = "Yosra",
            Email = "yosra.hammami@ey.com",
            Adresse = "Résidence Andalous, Menzah 6",
            Ville = "Tunis", Pays = "Tunisie",
            TelephonePersonnel = "+216 25 200 001",
            Matricule = "EY-DIR-001",
            Grade = "Director", Departement = "Technology", Poste = "Director",
            BusinessUnit = "Consulting", Localisation = "Tunis — Lac 1",
            TypeContrat = "CDI", NiveauHierarchique = "Director",
            DateEmbauche = new DateTime(2010, 4, 1),
            DatePrisePoste = new DateTime(2019, 10, 1),
            NiveauPreparationSuccession = 90, PotentielCarriere = "Haut potentiel",
            RoleRH = "Director", Actif = true, Statut = StatutCollaborateur.Actif,
            ManagerId = pKarim.Id,
            DepartmentId = dTech.Id, GradeId = gradeDirector?.Id,
            BusinessUnitId = buConsulting?.Id, LocationId = locLac1?.Id,
            ContractTypeId = ctCDI?.Id, PositionId = posDirector.Id
        });
        await AddCollab(ctx, new Collaborateur
        {
            Nom = "Jlassi", Prenom = "Mehdi",
            Email = "mehdi.jlassi@ey.com",
            Adresse = "Cité Ennasr 2, Rue des Roses 14",
            Ville = "Tunis", Pays = "Tunisie",
            TelephonePersonnel = "+216 25 200 002",
            Matricule = "EY-DIR-002",
            Grade = "Director", Departement = "Data & Analytics", Poste = "Director",
            BusinessUnit = "Consulting", Localisation = "Tunis — Lac 1",
            TypeContrat = "CDI", NiveauHierarchique = "Director",
            DateEmbauche = new DateTime(2011, 9, 1),
            DatePrisePoste = new DateTime(2020, 6, 1),
            NiveauPreparationSuccession = 88, PotentielCarriere = "Haut potentiel",
            RoleRH = "Director", Actif = true, Statut = StatutCollaborateur.Actif,
            ManagerId = pKarim.Id,
            DepartmentId = dData.Id, GradeId = gradeDirector?.Id,
            BusinessUnitId = buConsulting?.Id, LocationId = locLac1?.Id,
            ContractTypeId = ctCDI?.Id, PositionId = posDirector.Id
        });
        await AddCollab(ctx, new Collaborateur
        {
            Nom = "Ben Salem", Prenom = "Nour",
            Email = "nour.bensalem@ey.com",
            Adresse = "Soukra, Résidence El Habib, Bloc B",
            Ville = "Ariana", Pays = "Tunisie",
            TelephonePersonnel = "+216 25 200 003",
            Matricule = "EY-DIR-003",
            Grade = "Director", Departement = "Consulting", Poste = "Director",
            BusinessUnit = "Consulting", Localisation = "Tunis — Lac 2",
            TypeContrat = "CDI", NiveauHierarchique = "Director",
            DateEmbauche = new DateTime(2009, 2, 1),
            DatePrisePoste = new DateTime(2018, 11, 1),
            NiveauPreparationSuccession = 92, PotentielCarriere = "Haut potentiel",
            RoleRH = "Director", Actif = true, Statut = StatutCollaborateur.Actif,
            ManagerId = pKarim.Id,
            DepartmentId = dCons.Id, GradeId = gradeDirector?.Id,
            BusinessUnitId = buConsulting?.Id, LocationId = locLac2?.Id,
            ContractTypeId = ctCDI?.Id, PositionId = posDirector.Id
        });
        await AddCollab(ctx, new Collaborateur
        {
            Nom = "Khelifi", Prenom = "Walid",
            Email = "walid.khelifi@ey.com",
            Adresse = "La Marsa, Rue de la Plage 22",
            Ville = "La Marsa", Pays = "Tunisie",
            TelephonePersonnel = "+216 25 200 004",
            Matricule = "EY-DIR-004",
            Grade = "Director", Departement = "Cybersecurity", Poste = "Director",
            BusinessUnit = "Consulting", Localisation = "Tunis — Lac 1",
            TypeContrat = "CDI", NiveauHierarchique = "Director",
            DateEmbauche = new DateTime(2012, 7, 1),
            DatePrisePoste = new DateTime(2021, 3, 1),
            NiveauPreparationSuccession = 85, PotentielCarriere = "Potentiel solide",
            RoleRH = "Director", Actif = true, Statut = StatutCollaborateur.Actif,
            ManagerId = pHatem.Id,
            DepartmentId = dCyber.Id, GradeId = gradeDirector?.Id,
            BusinessUnitId = buConsulting?.Id, LocationId = locLac1?.Id,
            ContractTypeId = ctCDI?.Id, PositionId = posDirector.Id
        });
        await AddCollab(ctx, new Collaborateur
        {
            Nom = "Chebbi", Prenom = "Rania",
            Email = "rania.chebbi@ey.com",
            Adresse = "Jardins de Carthage, Apt 7B",
            Ville = "Tunis", Pays = "Tunisie",
            TelephonePersonnel = "+216 25 200 005",
            Matricule = "EY-DIR-005",
            Grade = "Director", Departement = "People Consulting", Poste = "Director",
            BusinessUnit = "CBS", Localisation = "Tunis — Lac 1",
            TypeContrat = "CDI", NiveauHierarchique = "Director",
            DateEmbauche = new DateTime(2013, 1, 15),
            DatePrisePoste = new DateTime(2020, 9, 1),
            NiveauPreparationSuccession = 87, PotentielCarriere = "Haut potentiel",
            RoleRH = "Director", Actif = true, Statut = StatutCollaborateur.Actif,
            ManagerId = pSami.Id,
            DepartmentId = dPeo.Id, GradeId = gradeDirector?.Id,
            BusinessUnitId = buCBS?.Id, LocationId = locLac1?.Id,
            ContractTypeId = ctCDI?.Id, PositionId = posDirector.Id
        });
        await ctx.SaveChangesAsync();

        var dYosra  = await ctx.Collaborateurs.FirstAsync(c => c.Email == "yosra.hammami@ey.com");
        var dMehdi  = await ctx.Collaborateurs.FirstAsync(c => c.Email == "mehdi.jlassi@ey.com");
        var dNour   = await ctx.Collaborateurs.FirstAsync(c => c.Email == "nour.bensalem@ey.com");
        var dWalid  = await ctx.Collaborateurs.FirstAsync(c => c.Email == "walid.khelifi@ey.com");
        var dRania  = await ctx.Collaborateurs.FirstAsync(c => c.Email == "rania.chebbi@ey.com");

        // ── Step 7 · Senior Managers & Managers ───────────────────────────────
        await AddCollab(ctx, new Collaborateur
        {
            Nom = "Trabelsi", Prenom = "Aymen", Email = "aymen.trabelsi@ey.com",
            Adresse = "Cité Olympic, Bloc 3 Apt 21", Ville = "Tunis", Pays = "Tunisie",
            TelephonePersonnel = "+216 27 300 001", Matricule = "EY-SMG-001",
            Grade = "Senior Manager", Departement = "Technology", Poste = "Senior Manager",
            BusinessUnit = "Consulting", Localisation = "Tunis — Lac 1",
            TypeContrat = "CDI", NiveauHierarchique = "Senior Manager",
            DateEmbauche = new DateTime(2014, 5, 1), DatePrisePoste = new DateTime(2021, 9, 1),
            NiveauPreparationSuccession = 80, PotentielCarriere = "Haut potentiel",
            RoleRH = "Manager", Actif = true, Statut = StatutCollaborateur.Actif,
            ManagerId = dYosra.Id,
            DepartmentId = dTech.Id, GradeId = gradeSrMgr?.Id,
            BusinessUnitId = buConsulting?.Id, LocationId = locLac1?.Id,
            ContractTypeId = ctCDI?.Id, PositionId = posSrMgr.Id
        });
        await AddCollab(ctx, new Collaborateur
        {
            Nom = "Ben Ali", Prenom = "Sarra", Email = "sarra.benali@ey.com",
            Adresse = "El Menzah 9, Résidence Les Lauriers", Ville = "Tunis", Pays = "Tunisie",
            TelephonePersonnel = "+216 27 300 002", Matricule = "EY-SMG-002",
            Grade = "Senior Manager", Departement = "Data & Analytics", Poste = "Senior Manager",
            BusinessUnit = "Consulting", Localisation = "Tunis — Lac 1",
            TypeContrat = "CDI", NiveauHierarchique = "Senior Manager",
            DateEmbauche = new DateTime(2015, 3, 1), DatePrisePoste = new DateTime(2022, 4, 1),
            NiveauPreparationSuccession = 75, PotentielCarriere = "Haut potentiel",
            RoleRH = "Manager", Actif = true, Statut = StatutCollaborateur.Actif,
            ManagerId = dMehdi.Id,
            DepartmentId = dData.Id, GradeId = gradeSrMgr?.Id,
            BusinessUnitId = buConsulting?.Id, LocationId = locLac1?.Id,
            ContractTypeId = ctCDI?.Id, PositionId = posSrMgr.Id
        });
        await AddCollab(ctx, new Collaborateur
        {
            Nom = "Jebali", Prenom = "Amine", Email = "amine.jebali@ey.com",
            Adresse = "Rue Ibn Khaldoun 33, Belvédère", Ville = "Tunis", Pays = "Tunisie",
            TelephonePersonnel = "+216 27 300 003", Matricule = "EY-MGR-001",
            Grade = "Manager", Departement = "Consulting", Poste = "Manager",
            BusinessUnit = "Consulting", Localisation = "Tunis — Lac 2",
            TypeContrat = "CDI", NiveauHierarchique = "Manager",
            DateEmbauche = new DateTime(2016, 9, 1), DatePrisePoste = new DateTime(2022, 1, 1),
            NiveauPreparationSuccession = 65, PotentielCarriere = "Potentiel solide",
            RoleRH = "Manager", Actif = true, Statut = StatutCollaborateur.Actif,
            ManagerId = dNour.Id,
            DepartmentId = dCons.Id, GradeId = gradeManager?.Id,
            BusinessUnitId = buConsulting?.Id, LocationId = locLac2?.Id,
            ContractTypeId = ctCDI?.Id, PositionId = posManager.Id
        });
        await AddCollab(ctx, new Collaborateur
        {
            Nom = "Gharbi", Prenom = "Meriem", Email = "meriem.gharbi@ey.com",
            Adresse = "Avenue Farhat Hached 15, Bab Saadoun", Ville = "Tunis", Pays = "Tunisie",
            TelephonePersonnel = "+216 27 300 004", Matricule = "EY-MGR-002",
            Grade = "Manager", Departement = "Cybersecurity", Poste = "Manager",
            BusinessUnit = "Consulting", Localisation = "Tunis — Lac 1",
            TypeContrat = "CDI", NiveauHierarchique = "Manager",
            DateEmbauche = new DateTime(2017, 2, 1), DatePrisePoste = new DateTime(2022, 11, 1),
            NiveauPreparationSuccession = 60, PotentielCarriere = "Potentiel solide",
            RoleRH = "Manager", Actif = true, Statut = StatutCollaborateur.Actif,
            ManagerId = dWalid.Id,
            DepartmentId = dCyber.Id, GradeId = gradeManager?.Id,
            BusinessUnitId = buConsulting?.Id, LocationId = locLac1?.Id,
            ContractTypeId = ctCDI?.Id, PositionId = posManager.Id
        });
        await AddCollab(ctx, new Collaborateur
        {
            Nom = "Ben Salah", Prenom = "Omar", Email = "omar.bensalah@ey.com",
            Adresse = "Cité El Manar 1, Apt 44", Ville = "Tunis", Pays = "Tunisie",
            TelephonePersonnel = "+216 27 300 005", Matricule = "EY-SMG-003",
            Grade = "Senior Manager", Departement = "Assurance", Poste = "Audit Manager",
            BusinessUnit = "Assurance", Localisation = "Tunis — Lac 1",
            TypeContrat = "CDI", NiveauHierarchique = "Senior Manager",
            DateEmbauche = new DateTime(2013, 11, 1), DatePrisePoste = new DateTime(2021, 6, 1),
            NiveauPreparationSuccession = 78, PotentielCarriere = "Potentiel solide",
            RoleRH = "Manager", Actif = true, Statut = StatutCollaborateur.Actif,
            ManagerId = pSami.Id,
            DepartmentId = dAss.Id, GradeId = gradeSrMgr?.Id,
            BusinessUnitId = buAssurance?.Id, LocationId = locLac1?.Id,
            ContractTypeId = ctCDI?.Id, PositionId = posAuditMgr.Id
        });
        await AddCollab(ctx, new Collaborateur
        {
            Nom = "Kooli", Prenom = "Yasmine", Email = "yasmine.kooli@ey.com",
            Adresse = "Mutuelleville, Rue de l'Inde 8", Ville = "Tunis", Pays = "Tunisie",
            TelephonePersonnel = "+216 27 300 006", Matricule = "EY-MGR-003",
            Grade = "Manager", Departement = "People Consulting", Poste = "Manager",
            BusinessUnit = "CBS", Localisation = "Tunis — Lac 1",
            TypeContrat = "CDI", NiveauHierarchique = "Manager",
            DateEmbauche = new DateTime(2018, 6, 1), DatePrisePoste = new DateTime(2023, 2, 1),
            NiveauPreparationSuccession = 58, PotentielCarriere = "Haut potentiel",
            RoleRH = "Manager", Actif = true, Statut = StatutCollaborateur.Actif,
            ManagerId = dRania.Id,
            DepartmentId = dPeo.Id, GradeId = gradeManager?.Id,
            BusinessUnitId = buCBS?.Id, LocationId = locLac1?.Id,
            ContractTypeId = ctCDI?.Id, PositionId = posManager.Id
        });
        await AddCollab(ctx, new Collaborateur
        {
            Nom = "Hammami", Prenom = "Nidhal", Email = "nidhal.hammami@ey.com",
            Adresse = "Hammam Lif, Rue Habib Bourguiba 44", Ville = "Ben Arous", Pays = "Tunisie",
            TelephonePersonnel = "+216 27 300 007", Matricule = "EY-MGR-004",
            Grade = "Manager", Departement = "Business Transformation", Poste = "Manager",
            BusinessUnit = "Strategy & Transactions", Localisation = "Tunis — Lac 2",
            TypeContrat = "CDI", NiveauHierarchique = "Manager",
            DateEmbauche = new DateTime(2017, 8, 1), DatePrisePoste = new DateTime(2023, 1, 1),
            NiveauPreparationSuccession = 55, PotentielCarriere = "Potentiel solide",
            RoleRH = "Manager", Actif = true, Statut = StatutCollaborateur.Actif,
            ManagerId = pKarim.Id,
            DepartmentId = dBT.Id, GradeId = gradeManager?.Id,
            BusinessUnitId = buStrategy?.Id, LocationId = locLac2?.Id,
            ContractTypeId = ctCDI?.Id, PositionId = posManager.Id
        });
        await AddCollab(ctx, new Collaborateur
        {
            Nom = "Triki", Prenom = "Rim", Email = "rim.triki@ey.com",
            Adresse = "Ennasr, Résidence Jasmin, Apt 12", Ville = "Ariana", Pays = "Tunisie",
            TelephonePersonnel = "+216 27 300 008", Matricule = "EY-MGR-005",
            Grade = "Manager", Departement = "Tax", Poste = "Tax Manager",
            BusinessUnit = "Tax", Localisation = "Tunis — Lac 1",
            TypeContrat = "CDI", NiveauHierarchique = "Manager",
            DateEmbauche = new DateTime(2016, 4, 1), DatePrisePoste = new DateTime(2022, 7, 1),
            NiveauPreparationSuccession = 62, PotentielCarriere = "Potentiel solide",
            RoleRH = "Manager", Actif = true, Statut = StatutCollaborateur.Actif,
            ManagerId = pHatem.Id,
            DepartmentId = dTax.Id, GradeId = gradeManager?.Id,
            BusinessUnitId = buTax?.Id, LocationId = locLac1?.Id,
            ContractTypeId = ctCDI?.Id, PositionId = posTaxMgr.Id
        });
        await AddCollab(ctx, new Collaborateur
        {
            Nom = "Ben Amor", Prenom = "Houssem", Email = "houssem.benamor@ey.com",
            Adresse = "La Soukra, Rue des Pommiers 7", Ville = "Ariana", Pays = "Tunisie",
            TelephonePersonnel = "+216 27 300 009", Matricule = "EY-MGR-006",
            Grade = "Manager", Departement = "Technology", Poste = "Manager",
            BusinessUnit = "Consulting", Localisation = "Tunis — Lac 1",
            TypeContrat = "CDI", NiveauHierarchique = "Manager",
            DateEmbauche = new DateTime(2017, 1, 15), DatePrisePoste = new DateTime(2022, 9, 1),
            NiveauPreparationSuccession = 63, PotentielCarriere = "Potentiel solide",
            RoleRH = "Manager", Actif = true, Statut = StatutCollaborateur.Actif,
            ManagerId = dYosra.Id,
            DepartmentId = dTech.Id, GradeId = gradeManager?.Id,
            BusinessUnitId = buConsulting?.Id, LocationId = locLac1?.Id,
            ContractTypeId = ctCDI?.Id, PositionId = posManager.Id
        });
        await AddCollab(ctx, new Collaborateur
        {
            Nom = "Ben Mustapha", Prenom = "Dorra", Email = "dorra.benmustapha@ey.com",
            Adresse = "Cité Riadh, Manouba, Apt 5", Ville = "Manouba", Pays = "Tunisie",
            TelephonePersonnel = "+216 27 300 010", Matricule = "EY-MGR-007",
            Grade = "Manager", Departement = "Data & Analytics", Poste = "Manager",
            BusinessUnit = "Consulting", Localisation = "Tunis — Lac 1",
            TypeContrat = "CDI", NiveauHierarchique = "Manager",
            DateEmbauche = new DateTime(2018, 3, 1), DatePrisePoste = new DateTime(2023, 4, 1),
            NiveauPreparationSuccession = 57, PotentielCarriere = "Haut potentiel",
            RoleRH = "Manager", Actif = true, Statut = StatutCollaborateur.Actif,
            ManagerId = dMehdi.Id,
            DepartmentId = dData.Id, GradeId = gradeManager?.Id,
            BusinessUnitId = buConsulting?.Id, LocationId = locLac1?.Id,
            ContractTypeId = ctCDI?.Id, PositionId = posManager.Id
        });
        await ctx.SaveChangesAsync();

        var mAymen   = await ctx.Collaborateurs.FirstAsync(c => c.Email == "aymen.trabelsi@ey.com");
        var mSarra   = await ctx.Collaborateurs.FirstAsync(c => c.Email == "sarra.benali@ey.com");
        var mAmine   = await ctx.Collaborateurs.FirstAsync(c => c.Email == "amine.jebali@ey.com");
        var mMeriem  = await ctx.Collaborateurs.FirstAsync(c => c.Email == "meriem.gharbi@ey.com");
        var mOmar    = await ctx.Collaborateurs.FirstAsync(c => c.Email == "omar.bensalah@ey.com");
        var mYasmine = await ctx.Collaborateurs.FirstAsync(c => c.Email == "yasmine.kooli@ey.com");
        var mNidhal  = await ctx.Collaborateurs.FirstAsync(c => c.Email == "nidhal.hammami@ey.com");
        var mRim     = await ctx.Collaborateurs.FirstAsync(c => c.Email == "rim.triki@ey.com");
        var mHoussem = await ctx.Collaborateurs.FirstAsync(c => c.Email == "houssem.benamor@ey.com");
        var mDorra   = await ctx.Collaborateurs.FirstAsync(c => c.Email == "dorra.benmustapha@ey.com");

        // ── Step 8 · Senior Consultants & Consultants ─────────────────────────
        var juniors = new[]
        {
            new Collaborateur { Nom="Belhadj",     Prenom="Aziz",    Email="aziz.belhadj@ey.com",
                Adresse="Rue Ibn Rachiq 17, Lafayette",    Ville="Tunis",      Pays="Tunisie",
                TelephonePersonnel="+216 29 400 001", Matricule="EY-SRC-001",
                Grade="Senior", Departement="Consulting",  Poste="Senior Consultant",
                BusinessUnit="Consulting", Localisation="Tunis — Lac 2",
                TypeContrat="CDI", NiveauHierarchique="Senior Consultant",
                DateEmbauche=new DateTime(2020,9,1), DatePrisePoste=new DateTime(2022,9,1),
                NiveauPreparationSuccession=45, PotentielCarriere="Potentiel solide",
                RoleRH="Collaborateur", Actif=true, Statut=StatutCollaborateur.Actif,
                ManagerId=mAmine.Id, DepartmentId=dCons.Id, GradeId=gradeSenior?.Id,
                BusinessUnitId=buConsulting?.Id, LocationId=locLac2?.Id,
                ContractTypeId=ctCDI?.Id, PositionId=posSrCons.Id },

            new Collaborateur { Nom="Mansouri",    Prenom="Lina",    Email="lina.mansouri@ey.com",
                Adresse="Mutuelleville, Rue de Grèce 29",  Ville="Tunis",      Pays="Tunisie",
                TelephonePersonnel="+216 29 400 002", Matricule="EY-CNS-001",
                Grade="Junior", Departement="Technology",  Poste="Consultant",
                BusinessUnit="Consulting", Localisation="Tunis — Lac 1",
                TypeContrat="CDI", NiveauHierarchique="Consultant",
                DateEmbauche=new DateTime(2022,9,1), DatePrisePoste=new DateTime(2022,9,1),
                NiveauPreparationSuccession=22, PotentielCarriere="Potentiel solide",
                RoleRH="Collaborateur", Actif=true, Statut=StatutCollaborateur.Actif,
                ManagerId=mAymen.Id, DepartmentId=dTech.Id, GradeId=gradeJunior?.Id,
                BusinessUnitId=buConsulting?.Id, LocationId=locLac1?.Id,
                ContractTypeId=ctCDI?.Id, PositionId=posConsultant.Id },

            new Collaborateur { Nom="Ben Romdhane",Prenom="Khalil",  Email="khalil.benromdhane@ey.com",
                Adresse="Jardins El Menzah, Apt 3C",       Ville="Tunis",      Pays="Tunisie",
                TelephonePersonnel="+216 29 400 003", Matricule="EY-SRC-002",
                Grade="Senior", Departement="Data & Analytics", Poste="Senior Consultant",
                BusinessUnit="Consulting", Localisation="Tunis — Lac 1",
                TypeContrat="CDI", NiveauHierarchique="Senior Consultant",
                DateEmbauche=new DateTime(2019,6,1), DatePrisePoste=new DateTime(2021,6,1),
                NiveauPreparationSuccession=42, PotentielCarriere="Haut potentiel",
                RoleRH="Collaborateur", Actif=true, Statut=StatutCollaborateur.Actif,
                ManagerId=mSarra.Id, DepartmentId=dData.Id, GradeId=gradeSenior?.Id,
                BusinessUnitId=buConsulting?.Id, LocationId=locLac1?.Id,
                ContractTypeId=ctCDI?.Id, PositionId=posSrCons.Id },

            new Collaborateur { Nom="Chaabane",    Prenom="Fatma",   Email="fatma.chaabane@ey.com",
                Adresse="Cité Mahrajène, Rue du Nil 5",    Ville="Tunis",      Pays="Tunisie",
                TelephonePersonnel="+216 29 400 004", Matricule="EY-CNS-002",
                Grade="Junior", Departement="Cybersecurity", Poste="Cybersecurity Analyst",
                BusinessUnit="Consulting", Localisation="Tunis — Lac 1",
                TypeContrat="CDI", NiveauHierarchique="Consultant",
                DateEmbauche=new DateTime(2023,3,1), DatePrisePoste=new DateTime(2023,3,1),
                NiveauPreparationSuccession=18, PotentielCarriere="Développement requis",
                RoleRH="Collaborateur", Actif=true, Statut=StatutCollaborateur.Actif,
                ManagerId=mMeriem.Id, DepartmentId=dCyber.Id, GradeId=gradeJunior?.Id,
                BusinessUnitId=buConsulting?.Id, LocationId=locLac1?.Id,
                ContractTypeId=ctCDI?.Id, PositionId=posCyberAna.Id },

            new Collaborateur { Nom="Mabrouk",     Prenom="Mehdi",   Email="mehdi.mabrouk@ey.com",
                Adresse="Rue El Jazira 11, Carthage",      Ville="Tunis",      Pays="Tunisie",
                TelephonePersonnel="+216 29 400 005", Matricule="EY-SRC-003",
                Grade="Senior", Departement="Assurance",  Poste="Auditeur Senior",
                BusinessUnit="Assurance", Localisation="Tunis — Lac 1",
                TypeContrat="CDI", NiveauHierarchique="Senior Consultant",
                DateEmbauche=new DateTime(2020,1,15), DatePrisePoste=new DateTime(2022,1,15),
                NiveauPreparationSuccession=48, PotentielCarriere="Potentiel solide",
                RoleRH="Collaborateur", Actif=true, Statut=StatutCollaborateur.Actif,
                ManagerId=mOmar.Id, DepartmentId=dAss.Id, GradeId=gradeSenior?.Id,
                BusinessUnitId=buAssurance?.Id, LocationId=locLac1?.Id,
                ContractTypeId=ctCDI?.Id, PositionId=posSrCons.Id },

            new Collaborateur { Nom="Boughanmi",   Prenom="Sarah",   Email="sarah.boughanmi@ey.com",
                Adresse="El Menzah 8, Rue des Chênes 3",  Ville="Tunis",      Pays="Tunisie",
                TelephonePersonnel="+216 29 400 006", Matricule="EY-CNS-003",
                Grade="Junior", Departement="People Consulting", Poste="Consultant",
                BusinessUnit="CBS", Localisation="Tunis — Lac 1",
                TypeContrat="CDI", NiveauHierarchique="Consultant",
                DateEmbauche=new DateTime(2023,9,1), DatePrisePoste=new DateTime(2023,9,1),
                NiveauPreparationSuccession=15, PotentielCarriere="Haut potentiel",
                RoleRH="Collaborateur", Actif=true, Statut=StatutCollaborateur.Actif,
                ManagerId=mYasmine.Id, DepartmentId=dPeo.Id, GradeId=gradeJunior?.Id,
                BusinessUnitId=buCBS?.Id, LocationId=locLac1?.Id,
                ContractTypeId=ctCDI?.Id, PositionId=posConsultant.Id },

            new Collaborateur { Nom="Turki",       Prenom="Rami",    Email="rami.turki@ey.com",
                Adresse="Sidi Bou Said, Route Touristique 14", Ville="Sidi Bou Said", Pays="Tunisie",
                TelephonePersonnel="+216 29 400 007", Matricule="EY-CNS-004",
                Grade="Junior", Departement="Business Transformation", Poste="Consultant",
                BusinessUnit="Strategy & Transactions", Localisation="Tunis — Lac 2",
                TypeContrat="CDI", NiveauHierarchique="Consultant",
                DateEmbauche=new DateTime(2023,1,9), DatePrisePoste=new DateTime(2023,1,9),
                NiveauPreparationSuccession=20, PotentielCarriere="Potentiel solide",
                RoleRH="Collaborateur", Actif=true, Statut=StatutCollaborateur.Actif,
                ManagerId=mNidhal.Id, DepartmentId=dBT.Id, GradeId=gradeJunior?.Id,
                BusinessUnitId=buStrategy?.Id, LocationId=locLac2?.Id,
                ContractTypeId=ctCDI?.Id, PositionId=posConsultant.Id },

            new Collaborateur { Nom="Ben Slama",   Prenom="Amira",   Email="amira.benslama@ey.com",
                Adresse="Cité El Khadra, Apt 7",           Ville="Tunis",      Pays="Tunisie",
                TelephonePersonnel="+216 29 400 008", Matricule="EY-SRC-004",
                Grade="Senior", Departement="Tax", Poste="Senior Consultant",
                BusinessUnit="Tax", Localisation="Tunis — Lac 1",
                TypeContrat="CDI", NiveauHierarchique="Senior Consultant",
                DateEmbauche=new DateTime(2021,4,1), DatePrisePoste=new DateTime(2023,4,1),
                NiveauPreparationSuccession=40, PotentielCarriere="Potentiel solide",
                RoleRH="Collaborateur", Actif=true, Statut=StatutCollaborateur.Actif,
                ManagerId=mRim.Id, DepartmentId=dTax.Id, GradeId=gradeSenior?.Id,
                BusinessUnitId=buTax?.Id, LocationId=locLac1?.Id,
                ContractTypeId=ctCDI?.Id, PositionId=posSrCons.Id },

            new Collaborateur { Nom="Kacem",       Prenom="Yassine", Email="yassine.kacem@ey.com",
                Adresse="Rue Mokhtar Attia 24, Nabeul",    Ville="Nabeul",     Pays="Tunisie",
                TelephonePersonnel="+216 29 400 009", Matricule="EY-CNS-005",
                Grade="Junior", Departement="Technology",  Poste="Consultant",
                BusinessUnit="Consulting", Localisation="Remote",
                TypeContrat="CDI", NiveauHierarchique="Consultant",
                DateEmbauche=new DateTime(2024,1,15), DatePrisePoste=new DateTime(2024,1,15),
                NiveauPreparationSuccession=12, PotentielCarriere="Développement requis",
                RoleRH="Collaborateur", Actif=true, Statut=StatutCollaborateur.Actif,
                ManagerId=mHoussem.Id, DepartmentId=dTech.Id, GradeId=gradeJunior?.Id,
                BusinessUnitId=buConsulting?.Id, LocationId=locRemote?.Id,
                ContractTypeId=ctCDI?.Id, PositionId=posConsultant.Id },

            new Collaborateur { Nom="Ben Hassen",  Prenom="Nadia",   Email="nadia.benhassen@ey.com",
                Adresse="Rue Mongi Slim 18",               Ville="Sfax",       Pays="Tunisie",
                TelephonePersonnel="+216 29 400 010", Matricule="EY-SRC-005",
                Grade="Senior", Departement="Data & Analytics", Poste="Senior Data Analyst",
                BusinessUnit="Consulting", Localisation="Sfax",
                TypeContrat="CDI", NiveauHierarchique="Senior Consultant",
                DateEmbauche=new DateTime(2021,10,1), DatePrisePoste=new DateTime(2023,10,1),
                NiveauPreparationSuccession=38, PotentielCarriere="Haut potentiel",
                RoleRH="Collaborateur", Actif=true, Statut=StatutCollaborateur.Actif,
                ManagerId=mDorra.Id, DepartmentId=dData.Id, GradeId=gradeSenior?.Id,
                BusinessUnitId=buConsulting?.Id, LocationId=locSfax?.Id,
                ContractTypeId=ctCDI?.Id, PositionId=posSrAnalyst.Id },

            new Collaborateur { Nom="Hammami",     Prenom="Tarek",   Email="tarek.hammami@ey.com",
                Adresse="Cité Ettahrir, Rue Mokhtar Attia 9", Ville="Tunis",  Pays="Tunisie",
                TelephonePersonnel="+216 29 400 011", Matricule="EY-CNS-006",
                Grade="Junior", Departement="Consulting",  Poste="Consultant",
                BusinessUnit="Consulting", Localisation="Tunis — Lac 2",
                TypeContrat="CDI", NiveauHierarchique="Consultant",
                DateEmbauche=new DateTime(2023,6,1), DatePrisePoste=new DateTime(2023,6,1),
                NiveauPreparationSuccession=19, PotentielCarriere="Potentiel solide",
                RoleRH="Collaborateur", Actif=true, Statut=StatutCollaborateur.Actif,
                ManagerId=mAmine.Id, DepartmentId=dCons.Id, GradeId=gradeJunior?.Id,
                BusinessUnitId=buConsulting?.Id, LocationId=locLac2?.Id,
                ContractTypeId=ctCDI?.Id, PositionId=posConsultant.Id },

            new Collaborateur { Nom="Zribi",       Prenom="Hajer",   Email="hajer.zribi@ey.com",
                Adresse="Cité Khaznadar, Apt 11B",         Ville="Tunis",      Pays="Tunisie",
                TelephonePersonnel="+216 29 400 012", Matricule="EY-CNS-007",
                Grade="Junior", Departement="Assurance",  Poste="Auditeur",
                BusinessUnit="Assurance", Localisation="Tunis — Lac 1",
                TypeContrat="CDI", NiveauHierarchique="Consultant",
                DateEmbauche=new DateTime(2024,3,1), DatePrisePoste=new DateTime(2024,3,1),
                NiveauPreparationSuccession=10, PotentielCarriere="Développement requis",
                RoleRH="Collaborateur", Actif=true, Statut=StatutCollaborateur.Actif,
                ManagerId=mOmar.Id, DepartmentId=dAss.Id, GradeId=gradeJunior?.Id,
                BusinessUnitId=buAssurance?.Id, LocationId=locLac1?.Id,
                ContractTypeId=ctCDI?.Id, PositionId=posConsultant.Id },
        };
        foreach (var c in juniors) await AddCollab(ctx, c);
        await ctx.SaveChangesAsync();

        // ── Step 9 · Additional Formations ────────────────────────────────────
        var newFormations = new[]
        {
            new Formation { Titre="Architecture Cloud & DevOps", Formateur="Aymen Trabelsi",
                DureeHeures=21, CapaciteMax=15, PlacesPrises=7,
                Categorie="Technologie", DateDebut=new DateTime(2025,10,15),
                Organisme="EY Technology Academy", CompetenceVisee="Cloud Computing",
                DepartementCible="Technology", DomaineCompetence="Infrastructure",
                NiveauDifficulte="Avancé", EstCertifiante=true },
            new Formation { Titre="Machine Learning appliqué aux finances", Formateur="Sarra Ben Ali",
                DureeHeures=28, CapaciteMax=12, PlacesPrises=5,
                Categorie="Data Science", DateDebut=new DateTime(2025,11,1),
                Organisme="EY Data Institute", CompetenceVisee="Machine Learning",
                DepartementCible="Data & Analytics", DomaineCompetence="Data",
                NiveauDifficulte="Avancé", EstCertifiante=true },
            new Formation { Titre="Cybersécurité — ISO 27001 Lead Implementer", Formateur="Walid Khelifi",
                DureeHeures=35, CapaciteMax=10, PlacesPrises=4,
                Categorie="Sécurité", DateDebut=new DateTime(2025,9,20),
                Organisme="PECB", CompetenceVisee="ISO 27001",
                DepartementCible="Cybersecurity", DomaineCompetence="Sécurité",
                NiveauDifficulte="Expert", EstCertifiante=true },
            new Formation { Titre="Conduite du changement — Prosci ADKAR", Formateur="Rania Chebbi",
                DureeHeures=14, CapaciteMax=20, PlacesPrises=9,
                Categorie="Management", DateDebut=new DateTime(2025,10,5),
                Organisme="Prosci", CompetenceVisee="Conduite du changement",
                DepartementCible="Business Transformation", DomaineCompetence="Management",
                NiveauDifficulte="Intermédiaire", EstCertifiante=true },
            new Formation { Titre="IFRS 9 et 17 — Instruments financiers", Formateur="Hatem Gharbi",
                DureeHeures=16, CapaciteMax=18, PlacesPrises=6,
                Categorie="Finance", DateDebut=new DateTime(2025,11,10),
                Organisme="EY Academy of Business", CompetenceVisee="IFRS avancé",
                DepartementCible="Assurance", DomaineCompetence="Audit",
                NiveauDifficulte="Avancé", EstCertifiante=false },
            new Formation { Titre="Data Visualization — Tableau & Power BI avancé", Formateur="Khalil Ben Romdhane",
                DureeHeures=12, CapaciteMax=20, PlacesPrises=11,
                Categorie="Data", DateDebut=new DateTime(2025,10,20),
                Organisme="EY Learning", CompetenceVisee="Data Visualization",
                DepartementCible="Data & Analytics", DomaineCompetence="Data",
                NiveauDifficulte="Intermédiaire", EstCertifiante=false },
            new Formation { Titre="Fiscalité internationale — Prix de transfert", Formateur="Rim Triki",
                DureeHeures=18, CapaciteMax=16, PlacesPrises=7,
                Categorie="Fiscalité", DateDebut=new DateTime(2025,12,1),
                Organisme="EY Tax Academy", CompetenceVisee="Prix de transfert",
                DepartementCible="Tax", DomaineCompetence="Fiscalité",
                NiveauDifficulte="Expert", EstCertifiante=false },
            new Formation { Titre="Design Thinking pour l'innovation RH", Formateur="Yasmine Kooli",
                DureeHeures=8, CapaciteMax=25, PlacesPrises=14,
                Categorie="RH & Innovation", DateDebut=new DateTime(2025,9,8),
                Organisme="EY Learning", CompetenceVisee="Design Thinking",
                DepartementCible="People Consulting", DomaineCompetence="Méthodes",
                NiveauDifficulte="Fondamental", EstCertifiante=false },
        };
        foreach (var f in newFormations)
            if (!await ctx.Formations.AnyAsync(x => x.Titre == f.Titre))
                ctx.Formations.Add(f);
        await ctx.SaveChangesAsync();

        // ── Step 10 · Competences ─────────────────────────────────────────────
        await SeedCompetences(ctx);

        // ── Step 11 · Inscriptions ────────────────────────────────────────────
        await SeedInscriptions(ctx);

        // ── Step 12 · Talent Evaluations (Nine-Box) ───────────────────────────
        await SeedTalentEvaluations(ctx);

        // ── Step 13 · Plans de développement ─────────────────────────────────
        await SeedPlansDeveloppement(ctx);

        // ── Step 14 · Version marker ──────────────────────────────────────────
        ctx.Parametres.Add(new Parametre
        {
            Code = SeedVersionKey,
            Valeur = DateTime.UtcNow.ToString("O"),
            TypeValeur = "string",
            Description = "Enterprise demo — Big Four Tunisia office profile",
            EstModifiable = false,
            DerniereModification = DateTime.Now
        });
        await ctx.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  COMPETENCES — role-matched skill sets
    // ═══════════════════════════════════════════════════════════════════════════

    private static async Task SeedCompetences(ApplicationDbContext ctx)
    {
        var now = DateTime.Today;

        // Look up category IDs
        var cats = await ctx.CategoriesCompetences.ToDictionaryAsync(c => c.Nom, c => c.Id);
        int Cat(string nom) => cats.TryGetValue(nom, out var id) ? id : 0;

        // Collaborateur IDs by email
        var all = await ctx.Collaborateurs.ToDictionaryAsync(c => c.Email!, c => c.Id);
        if (!all.Any()) return;

        int? Id(string email) => all.TryGetValue(email, out var id) ? id : (int?)null;

        var competences = new List<Competence>();

        void Add(string email, string nom, string cat, int actuel, int cible, int daysAgo)
        {
            var colId = Id(email);
            if (colId == null) return;
            if (ctx.Competences.Any(x => x.CollaborateurId == colId && x.Nom == nom)) return;
            competences.Add(new Competence
            {
                CollaborateurId = colId.Value, Nom = nom,
                CategorieCompetenceId = Cat(cat) == 0 ? null : (int?)Cat(cat),
                NiveauActuel = actuel, NiveauCible = cible,
                DateEvaluation = now.AddDays(-daysAgo)
            });
        }

        // Partners — strategic / leadership mastery
        Add("karim.benyoussef@ey.com",  "Leadership stratégique",          "Leadership", 5, 5, 90);
        Add("karim.benyoussef@ey.com",  "Business Development",            "Management", 5, 5, 85);
        Add("karim.benyoussef@ey.com",  "Stakeholder management",          "Management", 5, 5, 80);
        Add("karim.benyoussef@ey.com",  "Communication executive",         "Soft skills",5, 5, 75);
        Add("karim.benyoussef@ey.com",  "Gestion des risques",             "Risk",       5, 5, 60);

        Add("sami.trabelsi@ey.com",     "Leadership stratégique",          "Leadership", 5, 5, 88);
        Add("sami.trabelsi@ey.com",     "Audit & contrôle interne",        "Audit",      5, 5, 84);
        Add("sami.trabelsi@ey.com",     "IFRS / normes comptables",        "Audit",      5, 5, 70);
        Add("sami.trabelsi@ey.com",     "Communication executive",         "Soft skills",5, 5, 65);
        Add("sami.trabelsi@ey.com",     "Client management",               "Management", 5, 5, 58);

        Add("hatem.gharbi@ey.com",      "Leadership stratégique",          "Leadership", 5, 5, 92);
        Add("hatem.gharbi@ey.com",      "Tax compliance",                  "Fiscalité",  5, 5, 80);
        Add("hatem.gharbi@ey.com",      "Fiscalité internationale",        "Fiscalité",  5, 5, 74);
        Add("hatem.gharbi@ey.com",      "Négociation",                     "Soft skills",5, 5, 62);
        Add("hatem.gharbi@ey.com",      "Gestion de projet",               "Management", 4, 5, 55);

        // Directors
        Add("yosra.hammami@ey.com",     "Architecture technologique",      "Outils",     5, 5, 60);
        Add("yosra.hammami@ey.com",     "Leadership",                      "Leadership", 5, 5, 55);
        Add("yosra.hammami@ey.com",     "Gestion de projet",               "Management", 4, 5, 50);
        Add("yosra.hammami@ey.com",     "Cloud Computing",                 "Outils",     4, 5, 45);
        Add("yosra.hammami@ey.com",     "Communication",                   "Soft skills",4, 5, 40);

        Add("mehdi.jlassi@ey.com",      "Machine Learning",                "Data",       4, 5, 55);
        Add("mehdi.jlassi@ey.com",      "Data Governance",                 "Data",       5, 5, 50);
        Add("mehdi.jlassi@ey.com",      "Leadership",                      "Leadership", 4, 5, 48);
        Add("mehdi.jlassi@ey.com",      "SQL",                             "Data",       5, 5, 40);
        Add("mehdi.jlassi@ey.com",      "Analyse & résolution de problèmes","Méthodes",  5, 5, 35);

        Add("nour.bensalem@ey.com",     "Conseil stratégique",             "Management", 5, 5, 60);
        Add("nour.bensalem@ey.com",     "Leadership",                      "Leadership", 4, 5, 55);
        Add("nour.bensalem@ey.com",     "Gestion de projet",               "Management", 4, 4, 50);
        Add("nour.bensalem@ey.com",     "Communication",                   "Soft skills",4, 5, 45);
        Add("nour.bensalem@ey.com",     "Analyse & résolution de problèmes","Méthodes",  4, 5, 40);

        Add("walid.khelifi@ey.com",     "ISO 27001",                       "Risk",       5, 5, 60);
        Add("walid.khelifi@ey.com",     "Gestion des risques",             "Risk",       5, 5, 55);
        Add("walid.khelifi@ey.com",     "Cybersécurité opérationnelle",    "Risk",       4, 5, 50);
        Add("walid.khelifi@ey.com",     "Leadership",                      "Leadership", 4, 4, 45);
        Add("walid.khelifi@ey.com",     "RGPD & conformité",               "Risk",       4, 5, 40);

        Add("rania.chebbi@ey.com",      "Gestion des talents",             "RH",         5, 5, 58);
        Add("rania.chebbi@ey.com",      "Conduite du changement",          "Management", 5, 5, 52);
        Add("rania.chebbi@ey.com",      "Leadership",                      "Leadership", 4, 5, 48);
        Add("rania.chebbi@ey.com",      "Communication",                   "Soft skills",5, 5, 42);
        Add("rania.chebbi@ey.com",      "Design Thinking",                 "Méthodes",   4, 4, 35);

        // Senior Managers
        Add("aymen.trabelsi@ey.com",    "Cloud Computing",                 "Outils",     4, 5, 45);
        Add("aymen.trabelsi@ey.com",    "Gestion de projet",               "Management", 4, 5, 40);
        Add("aymen.trabelsi@ey.com",    "Leadership",                      "Leadership", 3, 5, 38);
        Add("aymen.trabelsi@ey.com",    "Architecture technologique",      "Outils",     4, 5, 32);

        Add("sarra.benali@ey.com",      "Machine Learning",                "Data",       4, 5, 42);
        Add("sarra.benali@ey.com",      "Power BI",                        "Data",       4, 4, 38);
        Add("sarra.benali@ey.com",      "SQL",                             "Data",       4, 5, 35);
        Add("sarra.benali@ey.com",      "Data storytelling",               "Soft skills",3, 4, 30);

        Add("omar.bensalah@ey.com",     "Audit & contrôle interne",        "Audit",      4, 5, 45);
        Add("omar.bensalah@ey.com",     "IFRS / normes comptables",        "Audit",      4, 4, 40);
        Add("omar.bensalah@ey.com",     "Leadership",                      "Leadership", 3, 4, 38);
        Add("omar.bensalah@ey.com",     "Gestion de projet",               "Management", 4, 4, 30);

        // Managers
        Add("amine.jebali@ey.com",      "Gestion de projet",               "Management", 3, 4, 30);
        Add("amine.jebali@ey.com",      "Communication",                   "Soft skills",3, 4, 28);
        Add("amine.jebali@ey.com",      "Conseil stratégique",             "Management", 3, 4, 25);
        Add("amine.jebali@ey.com",      "Analyse & résolution de problèmes","Méthodes",  3, 4, 22);

        Add("meriem.gharbi@ey.com",     "RGPD & conformité",               "Risk",       3, 4, 30);
        Add("meriem.gharbi@ey.com",     "ISO 27001",                       "Risk",       3, 4, 28);
        Add("meriem.gharbi@ey.com",     "Gestion des risques",             "Risk",       4, 4, 24);
        Add("meriem.gharbi@ey.com",     "Communication",                   "Soft skills",3, 4, 20);

        Add("yasmine.kooli@ey.com",     "Gestion des talents",             "RH",         3, 4, 28);
        Add("yasmine.kooli@ey.com",     "Conduite du changement",          "Management", 3, 4, 25);
        Add("yasmine.kooli@ey.com",     "Communication",                   "Soft skills",4, 4, 22);
        Add("yasmine.kooli@ey.com",     "Design Thinking",                 "Méthodes",   3, 4, 18);

        Add("nidhal.hammami@ey.com",    "Change management",               "Management", 3, 4, 28);
        Add("nidhal.hammami@ey.com",    "Gestion de projet",               "Management", 3, 4, 24);
        Add("nidhal.hammami@ey.com",    "Communication",                   "Soft skills",3, 4, 20);

        Add("rim.triki@ey.com",         "Tax compliance",                  "Fiscalité",  4, 4, 30);
        Add("rim.triki@ey.com",         "Fiscalité internationale",        "Fiscalité",  3, 4, 26);
        Add("rim.triki@ey.com",         "Communication",                   "Soft skills",3, 4, 22);
        Add("rim.triki@ey.com",         "Excel avancé",                    "Outils",     4, 4, 18);

        Add("houssem.benamor@ey.com",   "Cloud Computing",                 "Outils",     3, 4, 28);
        Add("houssem.benamor@ey.com",   "Gestion de projet",               "Management", 3, 4, 24);
        Add("houssem.benamor@ey.com",   "Communication",                   "Soft skills",3, 4, 20);

        Add("dorra.benmustapha@ey.com", "Power BI",                        "Data",       4, 4, 26);
        Add("dorra.benmustapha@ey.com", "Machine Learning",                "Data",       3, 4, 22);
        Add("dorra.benmustapha@ey.com", "SQL",                             "Data",       3, 4, 18);
        Add("dorra.benmustapha@ey.com", "Data storytelling",               "Soft skills",3, 4, 14);

        // Senior Consultants
        Add("aziz.belhadj@ey.com",      "Gestion de projet",               "Management", 3, 4, 20);
        Add("aziz.belhadj@ey.com",      "Communication",                   "Soft skills",3, 4, 18);
        Add("aziz.belhadj@ey.com",      "Excel avancé",                    "Outils",     3, 4, 15);
        Add("aziz.belhadj@ey.com",      "Analyse & résolution de problèmes","Méthodes",  3, 4, 12);

        Add("khalil.benromdhane@ey.com","Machine Learning",                 "Data",       3, 4, 20);
        Add("khalil.benromdhane@ey.com","Power BI",                         "Data",       4, 4, 18);
        Add("khalil.benromdhane@ey.com","SQL",                              "Data",       3, 4, 15);
        Add("khalil.benromdhane@ey.com","Data storytelling",                "Soft skills",3, 4, 12);

        Add("mehdi.mabrouk@ey.com",     "Audit & contrôle interne",        "Audit",      3, 4, 18);
        Add("mehdi.mabrouk@ey.com",     "IFRS / normes comptables",        "Audit",      3, 4, 16);
        Add("mehdi.mabrouk@ey.com",     "Communication",                   "Soft skills",3, 4, 14);

        Add("amira.benslama@ey.com",    "Tax compliance",                  "Fiscalité",  3, 4, 18);
        Add("amira.benslama@ey.com",    "Excel avancé",                    "Outils",     3, 4, 15);
        Add("amira.benslama@ey.com",    "Communication",                   "Soft skills",3, 4, 12);

        Add("nadia.benhassen@ey.com",   "Power BI",                        "Data",       4, 4, 16);
        Add("nadia.benhassen@ey.com",   "SQL",                             "Data",       3, 4, 14);
        Add("nadia.benhassen@ey.com",   "Data storytelling",               "Soft skills",3, 4, 12);

        // Consultants / Juniors
        Add("lina.mansouri@ey.com",     "Cloud Computing",                 "Outils",     2, 3, 14);
        Add("lina.mansouri@ey.com",     "Communication",                   "Soft skills",2, 4, 12);
        Add("lina.mansouri@ey.com",     "Excel avancé",                    "Outils",     2, 3, 10);

        Add("fatma.chaabane@ey.com",    "RGPD & conformité",               "Risk",       2, 3, 12);
        Add("fatma.chaabane@ey.com",    "Gestion des risques",             "Risk",       2, 3, 10);
        Add("fatma.chaabane@ey.com",    "Communication",                   "Soft skills",2, 3, 8);

        Add("sarah.boughanmi@ey.com",   "Gestion des talents",             "RH",         2, 3, 12);
        Add("sarah.boughanmi@ey.com",   "Communication",                   "Soft skills",3, 4, 10);
        Add("sarah.boughanmi@ey.com",   "Design Thinking",                 "Méthodes",   2, 3, 8);

        Add("rami.turki@ey.com",        "Gestion de projet",               "Management", 2, 3, 12);
        Add("rami.turki@ey.com",        "Communication",                   "Soft skills",2, 3, 10);
        Add("rami.turki@ey.com",        "Change management",               "Management", 2, 3, 8);

        Add("yassine.kacem@ey.com",     "Cloud Computing",                 "Outils",     1, 3, 10);
        Add("yassine.kacem@ey.com",     "Communication",                   "Soft skills",2, 3, 8);
        Add("yassine.kacem@ey.com",     "Excel avancé",                    "Outils",     2, 3, 6);

        Add("tarek.hammami@ey.com",     "Gestion de projet",               "Management", 2, 3, 10);
        Add("tarek.hammami@ey.com",     "Communication",                   "Soft skills",2, 4, 8);
        Add("tarek.hammami@ey.com",     "Excel avancé",                    "Outils",     2, 3, 6);

        Add("hajer.zribi@ey.com",       "Audit & contrôle interne",        "Audit",      1, 3, 10);
        Add("hajer.zribi@ey.com",       "Communication",                   "Soft skills",2, 3, 8);
        Add("hajer.zribi@ey.com",       "Excel avancé",                    "Outils",     2, 3, 6);

        if (competences.Any())
        {
            ctx.Competences.AddRange(competences);
            await ctx.SaveChangesAsync();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  INSCRIPTIONS — realistic training enrollment history
    // ═══════════════════════════════════════════════════════════════════════════

    private static async Task SeedInscriptions(ApplicationDbContext ctx)
    {
        var collabs = await ctx.Collaborateurs.ToDictionaryAsync(c => c.Email!, c => c.Id);
        var formations = await ctx.Formations.ToDictionaryAsync(f => f.Titre, f => f.Id);
        var today = DateTime.Today;

        var inscriptions = new List<Inscription>();

        void Inscribe(string email, string titre, DateTime date, bool done, int progression, int? examDaysFromNow = null)
        {
            if (!collabs.TryGetValue(email, out var cId)) return;
            if (!formations.TryGetValue(titre, out var fId)) return;
            if (ctx.Inscriptions.Any(x => x.CollaborateurId == cId && x.FormationId == fId)) return;
            inscriptions.Add(new Inscription
            {
                CollaborateurId = cId,
                FormationId = fId,
                DateInscription = date,
                Terminee = done,
                Progression = progression,
                DateExamen = examDaysFromNow.HasValue ? today.AddDays(examDaysFromNow.Value) : null
            });
        }

        // Partners — certifications already obtained (all 100%)
        Inscribe("karim.benyoussef@ey.com",  "Leadership — piloter une équipe",            today.AddDays(-180), true,  100);
        Inscribe("karim.benyoussef@ey.com",  "Change management — outils & conduite",      today.AddDays(-150), true,  100);
        Inscribe("sami.trabelsi@ey.com",     "Audit & contrôle interne — fondamentaux",    today.AddDays(-200), true,  100);
        Inscribe("sami.trabelsi@ey.com",     "IFRS — principes clés",                      today.AddDays(-160), true,  100);
        Inscribe("hatem.gharbi@ey.com",      "RGPD & conformité — mise en pratique",       today.AddDays(-170), true,  100);
        Inscribe("hatem.gharbi@ey.com",      "Fiscalité internationale — Prix de transfert",today.AddDays(-90), true,  100);

        // Directors — mix of completed and in-progress
        Inscribe("yosra.hammami@ey.com",     "Architecture Cloud & DevOps",                today.AddDays(-60),  true,  100);
        Inscribe("yosra.hammami@ey.com",     "Leadership — piloter une équipe",            today.AddDays(-40),  false, 65, 25);
        Inscribe("mehdi.jlassi@ey.com",      "Machine Learning appliqué aux finances",     today.AddDays(-45),  true,  100);
        Inscribe("mehdi.jlassi@ey.com",      "Data Visualization — Tableau & Power BI avancé", today.AddDays(-20), false, 40);
        Inscribe("nour.bensalem@ey.com",     "Gestion de projet Agile (Scrum)",            today.AddDays(-90),  true,  100);
        Inscribe("nour.bensalem@ey.com",     "Communication impactante & feedback",        today.AddDays(-30),  false, 50);
        Inscribe("walid.khelifi@ey.com",     "Cybersécurité — ISO 27001 Lead Implementer", today.AddDays(-70),  true,  100);
        Inscribe("walid.khelifi@ey.com",     "RGPD & conformité — mise en pratique",       today.AddDays(-50),  true,  100);
        Inscribe("rania.chebbi@ey.com",      "Conduite du changement — Prosci ADKAR",      today.AddDays(-55),  true,  100);
        Inscribe("rania.chebbi@ey.com",      "Design Thinking pour l'innovation RH",       today.AddDays(-25),  false, 70);

        // Senior Managers
        Inscribe("aymen.trabelsi@ey.com",    "Architecture Cloud & DevOps",                today.AddDays(-30),  false, 75, 14);
        Inscribe("sarra.benali@ey.com",      "Machine Learning appliqué aux finances",     today.AddDays(-40),  false, 60, 20);
        Inscribe("sarra.benali@ey.com",      "Power BI — dashboards & DAX",                today.AddDays(-80),  true,  100);
        Inscribe("omar.bensalah@ey.com",     "Audit & contrôle interne — fondamentaux",    today.AddDays(-90),  true,  100);
        Inscribe("omar.bensalah@ey.com",     "IFRS 9 et 17 — Instruments financiers",      today.AddDays(-20),  false, 35);

        // Managers
        Inscribe("amine.jebali@ey.com",      "Gestion de projet Agile (Scrum)",            today.AddDays(-35),  false, 55);
        Inscribe("meriem.gharbi@ey.com",     "Cybersécurité — ISO 27001 Lead Implementer", today.AddDays(-40),  false, 45, 30);
        Inscribe("yasmine.kooli@ey.com",     "Design Thinking pour l'innovation RH",       today.AddDays(-15),  false, 30);
        Inscribe("nidhal.hammami@ey.com",    "Conduite du changement — Prosci ADKAR",      today.AddDays(-50),  true,  100);
        Inscribe("rim.triki@ey.com",         "Fiscalité internationale — Prix de transfert",today.AddDays(-30),  false, 50, 45);
        Inscribe("houssem.benamor@ey.com",   "Architecture Cloud & DevOps",                today.AddDays(-25),  false, 40);
        Inscribe("dorra.benmustapha@ey.com", "Machine Learning appliqué aux finances",     today.AddDays(-20),  false, 25);

        // Senior Consultants
        Inscribe("aziz.belhadj@ey.com",      "Excel avancé pour le conseil",               today.AddDays(-60),  true,  100);
        Inscribe("aziz.belhadj@ey.com",      "Gestion de projet Agile (Scrum)",            today.AddDays(-20),  false, 45);
        Inscribe("khalil.benromdhane@ey.com","Power BI — dashboards & DAX",                today.AddDays(-45),  true,  100);
        Inscribe("khalil.benromdhane@ey.com","SQL — requêtes et optimisation",              today.AddDays(-15),  false, 35);
        Inscribe("mehdi.mabrouk@ey.com",     "Audit & contrôle interne — fondamentaux",    today.AddDays(-50),  true,  100);
        Inscribe("amira.benslama@ey.com",    "RGPD & conformité — mise en pratique",       today.AddDays(-40),  true,  100);
        Inscribe("nadia.benhassen@ey.com",   "Data Visualization — Tableau & Power BI avancé", today.AddDays(-25), false, 55);

        // Juniors / Consultants
        Inscribe("lina.mansouri@ey.com",     "Communication impactante & feedback",        today.AddDays(-18),  false, 30);
        Inscribe("fatma.chaabane@ey.com",    "RGPD & conformité — mise en pratique",       today.AddDays(-12),  false, 20);
        Inscribe("sarah.boughanmi@ey.com",   "Design Thinking pour l'innovation RH",       today.AddDays(-10),  false, 15);
        Inscribe("rami.turki@ey.com",        "Change management — outils & conduite",      today.AddDays(-22),  false, 25);
        Inscribe("yassine.kacem@ey.com",     "Excel avancé pour le conseil",               today.AddDays(-8),   false, 10);
        Inscribe("tarek.hammami@ey.com",     "Communication impactante & feedback",        today.AddDays(-14),  false, 20);
        Inscribe("hajer.zribi@ey.com",       "Audit & contrôle interne — fondamentaux",    today.AddDays(-9),   false, 15);

        if (inscriptions.Any())
        {
            ctx.Inscriptions.AddRange(inscriptions);
            await ctx.SaveChangesAsync();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  TALENT EVALUATIONS — Nine-Box distribution
    // ═══════════════════════════════════════════════════════════════════════════

    private static async Task SeedTalentEvaluations(ApplicationDbContext ctx)
    {
        var collabs = await ctx.Collaborateurs.ToDictionaryAsync(c => c.Email!, c => c.Id);
        var now = DateTime.Today;

        var evals = new List<(string email, int perf, int pot, NineBoxCategory cat, string cPerf, string cPot)>
        {
            // Partners — Star (performance 5 / potentiel 5)
            ("karim.benyoussef@ey.com",  5, 5, NineBoxCategory.Star,             "Excelle dans toutes les dimensions. Pilote la stratégie régionale.",      "Leader visionnaire reconnu par les pairs et clients."),
            ("sami.trabelsi@ey.com",     5, 5, NineBoxCategory.Star,             "Expertise technique et relationnelle au plus haut niveau.",                "Capacité à développer les marchés et fidéliser les grands comptes."),
            ("hatem.gharbi@ey.com",      5, 5, NineBoxCategory.Star,             "Référence en fiscalité internationale au sein du cabinet.",                "Mentoring naturel, rayonnement externe exceptionnel."),

            // Directors — FutureLeader / HighProfessional
            ("yosra.hammami@ey.com",     5, 5, NineBoxCategory.FutureLeader,     "Délivre des missions complexes avec excellence constante.",                "Prête pour une promotion Partner dans 12-18 mois."),
            ("mehdi.jlassi@ey.com",      5, 4, NineBoxCategory.HighProfessional, "Expert Data reconnu, excellents résultats client.",                       "Fort potentiel de leadership, développe son réseau."),
            ("nour.bensalem@ey.com",     4, 5, NineBoxCategory.FutureLeader,     "Très bon niveau opérationnel, gestion d'équipe efficace.",                "Ambition claire, progression rapide vers Partner."),
            ("walid.khelifi@ey.com",     4, 4, NineBoxCategory.EmergingTalent,   "Expert cybersécurité, bon delivery sur les missions ISO.",                 "Leadership en développement, progresse bien."),
            ("rania.chebbi@ey.com",      4, 5, NineBoxCategory.FutureLeader,     "Excellent delivery RH, forte présence client.",                           "Candidate naturelle pour la promotion Partner."),

            // Senior Managers
            ("aymen.trabelsi@ey.com",    4, 4, NineBoxCategory.EmergingTalent,   "Delivery solide et équipe bien gérée.",                                   "Potentiel Director confirmé, accélérer le coaching."),
            ("sarra.benali@ey.com",      4, 5, NineBoxCategory.FutureLeader,     "Expertise Data de haut niveau, clients très satisfaits.",                 "Profil exceptionnel, pipeline Director clair."),
            ("omar.bensalah@ey.com",     4, 3, NineBoxCategory.HighProfessional, "Senior Manager Audit fiable, excellent niveau technique.",                 "Potentiel confirmé au niveau actuel."),

            // Managers
            ("amine.jebali@ey.com",      3, 4, NineBoxCategory.SolidProfessional,"Bonne maîtrise du delivery consulting.",                                  "Potentiel Senior Manager identifié."),
            ("meriem.gharbi@ey.com",     4, 4, NineBoxCategory.EmergingTalent,   "Montée en compétences rapide sur la cybersécurité.",                      "Profil prometteur, accompagner vers Senior Manager."),
            ("yasmine.kooli@ey.com",     3, 5, NineBoxCategory.RisingStar,       "Très bonne prestation, clients satisfaits.",                              "Fort potentiel inexploité, accélérer sa carrière."),
            ("nidhal.hammami@ey.com",    3, 3, NineBoxCategory.SolidProfessional,"Delivery correct, gestion d'équipe stable.",                              "Potentiel solide au niveau actuel."),
            ("rim.triki@ey.com",         4, 3, NineBoxCategory.SolidProfessional,"Expert Tax très apprécié par les clients.",                               "Profil technique fort, leadership à renforcer."),
            ("houssem.benamor@ey.com",   3, 4, NineBoxCategory.SolidProfessional,"Bon niveau, en progression sur les sujets Cloud.",                        "Potentiel visible, encourager la prise de responsabilités."),
            ("dorra.benmustapha@ey.com", 4, 4, NineBoxCategory.EmergingTalent,   "Excellente progression, bons feedbacks clients.",                         "Candidate Senior Manager dans 18 mois."),

            // Senior Consultants
            ("aziz.belhadj@ey.com",      3, 3, NineBoxCategory.SolidProfessional,"Contribution fiable sur les missions.",                                   "Progression régulière, pas encore de signal fort."),
            ("khalil.benromdhane@ey.com",3, 5, NineBoxCategory.RisingStar,       "Compétences Data solides, appétit pour le leadership.",                   "Potentiel élevé, suivi rapproché recommandé."),
            ("mehdi.mabrouk@ey.com",     3, 3, NineBoxCategory.SolidProfessional,"Auditeur sérieux, qualité de travail appréciée.",                         "Évolution attendue vers Senior Manager."),
            ("amira.benslama@ey.com",    3, 3, NineBoxCategory.SolidProfessional,"Bon niveau Tax, autonome sur les livrables.",                             "Potentiel confirmé au niveau actuel."),
            ("nadia.benhassen@ey.com",   4, 4, NineBoxCategory.EmergingTalent,   "Excellentes compétences analytiques, très appréciée.",                    "Profil à suivre de près pour accélérer."),

            // Juniors
            ("lina.mansouri@ey.com",     3, 3, NineBoxCategory.SolidProfessional,"Bonne intégration, progressions visibles.",                               "Potentiel solide, à développer."),
            ("fatma.chaabane@ey.com",    2, 3, NineBoxCategory.NeedDevelopment,  "Doit renforcer ses bases techniques Cyber.",                              "Potentiel présent mais nécessite encadrement renforcé."),
            ("sarah.boughanmi@ey.com",   3, 5, NineBoxCategory.RisingStar,       "Très bonne attitude, clients satisfaits.",                                "Fort potentiel, one to watch."),
            ("rami.turki@ey.com",        3, 3, NineBoxCategory.SolidProfessional,"Correct sur les livrables BT.",                                           "Potentiel confirmé au niveau Consultant."),
            ("yassine.kacem@ey.com",     2, 2, NineBoxCategory.NeedDevelopment,  "Doit améliorer son autonomie et sa rigueur.",                             "Suivi mensuel recommandé avec le manager."),
            ("tarek.hammami@ey.com",     3, 3, NineBoxCategory.SolidProfessional,"Bon junior Consulting, progressions visibles.",                           "Potentiel standard pour le niveau."),
            ("hajer.zribi@ey.com",       3, 3, NineBoxCategory.SolidProfessional,"Bonne maîtrise des bases d'audit.",                                       "Progression attendue sur les 12 prochains mois."),
        };

        foreach (var (email, perf, pot, cat, cPerf, cPot) in evals)
        {
            if (!collabs.TryGetValue(email, out var cId)) continue;
            if (await ctx.TalentEvaluations.AnyAsync(t => t.CollaborateurId == cId)) continue;
            ctx.TalentEvaluations.Add(new TalentEvaluation
            {
                CollaborateurId = cId,
                PerformanceScore = perf,
                PotentielScore = pot,
                Category = cat,
                CommentairesPerformance = cPerf,
                CommentairesPotentiel = cPot,
                DateEvaluation = now.AddDays(-new[] {10,15,20,25,30,35,40,45,50}[cId % 9]),
                Actif = true
            });
        }
        await ctx.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PLANS DE DÉVELOPPEMENT
    // ═══════════════════════════════════════════════════════════════════════════

    private static async Task SeedPlansDeveloppement(ApplicationDbContext ctx)
    {
        var collabs    = await ctx.Collaborateurs.ToDictionaryAsync(c => c.Email!, c => c.Id);
        var formations = await ctx.Formations.ToDictionaryAsync(f => f.Titre, f => f.Id);
        var today = DateTime.Today;

        var plans = new List<PlanDeveloppement>();

        void Plan(string email, string titre, string statut, string commentaire)
        {
            if (!collabs.TryGetValue(email, out var cId)) return;
            if (!formations.TryGetValue(titre, out var fId)) return;
            if (ctx.PlansDeveloppement.Any(p => p.CollaborateurId == cId && p.FormationId == fId)) return;
            plans.Add(new PlanDeveloppement
            {
                CollaborateurId = cId,
                FormationId = fId,
                DateRecommandation = today.AddDays(-30),
                Statut = statut,
                Commentaire = commentaire
            });
        }

        Plan("aymen.trabelsi@ey.com",    "Leadership — piloter une équipe",              "À faire",  "Priorité pour accélération vers Director.");
        Plan("sarra.benali@ey.com",      "Leadership — piloter une équipe",              "En cours", "Indispensable pour le pipeline Director.");
        Plan("amine.jebali@ey.com",      "Gestion de projet Agile (Scrum)",              "En cours", "Renforcer la maîtrise méthodologique.");
        Plan("meriem.gharbi@ey.com",     "Cybersécurité — ISO 27001 Lead Implementer",   "À faire",  "Certification prioritaire pour la mission en cours.");
        Plan("yasmine.kooli@ey.com",     "Leadership — piloter une équipe",              "À faire",  "Accélérer le leadership, fort potentiel.");
        Plan("nidhal.hammami@ey.com",    "Change management — outils & conduite",        "Validé",   "Acquis — maintenir les pratiques.");
        Plan("rim.triki@ey.com",         "Fiscalité internationale — Prix de transfert", "En cours", "Expertise clé pour les missions OCDE.");
        Plan("houssem.benamor@ey.com",   "Architecture Cloud & DevOps",                  "À faire",  "Indispensable pour les projets Cloud clients.");
        Plan("dorra.benmustapha@ey.com", "Machine Learning appliqué aux finances",        "En cours", "Montée en compétences Data Science confirmée.");
        Plan("khalil.benromdhane@ey.com","Machine Learning appliqué aux finances",        "À faire",  "Profil à fort potentiel — investir dans le ML.");
        Plan("lina.mansouri@ey.com",     "Communication impactante & feedback",          "À faire",  "Base indispensable en consulting.");
        Plan("fatma.chaabane@ey.com",    "RGPD & conformité — mise en pratique",         "En cours", "Fondamentaux Cyber à consolider impérativement.");
        Plan("sarah.boughanmi@ey.com",   "Conduite du changement — Prosci ADKAR",        "À faire",  "Potentiel fort — lui donner les outils RH.");
        Plan("hajer.zribi@ey.com",       "Audit & contrôle interne — fondamentaux",      "En cours", "Bases d'audit à solidifier en priorité.");

        if (plans.Any())
        {
            ctx.PlansDeveloppement.AddRange(plans);
            await ctx.SaveChangesAsync();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  HELPERS — idempotent get-or-create
    // ═══════════════════════════════════════════════════════════════════════════

    private static async Task<Department> GetOrCreateDept(
        ApplicationDbContext ctx, string name, string code, string? description)
    {
        var existing = await ctx.Departments.FirstOrDefaultAsync(d => d.Name == name);
        if (existing != null) return existing;
        var dept = new Department { Name = name, Code = code, Description = description, IsActive = true };
        ctx.Departments.Add(dept);
        await ctx.SaveChangesAsync();
        return dept;
    }

    private static async Task<SubDepartment> GetOrCreateSubDept(
        ApplicationDbContext ctx, string name, string code, int deptId)
    {
        var existing = await ctx.SubDepartments.FirstOrDefaultAsync(s => s.Name == name);
        if (existing != null) return existing;
        var sub = new SubDepartment { Name = name, Code = code, DepartmentId = deptId, IsActive = true };
        ctx.SubDepartments.Add(sub);
        await ctx.SaveChangesAsync();
        return sub;
    }

    private static async Task<Position> GetOrCreatePosition(
        ApplicationDbContext ctx, string name, string code, int? subDeptId)
    {
        var existing = await ctx.Positions.FirstOrDefaultAsync(p => p.Name == name);
        if (existing != null) return existing;
        var pos = new Position { Name = name, Code = code, SubDepartmentId = subDeptId, IsActive = true };
        ctx.Positions.Add(pos);
        await ctx.SaveChangesAsync();
        return pos;
    }

    private static async Task AddCollab(ApplicationDbContext ctx, Collaborateur c)
    {
        if (!await ctx.Collaborateurs.AnyAsync(x => x.Email == c.Email))
            ctx.Collaborateurs.Add(c);
    }
}
