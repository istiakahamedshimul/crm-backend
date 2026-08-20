using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadOriginsAndRepeatCustomers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreviousCustomerId",
                table: "Leads",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferrerEmail",
                table: "Leads",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ReferrerName",
                table: "Leads",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ReferrerPhone",
                table: "Leads",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_PreviousCustomerId",
                table: "Leads",
                column: "PreviousCustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_Customers_PreviousCustomerId",
                table: "Leads",
                column: "PreviousCustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leads_Customers_PreviousCustomerId",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_PreviousCustomerId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "PreviousCustomerId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ReferrerEmail",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ReferrerName",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ReferrerPhone",
                table: "Leads");
        }
    }
}
