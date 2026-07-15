using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSubGroupsAndCustomerProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CompanyName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubGroups", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "SubGroups",
                columns: new[] { "Id", "Name", "CompanyName", "Description" },
                values: new object[] { 1, "General", "Real Capital Group", "Default subgroup for existing projects" });

            migrationBuilder.AddColumn<int>(
                name: "SubGroupId",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_SubGroupId",
                table: "Projects",
                column: "SubGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_ProjectId",
                table: "Customers",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SubGroups_Name",
                table: "SubGroups",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Projects_ProjectId",
                table: "Customers",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_SubGroups_SubGroupId",
                table: "Projects",
                column: "SubGroupId",
                principalTable: "SubGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Projects_ProjectId",
                table: "Customers");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_SubGroups_SubGroupId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "SubGroups");

            migrationBuilder.DropIndex(
                name: "IX_Projects_SubGroupId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Customers_ProjectId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "SubGroupId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Customers");
        }
    }
}
