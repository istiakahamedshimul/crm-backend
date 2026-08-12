using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class BackfillBookedSalesAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A sales unit belongs to the executive assigned to the lead, not
            // necessarily the admin/user who performed the status update.
            migrationBuilder.Sql("""
                UPDATE `Customers` AS c
                INNER JOIN `Leads` AS l ON l.`Id` = c.`LeadId`
                SET c.`BookedById` = l.`AssignedToId`,
                    c.`BookedAt` = COALESCE(c.`BookedAt`, c.`CreatedAt`),
                    c.`AssignedToId` = COALESCE(c.`AssignedToId`, l.`AssignedToId`)
                WHERE l.`Status` = 9 AND l.`AssignedToId` IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
