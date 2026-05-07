using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class InitClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Collaborateurs_AspNetUsers_UserId",
                table: "Collaborateurs");

            migrationBuilder.DropForeignKey(
                name: "FK_Inscriptions_Collaborateurs_CollaborateurId",
                table: "Inscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Inscriptions_Formations_FormationId",
                table: "Inscriptions");

            migrationBuilder.DropTable(
                name: "EvaluationHistorique");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Collaborateurs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddForeignKey(
                name: "FK_Collaborateurs_AspNetUsers_UserId",
                table: "Collaborateurs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Inscriptions_Collaborateurs_CollaborateurId",
                table: "Inscriptions",
                column: "CollaborateurId",
                principalTable: "Collaborateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inscriptions_Formations_FormationId",
                table: "Inscriptions",
                column: "FormationId",
                principalTable: "Formations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Collaborateurs_AspNetUsers_UserId",
                table: "Collaborateurs");

            migrationBuilder.DropForeignKey(
                name: "FK_Inscriptions_Collaborateurs_CollaborateurId",
                table: "Inscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Inscriptions_Formations_FormationId",
                table: "Inscriptions");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Collaborateurs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "EvaluationHistorique",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompetenceId = table.Column<int>(type: "int", nullable: false),
                    DateChangement = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NiveauAncien = table.Column<int>(type: "int", nullable: false),
                    NiveauNouveau = table.Column<int>(type: "int", nullable: false),
                    Raison = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationHistorique", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationHistorique_Competences_CompetenceId",
                        column: x => x.CompetenceId,
                        principalTable: "Competences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationHistorique_CompetenceId",
                table: "EvaluationHistorique",
                column: "CompetenceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Collaborateurs_AspNetUsers_UserId",
                table: "Collaborateurs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Inscriptions_Collaborateurs_CollaborateurId",
                table: "Inscriptions",
                column: "CollaborateurId",
                principalTable: "Collaborateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inscriptions_Formations_FormationId",
                table: "Inscriptions",
                column: "FormationId",
                principalTable: "Formations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
