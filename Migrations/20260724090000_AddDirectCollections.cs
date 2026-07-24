using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using backend.Data;

#nullable disable

namespace backend.Migrations;

[DbContext(typeof(CrmDbContext))]
[Migration("20260724090000_AddDirectCollections")]
public partial class AddDirectCollections : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "InvoiceId", table: "Payments", type: "int", nullable: true,
            oldClrType: typeof(int), oldType: "int");
        migrationBuilder.AlterColumn<int>(
            name: "InvoiceId", table: "Commissions", type: "int", nullable: true,
            oldClrType: typeof(int), oldType: "int");
        migrationBuilder.AddColumn<string>(
            name: "CollectionNumber", table: "Payments", type: "varchar(64)",
            maxLength: 64, nullable: false, defaultValue: "");
        migrationBuilder.Sql("UPDATE Payments SET CollectionNumber = CONCAT('COL-LEGACY-', Id)");
        migrationBuilder.CreateIndex(
            name: "IX_Payments_CollectionNumber", table: "Payments",
            column: "CollectionNumber", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Payments_CollectionNumber", table: "Payments");
        migrationBuilder.DropColumn(name: "CollectionNumber", table: "Payments");
        migrationBuilder.AlterColumn<int>(
            name: "InvoiceId", table: "Payments", type: "int", nullable: false,
            defaultValue: 0, oldClrType: typeof(int), oldType: "int", oldNullable: true);
        migrationBuilder.AlterColumn<int>(
            name: "InvoiceId", table: "Commissions", type: "int", nullable: false,
            defaultValue: 0, oldClrType: typeof(int), oldType: "int", oldNullable: true);
    }
}
