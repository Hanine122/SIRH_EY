using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationPostFormation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvaluationsPostFormation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InscriptionId = table.Column<int>(type: "int", nullable: false),
                    NoteGlobale = table.Column<int>(type: "int", nullable: false),
                    NoteContenu = table.Column<int>(type: "int", nullable: true),
                    NoteFormateur = table.Column<int>(type: "int", nullable: true),
                    Recommande = table.Column<bool>(type: "bit", nullable: false),
                    Commentaire = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DateEvaluation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationsPostFormation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationsPostFormation_Inscriptions_InscriptionId",
                        column: x => x.InscriptionId,
                        principalTable: "Inscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationsPostFormation_InscriptionId",
                table: "EvaluationsPostFormation",
                column: "InscriptionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvaluationsPostFormation");
        }
    }
}
