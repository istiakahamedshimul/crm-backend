using System;
using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace backend.Migrations;

[DbContext(typeof(CrmDbContext))]
[Migration("20260829065000_AddAdvancedReportingAndDueWorkflow")]
public partial class AddAdvancedReportingAndDueWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(name: "UpdatedAt", table: "MonthlyCollections", type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)");
        migrationBuilder.AddColumn<DateTime>(name: "DueDate", table: "CustomerDues", type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)");
        migrationBuilder.AddColumn<DateTime>(name: "NotificationSentAt", table: "CustomerDues", type: "datetime(6)", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "PaidAt", table: "CustomerDues", type: "datetime(6)", nullable: true);
        migrationBuilder.AddColumn<int>(name: "PaidById", table: "CustomerDues", type: "int", nullable: true);
        migrationBuilder.AddColumn<string>(name: "PaidRemarks", table: "CustomerDues", type: "longtext", nullable: true);
        migrationBuilder.AddColumn<int>(name: "Status", table: "CustomerDues", type: "int", nullable: false, defaultValue: 0);
        migrationBuilder.Sql("UPDATE `CustomerDues` SET `DueDate` = STR_TO_DATE(CONCAT(DATE_FORMAT(`Month`, '%Y-%m'), '-01'), '%Y-%m-%d')");
        migrationBuilder.CreateIndex(name: "IX_CustomerDues_PaidById", table: "CustomerDues", column: "PaidById");
        migrationBuilder.AddForeignKey(name: "FK_CustomerDues_Users_PaidById", table: "CustomerDues", column: "PaidById", principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.CreateTable(name: "LeadStatusHistories", columns: table => new { Id = table.Column<long>(type: "bigint", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn), LeadId = table.Column<int>(type: "int", nullable: false), FromStatus = table.Column<int>(type: "int", nullable: false), ToStatus = table.Column<int>(type: "int", nullable: false), ChangedById = table.Column<int>(type: "int", nullable: false), ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false) }, constraints: table => { table.PrimaryKey("PK_LeadStatusHistories", x => x.Id); table.ForeignKey("FK_LeadStatusHistories_Leads_LeadId", x => x.LeadId, "Leads", "Id", onDelete: ReferentialAction.Cascade); table.ForeignKey("FK_LeadStatusHistories_Users_ChangedById", x => x.ChangedById, "Users", "Id", onDelete: ReferentialAction.Restrict); });
        migrationBuilder.CreateTable(name: "LeadAssignmentHistories", columns: table => new { Id = table.Column<long>(type: "bigint", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn), LeadId = table.Column<int>(type: "int", nullable: false), FromSalesExecutiveId = table.Column<int>(type: "int", nullable: true), ToSalesExecutiveId = table.Column<int>(type: "int", nullable: true), ChangedById = table.Column<int>(type: "int", nullable: false), ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false), Reason = table.Column<string>(type: "longtext", nullable: true) }, constraints: table => { table.PrimaryKey("PK_LeadAssignmentHistories", x => x.Id); table.ForeignKey("FK_LeadAssignmentHistories_Leads_LeadId", x => x.LeadId, "Leads", "Id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateTable(name: "MonthlyCollectionAudits", columns: table => new { Id = table.Column<long>(type: "bigint", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn), MonthlyCollectionId = table.Column<int>(type: "int", nullable: false), PreviousAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false), NewAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false), PreviousRemarks = table.Column<string>(type: "longtext", nullable: true), NewRemarks = table.Column<string>(type: "longtext", nullable: true), ChangedById = table.Column<int>(type: "int", nullable: false), ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false) }, constraints: table => { table.PrimaryKey("PK_MonthlyCollectionAudits", x => x.Id); table.ForeignKey("FK_MonthlyCollectionAudits_MonthlyCollections_MonthlyCollectionId", x => x.MonthlyCollectionId, "MonthlyCollections", "Id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateTable(name: "CustomerDueAudits", columns: table => new { Id = table.Column<long>(type: "bigint", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn), CustomerDueId = table.Column<int>(type: "int", nullable: false), FromStatus = table.Column<int>(type: "int", nullable: false), ToStatus = table.Column<int>(type: "int", nullable: false), ChangedById = table.Column<int>(type: "int", nullable: false), ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false), Remarks = table.Column<string>(type: "longtext", nullable: true) }, constraints: table => { table.PrimaryKey("PK_CustomerDueAudits", x => x.Id); table.ForeignKey("FK_CustomerDueAudits_CustomerDues_CustomerDueId", x => x.CustomerDueId, "CustomerDues", "Id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateTable(name: "ReportAccessAudits", columns: table => new { Id = table.Column<long>(type: "bigint", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn), UserId = table.Column<int>(type: "int", nullable: false), ReportKey = table.Column<string>(type: "varchar(255)", nullable: false), Action = table.Column<string>(type: "varchar(100)", nullable: false), FiltersJson = table.Column<string>(type: "longtext", nullable: false), CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false) }, constraints: table => table.PrimaryKey("PK_ReportAccessAudits", x => x.Id));
        migrationBuilder.CreateIndex("IX_LeadStatusHistories_LeadId_ChangedAt", "LeadStatusHistories", new[] { "LeadId", "ChangedAt" }); migrationBuilder.CreateIndex("IX_LeadStatusHistories_ChangedById", "LeadStatusHistories", "ChangedById");
        migrationBuilder.CreateIndex("IX_LeadAssignmentHistories_LeadId_ChangedAt", "LeadAssignmentHistories", new[] { "LeadId", "ChangedAt" }); migrationBuilder.CreateIndex("IX_MonthlyCollectionAudits_MonthlyCollectionId", "MonthlyCollectionAudits", "MonthlyCollectionId"); migrationBuilder.CreateIndex("IX_CustomerDueAudits_CustomerDueId", "CustomerDueAudits", "CustomerDueId"); migrationBuilder.CreateIndex("IX_ReportAccessAudits_UserId_CreatedAt", "ReportAccessAudits", new[] { "UserId", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("LeadStatusHistories"); migrationBuilder.DropTable("LeadAssignmentHistories"); migrationBuilder.DropTable("MonthlyCollectionAudits"); migrationBuilder.DropTable("CustomerDueAudits"); migrationBuilder.DropTable("ReportAccessAudits");
        migrationBuilder.DropForeignKey("FK_CustomerDues_Users_PaidById", "CustomerDues"); migrationBuilder.DropIndex("IX_CustomerDues_PaidById", "CustomerDues");
        migrationBuilder.DropColumn("UpdatedAt", "MonthlyCollections"); migrationBuilder.DropColumn("DueDate", "CustomerDues"); migrationBuilder.DropColumn("NotificationSentAt", "CustomerDues"); migrationBuilder.DropColumn("PaidAt", "CustomerDues"); migrationBuilder.DropColumn("PaidById", "CustomerDues"); migrationBuilder.DropColumn("PaidRemarks", "CustomerDues"); migrationBuilder.DropColumn("Status", "CustomerDues");
    }
}
