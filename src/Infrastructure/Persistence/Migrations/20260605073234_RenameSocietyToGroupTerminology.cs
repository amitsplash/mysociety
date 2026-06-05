using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySociety.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameSocietyToGroupTerminology : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "SocietyExpenses",
                newName: "GroupExpenses");

            migrationBuilder.RenameColumn(
                name: "OpeningSocietyBalance",
                table: "Groups",
                newName: "OpeningMaintenanceBalance");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OpeningMaintenanceBalance",
                table: "Groups",
                newName: "OpeningSocietyBalance");

            migrationBuilder.RenameTable(
                name: "GroupExpenses",
                newName: "SocietyExpenses");
        }
    }
}
