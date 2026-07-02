using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillOntologyAndGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprouveParId",
                table: "TalentEvaluations",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateApprobation",
                table: "TalentEvaluations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCycleId",
                table: "TalentEvaluations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Statut",
                table: "TalentEvaluations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    AncienneValeur = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NouvelleValeur = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UtilisateurId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DateAction = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_AspNetUsers_UtilisateurId",
                        column: x => x.UtilisateurId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DecisionRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParametresJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    DateEffet = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    ModifieParId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecisionRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DecisionRules_AspNetUsers_ModifieParId",
                        column: x => x.ModifieParId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReviewCycles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateDebut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    Perimetre = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillCategories_SkillCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "SkillCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SuccessionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CollaborateurTitulaireId = table.Column<int>(type: "int", nullable: true),
                    Poste = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Departement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewCycleId = table.Column<int>(type: "int", nullable: true),
                    Statut = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProposeParId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DateValidationManager = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprouveParId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DateApprobationRH = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CommentaireRefus = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuccessionPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuccessionPlans_AspNetUsers_ApprouveParId",
                        column: x => x.ApprouveParId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SuccessionPlans_AspNetUsers_ProposeParId",
                        column: x => x.ProposeParId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SuccessionPlans_Collaborateurs_CollaborateurTitulaireId",
                        column: x => x.CollaborateurTitulaireId,
                        principalTable: "Collaborateurs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SuccessionPlans_ReviewCycles_ReviewCycleId",
                        column: x => x.ReviewCycleId,
                        principalTable: "ReviewCycles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SkillCategoryId = table.Column<int>(type: "int", nullable: true),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Skills_SkillCategories_SkillCategoryId",
                        column: x => x.SkillCategoryId,
                        principalTable: "SkillCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SuccessorRankingSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SuccessionPlanId = table.Column<int>(type: "int", nullable: false),
                    CandidatId = table.Column<int>(type: "int", nullable: false),
                    Rang = table.Column<int>(type: "int", nullable: false),
                    ScoreSuccession = table.Column<int>(type: "int", nullable: false),
                    ScoreCouverture = table.Column<int>(type: "int", nullable: false),
                    Horizon = table.Column<int>(type: "int", nullable: false),
                    DateSnapshot = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuccessorRankingSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuccessorRankingSnapshots_Collaborateurs_CandidatId",
                        column: x => x.CandidatId,
                        principalTable: "Collaborateurs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SuccessorRankingSnapshots_SuccessionPlans_SuccessionPlanId",
                        column: x => x.SuccessionPlanId,
                        principalTable: "SuccessionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillAliases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SkillId = table.Column<int>(type: "int", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillAliases_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillCriticalities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SkillId = table.Column<int>(type: "int", nullable: false),
                    Niveau = table.Column<int>(type: "int", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Perimetre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateEvaluation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillCriticalities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillCriticalities_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SkillId = table.Column<int>(type: "int", nullable: false),
                    Niveau = table.Column<int>(type: "int", nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CriteresValidation = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillLevels_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillRelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceSkillId = table.Column<int>(type: "int", nullable: false),
                    TargetSkillId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillRelations_Skills_SourceSkillId",
                        column: x => x.SourceSkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SkillRelations_Skills_TargetSkillId",
                        column: x => x.TargetSkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SkillVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SkillId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateEffet = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false),
                    ModifieParId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillVersions_AspNetUsers_ModifieParId",
                        column: x => x.ModifieParId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SkillVersions_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TalentEvaluations_ApprouveParId",
                table: "TalentEvaluations",
                column: "ApprouveParId");

            migrationBuilder.CreateIndex(
                name: "IX_TalentEvaluations_ReviewCycleId",
                table: "TalentEvaluations",
                column: "ReviewCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UtilisateurId",
                table: "AuditLogs",
                column: "UtilisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_DecisionRules_ModifieParId",
                table: "DecisionRules",
                column: "ModifieParId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillAliases_SkillId",
                table: "SkillAliases",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCategories_ParentCategoryId",
                table: "SkillCategories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCriticalities_SkillId",
                table: "SkillCriticalities",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillLevels_SkillId",
                table: "SkillLevels",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillRelations_SourceSkillId",
                table: "SkillRelations",
                column: "SourceSkillId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillRelations_TargetSkillId",
                table: "SkillRelations",
                column: "TargetSkillId");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_SkillCategoryId",
                table: "Skills",
                column: "SkillCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillVersions_ModifieParId",
                table: "SkillVersions",
                column: "ModifieParId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillVersions_SkillId",
                table: "SkillVersions",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_SuccessionPlans_ApprouveParId",
                table: "SuccessionPlans",
                column: "ApprouveParId");

            migrationBuilder.CreateIndex(
                name: "IX_SuccessionPlans_CollaborateurTitulaireId",
                table: "SuccessionPlans",
                column: "CollaborateurTitulaireId");

            migrationBuilder.CreateIndex(
                name: "IX_SuccessionPlans_ProposeParId",
                table: "SuccessionPlans",
                column: "ProposeParId");

            migrationBuilder.CreateIndex(
                name: "IX_SuccessionPlans_ReviewCycleId",
                table: "SuccessionPlans",
                column: "ReviewCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_SuccessorRankingSnapshots_CandidatId",
                table: "SuccessorRankingSnapshots",
                column: "CandidatId");

            migrationBuilder.CreateIndex(
                name: "IX_SuccessorRankingSnapshots_SuccessionPlanId",
                table: "SuccessorRankingSnapshots",
                column: "SuccessionPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_TalentEvaluations_AspNetUsers_ApprouveParId",
                table: "TalentEvaluations",
                column: "ApprouveParId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TalentEvaluations_ReviewCycles_ReviewCycleId",
                table: "TalentEvaluations",
                column: "ReviewCycleId",
                principalTable: "ReviewCycles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TalentEvaluations_AspNetUsers_ApprouveParId",
                table: "TalentEvaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_TalentEvaluations_ReviewCycles_ReviewCycleId",
                table: "TalentEvaluations");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "DecisionRules");

            migrationBuilder.DropTable(
                name: "SkillAliases");

            migrationBuilder.DropTable(
                name: "SkillCriticalities");

            migrationBuilder.DropTable(
                name: "SkillLevels");

            migrationBuilder.DropTable(
                name: "SkillRelations");

            migrationBuilder.DropTable(
                name: "SkillVersions");

            migrationBuilder.DropTable(
                name: "SuccessorRankingSnapshots");

            migrationBuilder.DropTable(
                name: "Skills");

            migrationBuilder.DropTable(
                name: "SuccessionPlans");

            migrationBuilder.DropTable(
                name: "SkillCategories");

            migrationBuilder.DropTable(
                name: "ReviewCycles");

            migrationBuilder.DropIndex(
                name: "IX_TalentEvaluations_ApprouveParId",
                table: "TalentEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_TalentEvaluations_ReviewCycleId",
                table: "TalentEvaluations");

            migrationBuilder.DropColumn(
                name: "ApprouveParId",
                table: "TalentEvaluations");

            migrationBuilder.DropColumn(
                name: "DateApprobation",
                table: "TalentEvaluations");

            migrationBuilder.DropColumn(
                name: "ReviewCycleId",
                table: "TalentEvaluations");

            migrationBuilder.DropColumn(
                name: "Statut",
                table: "TalentEvaluations");
        }
    }
}
