using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySociety.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLedgerMemberTypeReferenceUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_Type_ReferenceId",
                table: "LedgerEntries");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_MemberId_Type_ReferenceId",
                table: "LedgerEntries",
                columns: new[] { "MemberId", "Type", "ReferenceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_MemberId_Type_ReferenceId",
                table: "LedgerEntries");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_Type_ReferenceId",
                table: "LedgerEntries",
                columns: new[] { "Type", "ReferenceId" });
        }
    }
}
