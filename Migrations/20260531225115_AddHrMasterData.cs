using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIRH.EY.Migrations
{
    /// <inheritdoc />
    public partial class AddHrMasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Collaborateurs_Collaborateurs_ManagerId",
                table: "Collaborateurs");

            migrationBuilder.RenameColumn(
                name: "PosteId",
                table: "Collaborateurs",
                newName: "SubDepartmentId");

            migrationBuilder.RenameColumn(
                name: "DepartementId",
                table: "Collaborateurs",
                newName: "PositionId");

            migrationBuilder.AddColumn<int>(
                name: "BusinessUnitId",
                table: "Collaborateurs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Collaborateurs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GradeId",
                table: "Collaborateurs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "Collaborateurs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessUnits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationsHistoriques",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompetenceId = table.Column<int>(type: "int", nullable: false),
                    NiveauAncien = table.Column<int>(type: "int", nullable: false),
                    NiveauNouveau = table.Column<int>(type: "int", nullable: false),
                    DateChangement = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Raison = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationsHistoriques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationsHistoriques_Competences_CompetenceId",
                        column: x => x.CompetenceId,
                        principalTable: "Competences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Grades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsEditable = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemParameters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubDepartments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubDepartments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubDepartments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SubDepartmentId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_SubDepartments_SubDepartmentId",
                        column: x => x.SubDepartmentId,
                        principalTable: "SubDepartments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Collaborateurs_BusinessUnitId",
                table: "Collaborateurs",
                column: "BusinessUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Collaborateurs_DepartmentId",
                table: "Collaborateurs",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Collaborateurs_GradeId",
                table: "Collaborateurs",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Collaborateurs_LocationId",
                table: "Collaborateurs",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Collaborateurs_PositionId",
                table: "Collaborateurs",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Collaborateurs_SubDepartmentId",
                table: "Collaborateurs",
                column: "SubDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationsHistoriques_CompetenceId",
                table: "EvaluationsHistoriques",
                column: "CompetenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_SubDepartmentId",
                table: "Positions",
                column: "SubDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SubDepartments_DepartmentId",
                table: "SubDepartments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemParameters_Key",
                table: "SystemParameters",
                column: "Key",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Collaborateurs_BusinessUnits_BusinessUnitId",
                table: "Collaborateurs",
                column: "BusinessUnitId",
                principalTable: "BusinessUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Collaborateurs_Collaborateurs_ManagerId",
                table: "Collaborateurs",
                column: "ManagerId",
                principalTable: "Collaborateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Collaborateurs_Departments_DepartmentId",
                table: "Collaborateurs",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Collaborateurs_Grades_GradeId",
                table: "Collaborateurs",
                column: "GradeId",
                principalTable: "Grades",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Collaborateurs_Locations_LocationId",
                table: "Collaborateurs",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Collaborateurs_Positions_PositionId",
                table: "Collaborateurs",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Collaborateurs_SubDepartments_SubDepartmentId",
                table: "Collaborateurs",
                column: "SubDepartmentId",
                principalTable: "SubDepartments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Collaborateurs_BusinessUnits_BusinessUnitId",
                table: "Collaborateurs");

            migrationBuilder.DropForeignKey(
                name: "FK_Collaborateurs_Collaborateurs_ManagerId",
                table: "Collaborateurs");

            migrationBuilder.DropForeignKey(
                name: "FK_Collaborateurs_Departments_DepartmentId",
                table: "Collaborateurs");

            migrationBuilder.DropForeignKey(
                name: "FK_Collaborateurs_Grades_GradeId",
                table: "Collaborateurs");

            migrationBuilder.DropForeignKey(
                name: "FK_Collaborateurs_Locations_LocationId",
                table: "Collaborateurs");

            migrationBuilder.DropForeignKey(
                name: "FK_Collaborateurs_Positions_PositionId",
                table: "Collaborateurs");

            migrationBuilder.DropForeignKey(
                name: "FK_Collaborateurs_SubDepartments_SubDepartmentId",
                table: "Collaborateurs");

            migrationBuilder.DropTable(
                name: "BusinessUnits");

            migrationBuilder.DropTable(
                name: "EvaluationsHistoriques");

            migrationBuilder.DropTable(
                name: "Grades");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "SystemParameters");

            migrationBuilder.DropTable(
                name: "SubDepartments");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Collaborateurs_BusinessUnitId",
                table: "Collaborateurs");

            migrationBuilder.DropIndex(
                name: "IX_Collaborateurs_DepartmentId",
                table: "Collaborateurs");

            migrationBuilder.DropIndex(
                name: "IX_Collaborateurs_GradeId",
                table: "Collaborateurs");

            migrationBuilder.DropIndex(
                name: "IX_Collaborateurs_LocationId",
                table: "Collaborateurs");

            migrationBuilder.DropIndex(
                name: "IX_Collaborateurs_PositionId",
                table: "Collaborateurs");

            migrationBuilder.DropIndex(
                name: "IX_Collaborateurs_SubDepartmentId",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "BusinessUnitId",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "GradeId",
                table: "Collaborateurs");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Collaborateurs");

            migrationBuilder.RenameColumn(
                name: "SubDepartmentId",
                table: "Collaborateurs",
                newName: "PosteId");

            migrationBuilder.RenameColumn(
                name: "PositionId",
                table: "Collaborateurs",
                newName: "DepartementId");

            migrationBuilder.AddForeignKey(
                name: "FK_Collaborateurs_Collaborateurs_ManagerId",
                table: "Collaborateurs",
                column: "ManagerId",
                principalTable: "Collaborateurs",
                principalColumn: "Id");
        }
    }
}
