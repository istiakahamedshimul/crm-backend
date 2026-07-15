using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class RemovePropertyUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_PropertyUnits_UnitId",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "PropertyUnits");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_UnitId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "Invoices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PropertyUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Bathrooms = table.Column<int>(type: "int", nullable: true),
                    Bedrooms = table.Column<int>(type: "int", nullable: true),
                    BookingMoney = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    FacingDirection = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FinalPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    FloorNumber = table.Column<int>(type: "int", nullable: true),
                    SizeSqft = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TowerOrBlock = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnitNumber = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyUnits_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_UnitId",
                table: "Invoices",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyUnits_ProjectId",
                table: "PropertyUnits",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_PropertyUnits_UnitId",
                table: "Invoices",
                column: "UnitId",
                principalTable: "PropertyUnits",
                principalColumn: "Id");
        }
    }
}
