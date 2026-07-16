using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class ReconstructVisitBookingsAndVehicles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionalInformation",
                table: "VehicleBookings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "VehicleBookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Driver",
                table: "VehicleBookings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "VehicleBookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "VehicleBookings",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "VehicleId",
                table: "VehicleBookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "VisitTime",
                table: "VehicleBookings",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RegistrationNumber = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VehicleType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Brand = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Model = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Color = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SeatingCapacity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleBookings_CustomerId",
                table: "VehicleBookings",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleBookings_ProjectId",
                table: "VehicleBookings",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleBookings_VehicleId",
                table: "VehicleBookings",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_RegistrationNumber",
                table: "Vehicles",
                column: "RegistrationNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleBookings_Customers_CustomerId",
                table: "VehicleBookings",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleBookings_Projects_ProjectId",
                table: "VehicleBookings",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleBookings_Vehicles_VehicleId",
                table: "VehicleBookings",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VehicleBookings_Customers_CustomerId",
                table: "VehicleBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleBookings_Projects_ProjectId",
                table: "VehicleBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleBookings_Vehicles_VehicleId",
                table: "VehicleBookings");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_VehicleBookings_CustomerId",
                table: "VehicleBookings");

            migrationBuilder.DropIndex(
                name: "IX_VehicleBookings_ProjectId",
                table: "VehicleBookings");

            migrationBuilder.DropIndex(
                name: "IX_VehicleBookings_VehicleId",
                table: "VehicleBookings");

            migrationBuilder.DropColumn(
                name: "AdditionalInformation",
                table: "VehicleBookings");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "VehicleBookings");

            migrationBuilder.DropColumn(
                name: "Driver",
                table: "VehicleBookings");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "VehicleBookings");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "VehicleBookings");

            migrationBuilder.DropColumn(
                name: "VehicleId",
                table: "VehicleBookings");

            migrationBuilder.DropColumn(
                name: "VisitTime",
                table: "VehicleBookings");
        }
    }
}
