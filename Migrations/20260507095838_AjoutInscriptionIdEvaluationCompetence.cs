using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class AjoutInscriptionIdEvaluationCompetence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InscriptionId",
                table: "EvaluationsCompetences",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationsCompetences_InscriptionId",
                table: "EvaluationsCompetences",
                column: "InscriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_EvaluationsCompetences_Inscriptions_InscriptionId",
                table: "EvaluationsCompetences",
                column: "InscriptionId",
                principalTable: "Inscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvaluationsCompetences_Inscriptions_InscriptionId",
                table: "EvaluationsCompetences");

            migrationBuilder.DropIndex(
                name: "IX_EvaluationsCompetences_InscriptionId",
                table: "EvaluationsCompetences");

            migrationBuilder.DropColumn(
                name: "InscriptionId",
                table: "EvaluationsCompetences");
        }
    }
}
