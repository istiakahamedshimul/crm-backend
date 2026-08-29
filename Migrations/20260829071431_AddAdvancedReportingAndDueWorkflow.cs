using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedReportingAndDueWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MonthlyCollections",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "CustomerDues",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.AddColumn<DateTime>(
                name: "NotificationSentAt",
                table: "CustomerDues",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "CustomerDues",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaidById",
                table: "CustomerDues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaidRemarks",
                table: "CustomerDues",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "CustomerDues",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE `CustomerDues` SET `DueDate` = STR_TO_DATE(CONCAT(DATE_FORMAT(`Month`, '%Y-%m'), '-01'), '%Y-%m-%d')");

            migrationBuilder.CreateTable(
                name: "CustomerDueAudits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CustomerDueId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedById = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Remarks = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerDueAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerDueAudits_CustomerDues_CustomerDueId",
                        column: x => x.CustomerDueId,
                        principalTable: "CustomerDues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LeadAssignmentHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LeadId = table.Column<int>(type: "int", nullable: false),
                    FromSalesExecutiveId = table.Column<int>(type: "int", nullable: true),
                    ToSalesExecutiveId = table.Column<int>(type: "int", nullable: true),
                    ChangedById = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Reason = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadAssignmentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadAssignmentHistories_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LeadStatusHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LeadId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedById = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadStatusHistories_Leads_LeadId",
                        column: x => x.LeadId,
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeadStatusHistories_Users_ChangedById",
                        column: x => x.ChangedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MonthlyCollectionAudits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MonthlyCollectionId = table.Column<int>(type: "int", nullable: false),
                    PreviousAmount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    NewAmount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    PreviousRemarks = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewRemarks = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangedById = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyCollectionAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyCollectionAudits_MonthlyCollections_MonthlyCollection~",
                        column: x => x.MonthlyCollectionId,
                        principalTable: "MonthlyCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReportAccessAudits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ReportKey = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Action = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FiltersJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportAccessAudits", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDues_PaidById",
                table: "CustomerDues",
                column: "PaidById");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerDueAudits_CustomerDueId",
                table: "CustomerDueAudits",
                column: "CustomerDueId");

            migrationBuilder.CreateIndex(
                name: "IX_LeadAssignmentHistories_LeadId_ChangedAt",
                table: "LeadAssignmentHistories",
                columns: new[] { "LeadId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LeadStatusHistories_ChangedById",
                table: "LeadStatusHistories",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_LeadStatusHistories_LeadId_ChangedAt",
                table: "LeadStatusHistories",
                columns: new[] { "LeadId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyCollectionAudits_MonthlyCollectionId",
                table: "MonthlyCollectionAudits",
                column: "MonthlyCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportAccessAudits_UserId_CreatedAt",
                table: "ReportAccessAudits",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerDues_Users_PaidById",
                table: "CustomerDues",
                column: "PaidById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerDues_Users_PaidById",
                table: "CustomerDues");

            migrationBuilder.DropTable(
                name: "CustomerDueAudits");

            migrationBuilder.DropTable(
                name: "LeadAssignmentHistories");

            migrationBuilder.DropTable(
                name: "LeadStatusHistories");

            migrationBuilder.DropTable(
                name: "MonthlyCollectionAudits");

            migrationBuilder.DropTable(
                name: "ReportAccessAudits");

            migrationBuilder.DropIndex(
                name: "IX_CustomerDues_PaidById",
                table: "CustomerDues");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MonthlyCollections");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "CustomerDues");

            migrationBuilder.DropColumn(
                name: "NotificationSentAt",
                table: "CustomerDues");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "CustomerDues");

            migrationBuilder.DropColumn(
                name: "PaidById",
                table: "CustomerDues");

            migrationBuilder.DropColumn(
                name: "PaidRemarks",
                table: "CustomerDues");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CustomerDues");
        }
    }
}
