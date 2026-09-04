using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Statut",
                table: "Inscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateValidationManager",
                table: "EvaluationsSuiviFormation",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NiveauValide",
                table: "EvaluationsSuiviFormation",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ValidationManager",
                table: "EvaluationsSuiviFormation",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CollaborateurId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Lien = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Lu = table.Column<bool>(type: "bit", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Collaborateurs_CollaborateurId",
                        column: x => x.CollaborateurId,
                        principalTable: "Collaborateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CollaborateurId",
                table: "Notifications",
                column: "CollaborateurId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropColumn(
                name: "Statut",
                table: "Inscriptions");

            migrationBuilder.DropColumn(
                name: "DateValidationManager",
                table: "EvaluationsSuiviFormation");

            migrationBuilder.DropColumn(
                name: "NiveauValide",
                table: "EvaluationsSuiviFormation");

            migrationBuilder.DropColumn(
                name: "ValidationManager",
                table: "EvaluationsSuiviFormation");
        }
    }
}
