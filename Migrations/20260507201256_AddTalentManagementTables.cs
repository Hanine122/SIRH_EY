using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class AddTalentManagementTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OKRs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CollaborateurId = table.Column<int>(type: "int", nullable: false),
                    Objectif = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Annee = table.Column<int>(type: "int", nullable: false),
                    Trimestre = table.Column<int>(type: "int", nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    ProgressionGlobale = table.Column<int>(type: "int", nullable: false),
                    DateDebut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateFinCible = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateRealisation = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ManagerId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ValideParManager = table.Column<bool>(type: "bit", nullable: false),
                    DateValidation = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OKRs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OKRs_AspNetUsers_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OKRs_Collaborateurs_CollaborateurId",
                        column: x => x.CollaborateurId,
                        principalTable: "Collaborateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TalentEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CollaborateurId = table.Column<int>(type: "int", nullable: false),
                    PerformanceScore = table.Column<int>(type: "int", nullable: false),
                    PotentielScore = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    CommentairesPerformance = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommentairesPotentiel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvaluateurId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DateEvaluation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TalentEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TalentEvaluations_AspNetUsers_EvaluateurId",
                        column: x => x.EvaluateurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TalentEvaluations_Collaborateurs_CollaborateurId",
                        column: x => x.CollaborateurId,
                        principalTable: "Collaborateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KeyResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OKRId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ValeurCible = table.Column<double>(type: "float", nullable: false),
                    ValeurActuelle = table.Column<double>(type: "float", nullable: false),
                    Unite = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Difficulte = table.Column<int>(type: "int", nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeyResults_OKRs_OKRId",
                        column: x => x.OKRId,
                        principalTable: "OKRs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KeyResults_OKRId",
                table: "KeyResults",
                column: "OKRId");

            migrationBuilder.CreateIndex(
                name: "IX_OKRs_CollaborateurId",
                table: "OKRs",
                column: "CollaborateurId");

            migrationBuilder.CreateIndex(
                name: "IX_OKRs_ManagerId",
                table: "OKRs",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_TalentEvaluations_CollaborateurId",
                table: "TalentEvaluations",
                column: "CollaborateurId");

            migrationBuilder.CreateIndex(
                name: "IX_TalentEvaluations_EvaluateurId",
                table: "TalentEvaluations",
                column: "EvaluateurId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KeyResults");

            migrationBuilder.DropTable(
                name: "TalentEvaluations");

            migrationBuilder.DropTable(
                name: "OKRs");
        }
    }
}
