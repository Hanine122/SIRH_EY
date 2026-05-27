using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class HrProfileAndFormationCatalogColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DepartementCible",
                table: "Formations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DomaineCompetence",
                table: "Formations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EstCertifiante",
                table: "Formations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MetierCible",
                table: "Formations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NiveauDifficulte",
                table: "Formations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PosteCible",
                table: "Formations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Adresse",
                table: "Collaborateurs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessUnit",
                table: "Collaborateurs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactUrgence",
                table: "Collaborateurs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateNaissance",
                table: "Collaborateurs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DatePrisePoste",
                table: "Collaborateurs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EtatCivil",
                table: "Collaborateurs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormationsObligatoires",
                table: "Collaborateurs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Genre",
                table: "Collaborateurs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Localisation",
                table: "Collaborateurs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Matricule",
                table: "Collaborateurs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nationalite",
                table: "Collaborateurs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NiveauHierarchique",
                table: "Collaborateurs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NiveauPreparationSuccession",
                table: "Collaborateurs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pays",
                table: "Collaborateurs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PotentielCarriere",
                table: "Collaborateurs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelephonePersonnel",
                table: "Collaborateurs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypeContrat",
                table: "Collaborateurs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ville",
                table: "Collaborateurs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepartementCible",
                table: "Formations");

            migrationBuilder.DropColumn(
                name: "DomaineCompetence",
                table: "Formations");

            migrationBuilder.DropColumn(
                name: "EstCertifiante",
                table: "Formations");

            migrationBuilder.DropColumn(
                name: "MetierCible",
                table: "Formations");

            migrationBuilder.DropColumn(
                name: "NiveauDifficulte",
                table: "Formations");

            migrationBuilder.DropColumn(
                name: "PosteCible",
                table: "Formations");

            migrationBuilder.DropColumn(
                name: "Adresse",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "BusinessUnit",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "ContactUrgence",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "DateNaissance",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "DatePrisePoste",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "EtatCivil",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "FormationsObligatoires",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "Genre",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "Localisation",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "Matricule",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "Nationalite",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "NiveauHierarchique",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "NiveauPreparationSuccession",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "Pays",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "PotentielCarriere",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "TelephonePersonnel",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "TypeContrat",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "Ville",
                table: "Collaborateurs");
        }
    }
}
