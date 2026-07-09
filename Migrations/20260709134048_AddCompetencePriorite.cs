using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetencePriorite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Priorite",
                table: "CompetencesRequisesParPoste",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Prioritaire");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priorite",
                table: "CompetencesRequisesParPoste");
        }
    }
}
