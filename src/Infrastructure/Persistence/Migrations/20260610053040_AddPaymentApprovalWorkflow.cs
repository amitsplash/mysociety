using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySociety.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_GroupId",
                table: "Payments");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByMemberId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecordedByMemberId",
                table: "Payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmissionId",
                table: "Payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ApprovedByMemberId",
                table: "Payments",
                column: "ApprovedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_GroupId_Status",
                table: "Payments",
                columns: new[] { "GroupId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RecordedByMemberId",
                table: "Payments",
                column: "RecordedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SubmissionId",
                table: "Payments",
                column: "SubmissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Members_ApprovedByMemberId",
                table: "Payments",
                column: "ApprovedByMemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Members_RecordedByMemberId",
                table: "Payments",
                column: "RecordedByMemberId",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                UPDATE "Payments"
                SET "RecordedByMemberId" = "MemberId",
                    "SubmissionId" = "Id",
                    "Status" = 1,
                    "ApprovedAt" = "CreatedAt"
                WHERE "RecordedByMemberId" = '00000000-0000-0000-0000-000000000000';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Members_ApprovedByMemberId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Members_RecordedByMemberId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ApprovedByMemberId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_GroupId_Status",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_RecordedByMemberId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_SubmissionId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ApprovedByMemberId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RecordedByMemberId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SubmissionId",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_GroupId",
                table: "Payments",
                column: "GroupId");
        }
    }
}
