using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationSuiviFormation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvaluationsSuiviFormation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InscriptionId = table.Column<int>(type: "int", nullable: false),
                    NoteApplicationCompetences = table.Column<int>(type: "int", nullable: false),
                    NoteImpactBusiness = table.Column<int>(type: "int", nullable: false),
                    ExemplesConcrets = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Commentaire = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DateEvaluation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationsSuiviFormation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationsSuiviFormation_Inscriptions_InscriptionId",
                        column: x => x.InscriptionId,
                        principalTable: "Inscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationsSuiviFormation_InscriptionId",
                table: "EvaluationsSuiviFormation",
                column: "InscriptionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvaluationsSuiviFormation");
        }
    }
}
