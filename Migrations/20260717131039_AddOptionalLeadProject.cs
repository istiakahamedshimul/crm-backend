using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddOptionalLeadProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Leads",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leads_ProjectId",
                table: "Leads",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Leads_Projects_ProjectId",
                table: "Leads",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leads_Projects_ProjectId",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_ProjectId",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Leads");

        }
    }
}
