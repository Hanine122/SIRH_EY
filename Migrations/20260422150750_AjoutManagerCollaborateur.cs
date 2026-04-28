using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class AjoutManagerCollaborateur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ManagerId",
                table: "Collaborateurs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Collaborateurs_ManagerId",
                table: "Collaborateurs",
                column: "ManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Collaborateurs_Collaborateurs_ManagerId",
                table: "Collaborateurs",
                column: "ManagerId",
                principalTable: "Collaborateurs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Collaborateurs_Collaborateurs_ManagerId",
                table: "Collaborateurs");

            migrationBuilder.DropIndex(
                name: "IX_Collaborateurs_ManagerId",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "ManagerId",
                table: "Collaborateurs");
        }
    }
}
