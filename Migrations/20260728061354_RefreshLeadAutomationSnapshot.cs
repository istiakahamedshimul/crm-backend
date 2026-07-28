using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class RefreshLeadAutomationSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "Leads",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAssignmentReminderAt",
                table: "Leads",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.Sql("UPDATE Leads SET AssignedAt = CreatedAt WHERE AssignedToId IS NOT NULL AND Status = 1");

            migrationBuilder.CreateTable(
                name: "LeadAutomationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UnassignAfterHours = table.Column<int>(type: "int", nullable: false),
                    ReminderIntervalHours = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadAutomationSettings", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(
                "INSERT INTO LeadAutomationSettings (Id, UnassignAfterHours, ReminderIntervalHours, UpdatedAt) VALUES (1, 24, 1, UTC_TIMESTAMP(6))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeadAutomationSettings");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "LastAssignmentReminderAt",
                table: "Leads");
        }
    }
}
