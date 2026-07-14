using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class AddFormationCompetenceSkillBridge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SkillId",
                table: "Formations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SkillId",
                table: "Competences",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Formations_SkillId",
                table: "Formations",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_Competences_SkillId",
                table: "Competences",
                column: "SkillId");

            migrationBuilder.AddForeignKey(
                name: "FK_Competences_Skills_SkillId",
                table: "Competences",
                column: "SkillId",
                principalTable: "Skills",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Formations_Skills_SkillId",
                table: "Formations",
                column: "SkillId",
                principalTable: "Skills",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Competences_Skills_SkillId",
                table: "Competences");

            migrationBuilder.DropForeignKey(
                name: "FK_Formations_Skills_SkillId",
                table: "Formations");

            migrationBuilder.DropIndex(
                name: "IX_Formations_SkillId",
                table: "Formations");

            migrationBuilder.DropIndex(
                name: "IX_Competences_SkillId",
                table: "Competences");

            migrationBuilder.DropColumn(
                name: "SkillId",
                table: "Formations");

            migrationBuilder.DropColumn(
                name: "SkillId",
                table: "Competences");
        }
    }
}
