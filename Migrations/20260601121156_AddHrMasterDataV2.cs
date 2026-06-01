using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class AddHrMasterDataV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SubDepartments",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "SubDepartments",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "SubDepartments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "SubDepartments",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Positions",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Positions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Positions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Positions",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Locations",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Locations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Locations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Locations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Grades",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Grades",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Grades",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Grades",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Departments",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Departments",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Departments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Departments",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContractTypeId",
                table: "Collaborateurs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "BusinessUnits",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "BusinessUnits",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "BusinessUnits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "BusinessUnits",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContractTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxDurationMonths = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PositionGradeEligibilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PositionId = table.Column<int>(type: "int", nullable: false),
                    GradeEntityId = table.Column<int>(type: "int", nullable: false),
                    IsMinimum = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionGradeEligibilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PositionGradeEligibilities_Grades_GradeEntityId",
                        column: x => x.GradeEntityId,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PositionGradeEligibilities_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PositionMandatoryFormations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PositionId = table.Column<int>(type: "int", nullable: false),
                    FormationId = table.Column<int>(type: "int", nullable: false),
                    DeadlineMonths = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionMandatoryFormations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PositionMandatoryFormations_Formations_FormationId",
                        column: x => x.FormationId,
                        principalTable: "Formations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PositionMandatoryFormations_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PositionRequiredCompetences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PositionId = table.Column<int>(type: "int", nullable: false),
                    CompetenceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequiredLevel = table.Column<int>(type: "int", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PositionRequiredCompetences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PositionRequiredCompetences_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Collaborateurs_ContractTypeId",
                table: "Collaborateurs",
                column: "ContractTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionGradeEligibilities_GradeEntityId",
                table: "PositionGradeEligibilities",
                column: "GradeEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionGradeEligibilities_PositionId",
                table: "PositionGradeEligibilities",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionMandatoryFormations_FormationId",
                table: "PositionMandatoryFormations",
                column: "FormationId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionMandatoryFormations_PositionId",
                table: "PositionMandatoryFormations",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionRequiredCompetences_PositionId",
                table: "PositionRequiredCompetences",
                column: "PositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Collaborateurs_ContractTypes_ContractTypeId",
                table: "Collaborateurs",
                column: "ContractTypeId",
                principalTable: "ContractTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Collaborateurs_ContractTypes_ContractTypeId",
                table: "Collaborateurs");

            migrationBuilder.DropTable(
                name: "ContractTypes");

            migrationBuilder.DropTable(
                name: "PositionGradeEligibilities");

            migrationBuilder.DropTable(
                name: "PositionMandatoryFormations");

            migrationBuilder.DropTable(
                name: "PositionRequiredCompetences");

            migrationBuilder.DropIndex(
                name: "IX_Collaborateurs_ContractTypeId",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SubDepartments");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SubDepartments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "SubDepartments");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "SubDepartments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "ContractTypeId",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "BusinessUnits");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "BusinessUnits");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "BusinessUnits");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "BusinessUnits");
        }
    }
}
