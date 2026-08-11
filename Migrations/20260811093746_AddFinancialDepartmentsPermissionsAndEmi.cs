using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialDepartmentsPermissionsAndEmi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Roles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Roles",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Payments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Payments",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "InstallmentId",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReversed",
                table: "Payments",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDate",
                table: "Payments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReversalReason",
                table: "Payments",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReversedAt",
                table: "Payments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReversedById",
                table: "Payments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionReference",
                table: "Payments",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Preserve the effective payment date for legacy production rows.
            migrationBuilder.Sql("UPDATE `Payments` SET `PaymentDate` = `CreatedAt` WHERE `PaymentDate` IS NULL;");
            migrationBuilder.AlterColumn<DateTime>(name: "PaymentDate", table: "Payments", type: "datetime(6)", nullable: false, oldClrType: typeof(DateTime), oldType: "datetime(6)", oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BookedAt",
                table: "Customers",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BookedById",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileId",
                table: "Customers",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "AppNotifications",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "DueAmount",
                table: "AppNotifications",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "AppNotifications",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileId",
                table: "AppNotifications",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "OutstandingBalance",
                table: "AppNotifications",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EntityType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Action = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DetailsJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PerformedById = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FinancialAgreements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    TotalAgreedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BookingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentPlan = table.Column<int>(type: "int", nullable: false),
                    EmiStartDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MonthlyEmiAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    InstallmentCount = table.Column<int>(type: "int", nullable: true),
                    Remarks = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedById = table.Column<int>(type: "int", nullable: false),
                    UpdatedById = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialAgreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialAgreements_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinancialAgreements_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinancialAgreements_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FinancialAuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    PaymentId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<int>(type: "int", nullable: false),
                    DetailsJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PerformedById = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialAuditLogs_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinancialAuditLogs_Users_PerformedById",
                        column: x => x.PerformedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "NotificationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DueCheckIntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    DueSoonDays = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationSettings", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PermissionGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionGroups", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmiInstallments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FinancialAgreementId = table.Column<int>(type: "int", nullable: false),
                    InstallmentNumber = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmiInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmiInstallments_FinancialAgreements_FinancialAgreementId",
                        column: x => x.FinancialAgreementId,
                        principalTable: "FinancialAgreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PermissionGroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Permissions_PermissionGroups_PermissionGroupId",
                        column: x => x.PermissionGroupId,
                        principalTable: "PermissionGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => new { x.UserId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_UserPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_IdempotencyKey",
                table: "Payments",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InstallmentId",
                table: "Payments",
                column: "InstallmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReversedById",
                table: "Payments",
                column: "ReversedById");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_BookedById",
                table: "Customers",
                column: "BookedById");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_FileId",
                table: "Customers",
                column: "FileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmiInstallments_FinancialAgreementId_InstallmentNumber",
                table: "EmiInstallments",
                columns: new[] { "FinancialAgreementId", "InstallmentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAgreements_CreatedById",
                table: "FinancialAgreements",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAgreements_CustomerId",
                table: "FinancialAgreements",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAgreements_UpdatedById",
                table: "FinancialAgreements",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAuditLogs_CustomerId",
                table: "FinancialAuditLogs",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAuditLogs_PerformedById",
                table: "FinancialAuditLogs",
                column: "PerformedById");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionGroups_Name",
                table: "PermissionGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_PermissionGroupId",
                table: "Permissions",
                column: "PermissionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_PermissionId",
                table: "UserPermissions",
                column: "PermissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Users_BookedById",
                table: "Customers",
                column: "BookedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_EmiInstallments_InstallmentId",
                table: "Payments",
                column: "InstallmentId",
                principalTable: "EmiInstallments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_ReversedById",
                table: "Payments",
                column: "ReversedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Users_BookedById",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_EmiInstallments_InstallmentId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_ReversedById",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "EmiInstallments");

            migrationBuilder.DropTable(
                name: "FinancialAuditLogs");

            migrationBuilder.DropTable(
                name: "NotificationSettings");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "FinancialAgreements");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "PermissionGroups");

            migrationBuilder.DropIndex(
                name: "IX_Payments_IdempotencyKey",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_InstallmentId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ReversedById",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Customers_BookedById",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_FileId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "InstallmentId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "IsReversed",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentDate",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ReversalReason",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ReversedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ReversedById",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "TransactionReference",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "BookedAt",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "BookedById",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "FileId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                table: "AppNotifications");

            migrationBuilder.DropColumn(
                name: "DueAmount",
                table: "AppNotifications");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "AppNotifications");

            migrationBuilder.DropColumn(
                name: "FileId",
                table: "AppNotifications");

            migrationBuilder.DropColumn(
                name: "OutstandingBalance",
                table: "AppNotifications");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Payments",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);
        }
    }
}
