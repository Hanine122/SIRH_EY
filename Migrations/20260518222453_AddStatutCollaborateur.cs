using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class AddStatutCollaborateur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Statut",
                table: "Collaborateurs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Statut",
                table: "Collaborateurs");
        }
    }
}
