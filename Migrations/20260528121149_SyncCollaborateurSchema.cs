using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class SyncCollaborateurSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartementId",
                table: "Collaborateurs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PosteId",
                table: "Collaborateurs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleRH",
                table: "Collaborateurs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepartementId",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "PosteId",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "RoleRH",
                table: "Collaborateurs");
        }
    }
}
