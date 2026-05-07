using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class AddUserToCollaborateur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Collaborateurs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Collaborateurs_UserId",
                table: "Collaborateurs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Collaborateurs_AspNetUsers_UserId",
                table: "Collaborateurs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Collaborateurs_AspNetUsers_UserId",
                table: "Collaborateurs");

            migrationBuilder.DropIndex(
                name: "IX_Collaborateurs_UserId",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Collaborateurs");
        }
    }
}
