using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class CheckChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateCompletion",
                table: "Inscriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateExpiration",
                table: "Inscriptions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceCertification",
                table: "Inscriptions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificationNom",
                table: "Formations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompetencesRequises",
                table: "Formations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Formations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EstForteDemande",
                table: "Formations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EstStrategique",
                table: "Formations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ExternalUrl",
                table: "Formations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MentorEmail",
                table: "Formations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Plateforme",
                table: "Formations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportPdfUrl",
                table: "Formations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateCompletion",
                table: "Inscriptions");

            migrationBuilder.DropColumn(
                name: "DateExpiration",
                table: "Inscriptions");

            migrationBuilder.DropColumn(
                name: "SourceCertification",
                table: "Inscriptions");

            migrationBuilder.DropColumn(
                name: "CertificationNom",
                table: "Formations");

            migrationBuilder.DropColumn(
                name: "CompetencesRequises",
                table: "Formations");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Formations");

            migrationBuilder.DropColumn(
                name: "EstForteDemande",
                table: "Formations");

            migrationBuilder.DropColumn(
                name: "EstStrategique",
                table: "Formations");

            migrationBuilder.DropColumn(
                name: "ExternalUrl",
                table: "Formations");

            migrationBuilder.DropColumn(
                name: "MentorEmail",
                table: "Formations");

            migrationBuilder.DropColumn(
                name: "Plateforme",
                table: "Formations");

            migrationBuilder.DropColumn(
                name: "SupportPdfUrl",
                table: "Formations");
        }
    }
}
