using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySociety.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformRoleAndPasswordResetIssuer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlatformRole",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedByMemberId",
                table: "PasswordResetTokens",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "PasswordResetTokens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_CreatedByUserId",
                table: "PasswordResetTokens",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PasswordResetTokens_Users_CreatedByUserId",
                table: "PasswordResetTokens",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("UPDATE Members SET Role = 1 WHERE Role = 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PasswordResetTokens_Users_CreatedByUserId",
                table: "PasswordResetTokens");

            migrationBuilder.DropIndex(
                name: "IX_PasswordResetTokens_CreatedByUserId",
                table: "PasswordResetTokens");

            migrationBuilder.DropColumn(
                name: "PlatformRole",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "PasswordResetTokens");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedByMemberId",
                table: "PasswordResetTokens",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
