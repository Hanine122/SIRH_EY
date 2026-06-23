using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class AddCertifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExperienceDomainAnnees",
                table: "Collaborateurs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModeDeploiement",
                table: "Collaborateurs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NombreImplementations",
                table: "Collaborateurs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Certifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Organisme = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Domaine = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CodeExamen = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EstReconnue = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GradeReferentiels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Grade = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NiveauMinCompetences = table.Column<double>(type: "float", nullable: false),
                    AncienneteMinAns = table.Column<int>(type: "int", nullable: false),
                    NombreImplementationsMin = table.Column<int>(type: "int", nullable: false),
                    ExperienceDomainMinAns = table.Column<int>(type: "int", nullable: false),
                    GradeSuivant = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Niveau = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeReferentiels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CollaborateurCertifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CollaborateurId = table.Column<int>(type: "int", nullable: false),
                    CertificationId = table.Column<int>(type: "int", nullable: false),
                    DateObtention = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateExpiration = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Statut = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollaborateurCertifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollaborateurCertifications_Certifications_CertificationId",
                        column: x => x.CertificationId,
                        principalTable: "Certifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CollaborateurCertifications_Collaborateurs_CollaborateurId",
                        column: x => x.CollaborateurId,
                        principalTable: "Collaborateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollaborateurCertifications_CertificationId",
                table: "CollaborateurCertifications",
                column: "CertificationId");

            migrationBuilder.CreateIndex(
                name: "IX_CollaborateurCertifications_CollaborateurId",
                table: "CollaborateurCertifications",
                column: "CollaborateurId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollaborateurCertifications");

            migrationBuilder.DropTable(
                name: "GradeReferentiels");

            migrationBuilder.DropTable(
                name: "Certifications");

            migrationBuilder.DropColumn(
                name: "ExperienceDomainAnnees",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "ModeDeploiement",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "NombreImplementations",
                table: "Collaborateurs");
        }
    }
}
