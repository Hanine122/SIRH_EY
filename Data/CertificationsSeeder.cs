using Microsoft.EntityFrameworkCore;
using SIRH.EY.Models;

namespace SIRH.EY.Data;

public static class CertificationsSeeder
{
    private const string SeedVersion = "CERTIFICATIONS_V1_2026_06";

    public static async Task SeedAsync(ApplicationDbContext ctx)
    {
        if (await ctx.Parametres.AnyAsync(p => p.Code == SeedVersion))
            return;

        // ── 1. Catalogue de certifications ───────────────────────────────────
        var certifications = new List<Certification>
        {
            new() { Nom = "PL-900: Microsoft Power Platform Fundamentals",          Organisme = "Microsoft", Domaine = "CRM / Low-Code",  CodeExamen = "PL-900",  EstReconnue = true },
            new() { Nom = "PL-100: Microsoft Power Platform App Maker",             Organisme = "Microsoft", Domaine = "CRM / Low-Code",  CodeExamen = "PL-100",  EstReconnue = true },
            new() { Nom = "MB-210: Microsoft Dynamics 365 Sales",                   Organisme = "Microsoft", Domaine = "CRM",             CodeExamen = "MB-210",  EstReconnue = true },
            new() { Nom = "MB-230: Microsoft Dynamics 365 Customer Service",        Organisme = "Microsoft", Domaine = "CRM",             CodeExamen = "MB-230",  EstReconnue = true },
            new() { Nom = "MB-700: Microsoft Dynamics 365 Solution Architect",      Organisme = "Microsoft", Domaine = "Architecture CRM",CodeExamen = "MB-700",  EstReconnue = true },
            new() { Nom = "AZ-204: Developing Solutions for Microsoft Azure",       Organisme = "Microsoft", Domaine = "Cloud / Azure",   CodeExamen = "AZ-204",  EstReconnue = true },
            new() { Nom = "PMP: Project Management Professional",                   Organisme = "PMI",       Domaine = "Management",      CodeExamen = "PMP",     EstReconnue = true },
            new() { Nom = "Prosci Change Management Certification",                 Organisme = "Prosci",    Domaine = "Change",          CodeExamen = "PROSCI",  EstReconnue = true },
            new() { Nom = "AWS Solutions Architect Associate",                      Organisme = "Amazon",    Domaine = "Cloud",           CodeExamen = "SAA-C03", EstReconnue = true },
            new() { Nom = "Tableau Desktop Specialist",                             Organisme = "Salesforce",Domaine = "Data / BI",       CodeExamen = "TDS",     EstReconnue = true },
            new() { Nom = "ISO 27001 Lead Implementer",                             Organisme = "PECB",      Domaine = "Cybersécurité",   CodeExamen = "ISO27001",EstReconnue = true },
            new() { Nom = "Google Project Management Certificate",                  Organisme = "Google",    Domaine = "Management",      CodeExamen = "GPMC",    EstReconnue = true },
            new() { Nom = "PL-300: Microsoft Power BI Data Analyst",               Organisme = "Microsoft", Domaine = "Data / BI",       CodeExamen = "PL-300",  EstReconnue = true },
            new() { Nom = "Diplôme IFRS — ACCA",                                   Organisme = "ACCA",      Domaine = "Finance / Audit", CodeExamen = "IFRS",    EstReconnue = true },
            new() { Nom = "Certification DPO — CNIL",                              Organisme = "CNIL",      Domaine = "RGPD / Droit",    CodeExamen = "DPO",     EstReconnue = true },
        };

        ctx.Certifications.AddRange(certifications);
        await ctx.SaveChangesAsync();

        // ── 2. Liaisons collaborateurs → certifications ───────────────────────
        // Résolution par email pour garantir l'idempotence même si les IDs varient
        var liaisons = new List<(string Email, string CertNom, int MoisObtention, string Statut)>
        {
            // Nour Smiäi — Data Analyst Tax
            ("smiai.nour@ey.com",        "PL-300: Microsoft Power BI Data Analyst",      -18, "Active"),
            ("smiai.nour@ey.com",        "Google Project Management Certificate",         -6,  "Active"),

            // Mariem Safri — Senior Auditor
            ("mariem.safri@ey.com",      "Diplôme IFRS — ACCA",                          -24, "Active"),

            // Ahmed Ben Youssef — Audit Manager
            ("Ahmed.benyoussef@ey.com",  "Diplôme IFRS — ACCA",                          -36, "Active"),
            ("Ahmed.benyoussef@ey.com",  "PMP: Project Management Professional",          -12, "Active"),

            // Sofien Klaou — Senior Consultant Advisory
            ("sofien.klaou@ey.com",      "PMP: Project Management Professional",          -8,  "Active"),
            ("sofien.klaou@ey.com",      "Prosci Change Management Certification",        -3,  "Active"),

            // Ibtissem Besrour — Risk Manager
            ("ibtissem.bessrour@ey.com", "ISO 27001 Lead Implementer",                   -14, "Active"),
            ("ibtissem.bessrour@ey.com", "Certification DPO — CNIL",                     -20, "Active"),

            // Hanine Hammami — HR Director
            ("hanine.hammami@ey.com",    "Prosci Change Management Certification",        -30, "Active"),
            ("hanine.hammami@ey.com",    "PMP: Project Management Professional",          -48, "Active"),
        };

        var byEmail = await ctx.Collaborateurs
            .Where(c => liaisons.Select(l => l.Email).Contains(c.Email))
            .ToDictionaryAsync(c => c.Email!);

        var byCertNom = certifications.ToDictionary(c => c.Nom);

        var collabCerts = new List<CollaborateurCertification>();

        foreach (var (email, certNom, moisObtention, statut) in liaisons)
        {
            if (!byEmail.TryGetValue(email, out var collab)) continue;
            if (!byCertNom.TryGetValue(certNom, out var cert)) continue;

            var dateObtention = DateTime.Today.AddMonths(moisObtention);
            collabCerts.Add(new CollaborateurCertification
            {
                CollaborateurId = collab.Id,
                CertificationId = cert.Id,
                DateObtention   = dateObtention,
                DateExpiration  = statut == "Active" ? dateObtention.AddYears(3) : null,
                Statut          = statut,
            });
        }

        ctx.CollaborateurCertifications.AddRange(collabCerts);
        ctx.Parametres.Add(new Parametre { Code = SeedVersion, Valeur = "done" });
        await ctx.SaveChangesAsync();
    }
}
