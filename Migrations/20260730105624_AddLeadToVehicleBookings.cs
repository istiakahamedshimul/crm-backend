using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadToVehicleBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LeadId",
                table: "VehicleBookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleBookings_LeadId",
                table: "VehicleBookings",
                column: "LeadId");

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleBookings_Leads_LeadId",
                table: "VehicleBookings",
                column: "LeadId",
                principalTable: "Leads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VehicleBookings_Leads_LeadId",
                table: "VehicleBookings");

            migrationBuilder.DropIndex(
                name: "IX_VehicleBookings_LeadId",
                table: "VehicleBookings");

            migrationBuilder.DropColumn(
                name: "LeadId",
                table: "VehicleBookings");
        }
    }
}
