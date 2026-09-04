using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class BackfillInscriptionStatut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rows created before the approval workflow existed were never gated — the new
            // Statut column must not retroactively lock them behind PendingApproval (its
            // default). Completed -> Completed, everything else was already freely accessible
            // -> InProgress.
            migrationBuilder.Sql(
                "UPDATE Inscriptions SET Statut = CASE WHEN Terminee = 1 THEN 4 ELSE 3 END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Inscriptions SET Statut = 0;");
        }
    }
}
