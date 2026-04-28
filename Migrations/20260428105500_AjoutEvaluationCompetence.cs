using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SIRH.EY.Data;

#nullable disable

namespace SIRH.EY.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260428105500_AjoutEvaluationCompetence")]
    public partial class AjoutEvaluationCompetence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvaluationsCompetences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompetenceId = table.Column<int>(type: "int", nullable: false),
                    SeuilRh = table.Column<int>(type: "int", nullable: false),
                    AutoEvaluationCollaborateur = table.Column<int>(type: "int", nullable: false),
                    EvaluationManager = table.Column<int>(type: "int", nullable: true),
                    ValidationManager = table.Column<bool>(type: "bit", nullable: false),
                    DateAutoEvaluation = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateValidationManager = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CommentaireCollaborateur = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CommentaireManager = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationsCompetences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationsCompetences_Competences_CompetenceId",
                        column: x => x.CompetenceId,
                        principalTable: "Competences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationsCompetences_CompetenceId",
                table: "EvaluationsCompetences",
                column: "CompetenceId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvaluationsCompetences");
        }
    }
}
