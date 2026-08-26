using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesGroupTeamHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SalesTeamId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SalesGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GroupLeaderId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesGroups_Users_GroupLeaderId",
                        column: x => x.GroupLeaderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SalesGroupTargets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SalesGroupId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<DateOnly>(type: "date", nullable: false),
                    UnitTarget = table.Column<int>(type: "int", nullable: false),
                    CollectionTarget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesGroupTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesGroupTargets_SalesGroups_SalesGroupId",
                        column: x => x.SalesGroupId,
                        principalTable: "SalesGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesGroupTargets_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SalesTeams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SalesGroupId = table.Column<int>(type: "int", nullable: false),
                    ParentTeamId = table.Column<int>(type: "int", nullable: true),
                    TeamLeaderId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesTeams_SalesGroups_SalesGroupId",
                        column: x => x.SalesGroupId,
                        principalTable: "SalesGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesTeams_SalesTeams_ParentTeamId",
                        column: x => x.ParentTeamId,
                        principalTable: "SalesTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesTeams_Users_TeamLeaderId",
                        column: x => x.TeamLeaderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SalesTeamTargets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SalesTeamId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<DateOnly>(type: "date", nullable: false),
                    UnitTarget = table.Column<int>(type: "int", nullable: false),
                    CollectionTarget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesTeamTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesTeamTargets_SalesTeams_SalesTeamId",
                        column: x => x.SalesTeamId,
                        principalTable: "SalesTeams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesTeamTargets_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SalesTeamId",
                table: "Users",
                column: "SalesTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesGroups_GroupLeaderId",
                table: "SalesGroups",
                column: "GroupLeaderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesGroups_Name",
                table: "SalesGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesGroupTargets_SalesGroupId_Month",
                table: "SalesGroupTargets",
                columns: new[] { "SalesGroupId", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesGroupTargets_UpdatedById",
                table: "SalesGroupTargets",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_SalesTeams_ParentTeamId",
                table: "SalesTeams",
                column: "ParentTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesTeams_SalesGroupId_Name",
                table: "SalesTeams",
                columns: new[] { "SalesGroupId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesTeams_TeamLeaderId",
                table: "SalesTeams",
                column: "TeamLeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesTeamTargets_SalesTeamId_Month",
                table: "SalesTeamTargets",
                columns: new[] { "SalesTeamId", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesTeamTargets_UpdatedById",
                table: "SalesTeamTargets",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_SalesTeams_SalesTeamId",
                table: "Users",
                column: "SalesTeamId",
                principalTable: "SalesTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_SalesTeams_SalesTeamId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "SalesGroupTargets");

            migrationBuilder.DropTable(
                name: "SalesTeamTargets");

            migrationBuilder.DropTable(
                name: "SalesTeams");

            migrationBuilder.DropTable(
                name: "SalesGroups");

            migrationBuilder.DropIndex(
                name: "IX_Users_SalesTeamId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SalesTeamId",
                table: "Users");
        }
    }
}
