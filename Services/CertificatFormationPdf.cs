using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SIRH.EY.Models;

namespace SIRH.EY.Services;

public static class CertificatFormationPdf
{
    public static byte[] Generer(Inscription inscription)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var nom = $"{inscription.Collaborateur?.Prenom} {inscription.Collaborateur?.Nom}".Trim();
        if (string.IsNullOrEmpty(nom)) nom = "Collaborateur";
        var titre = inscription.Formation?.Titre ?? "Formation";
        var org = string.IsNullOrWhiteSpace(inscription.Formation?.Organisme)
            ? "EY Learning"
            : inscription.Formation!.Organisme!;
        var date = inscription.DateInscription.ToString("dd/MM/yyyy");
        var duree = inscription.Formation?.DureeHeures ?? 0;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Content().Column(col =>
                {
                    col.Item().AlignCenter().Text("EY").FontSize(28).Bold().FontColor(Colors.Blue.Medium);
                    col.Item().PaddingTop(24).AlignCenter().Text("Certificat de formation").FontSize(22).Bold();
                    col.Item().PaddingTop(32).Text(text =>
                    {
                        text.Span("Certifie que ");
                        text.Span(nom).Bold();
                        text.Span(" a complété avec succès la formation intitulée ");
                        text.Span(titre).Bold();
                        text.Span(".");
                    });
                    col.Item().PaddingTop(16).Text($"Durée : {duree} heures");
                    col.Item().Text($"Organisme : {org}");
                    col.Item().PaddingTop(24).Text($"Date de validation : {date}");
                    col.Item().PaddingTop(48).AlignCenter().Text("Document généré à des fins de démonstration — SIRH.EY").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }
}
