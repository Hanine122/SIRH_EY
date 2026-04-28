using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SIRH.EY.Services;

public record ComparaisonPdfRow(
    string Competence,
    IReadOnlyList<bool> Couverture // true = OK, false = à combler
);

public record ComparaisonPdfCandidat(
    int Id,
    string NomComplet,
    string Departement,
    int CompatibilitePourcent,
    int Manquantes,
    IReadOnlyList<string> Priorites,
    IReadOnlyList<string> Formations
);

public static class ComparaisonRemplacantsPdf
{
    public static byte[] Generer(
        string titre,
        string sousTitre,
        IReadOnlyList<string> competencesRequises,
        IReadOnlyList<ComparaisonPdfCandidat> candidats,
        IReadOnlyList<ComparaisonPdfRow> lignes)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text(titre).FontSize(18).Bold();
                        r.ConstantItem(180).AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontColor(Colors.Grey.Darken2);
                    });
                    col.Item().Text(sousTitre).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Column(col =>
                {
                    if (candidats.Count == 0)
                    {
                        col.Item().PaddingTop(18).Text("Aucun candidat sélectionné.").FontColor(Colors.Grey.Darken2);
                        return;
                    }

                    col.Item().PaddingTop(10).Text($"Candidats comparés : {candidats.Count}").Bold();

                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(210); // Compétence
                            foreach (var _ in candidats)
                                cols.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellHeader).Text("Compétence requise");
                            foreach (var c in candidats)
                            {
                                header.Cell().Element(CellHeader).Column(cc =>
                                {
                                    cc.Item().Text(c.NomComplet).Bold();
                                    cc.Item().Text(c.Departement).FontColor(Colors.Grey.Darken2).FontSize(9);
                                    cc.Item().PaddingTop(2).Row(rr =>
                                    {
                                        rr.RelativeItem().Text($"Compat. {c.CompatibilitePourcent}%").FontSize(9);
                                        rr.RelativeItem().AlignRight().Text($"Manq. {c.Manquantes}").FontSize(9);
                                    });
                                });
                            }
                        });

                        foreach (var row in lignes)
                        {
                            table.Cell().Element(CellBody).Text(row.Competence).SemiBold();
                            for (var i = 0; i < candidats.Count; i++)
                            {
                                var ok = row.Couverture.Count > i && row.Couverture[i];
                                table.Cell().Element(CellBody).AlignCenter().Text(ok ? "OK" : "À combler")
                                    .FontColor(ok ? Colors.Green.Darken2 : Colors.Red.Darken2)
                                    .Bold();
                            }
                        }

                        // Synthèse
                        table.Cell().Element(CellFooter).Text("Synthèse").FontColor(Colors.Grey.Darken2);
                        foreach (var c in candidats)
                        {
                            table.Cell().Element(CellFooter).Column(cc =>
                            {
                                if (c.Priorites.Count > 0)
                                {
                                    cc.Item().Text($"Priorités : {string.Join(", ", c.Priorites)}").FontSize(9);
                                }
                                else
                                {
                                    cc.Item().Text("Couverture complète").FontSize(9);
                                }

                                if (c.Formations.Count > 0)
                                {
                                    cc.Item().PaddingTop(2).Text($"Formations : {string.Join(" • ", c.Formations)}").FontSize(9).FontColor(Colors.Grey.Darken2);
                                }
                            });
                        }
                    });

                    if (competencesRequises.Count == 0)
                    {
                        col.Item().PaddingTop(12).Text("Note : aucune compétence requise disponible pour le poste/profil — enrichir le référentiel pour un matching plus fin.")
                            .FontSize(9).FontColor(Colors.Grey.Darken2);
                    }
                });

                page.Footer().AlignCenter().Text("SIRH.EY — Export comparaison remplaçants (prototype)").FontSize(9).FontColor(Colors.Grey.Darken2);
            });
        }).GeneratePdf();

        static IContainer CellHeader(IContainer c) =>
            c.PaddingVertical(6).PaddingHorizontal(6).Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2);

        static IContainer CellBody(IContainer c) =>
            c.PaddingVertical(6).PaddingHorizontal(6).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);

        static IContainer CellFooter(IContainer c) =>
            c.PaddingVertical(8).PaddingHorizontal(6).Background(Colors.Grey.Lighten5).BorderTop(1).BorderColor(Colors.Grey.Lighten2);
    }
}

