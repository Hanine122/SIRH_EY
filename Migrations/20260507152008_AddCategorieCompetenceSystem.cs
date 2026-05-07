using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class AddCategorieCompetenceSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Categorie",
                table: "Competences");

            migrationBuilder.AddColumn<int>(
                name: "CategorieCompetenceId",
                table: "Competences",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CategoriesCompetences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriesCompetences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Competences_CategorieCompetenceId",
                table: "Competences",
                column: "CategorieCompetenceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Competences_CategoriesCompetences_CategorieCompetenceId",
                table: "Competences",
                column: "CategorieCompetenceId",
                principalTable: "CategoriesCompetences",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Competences_CategoriesCompetences_CategorieCompetenceId",
                table: "Competences");

            migrationBuilder.DropTable(
                name: "CategoriesCompetences");

            migrationBuilder.DropIndex(
                name: "IX_Competences_CategorieCompetenceId",
                table: "Competences");

            migrationBuilder.DropColumn(
                name: "CategorieCompetenceId",
                table: "Competences");

            migrationBuilder.AddColumn<string>(
                name: "Categorie",
                table: "Competences",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
