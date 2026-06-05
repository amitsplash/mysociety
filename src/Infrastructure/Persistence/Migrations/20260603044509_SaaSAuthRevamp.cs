using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySociety.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SaaSAuthRevamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Users",
                type: "TEXT",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE Users
                SET Username = 'user_' || Phone
                WHERE Phone IS NOT NULL AND Phone != '';
                """);

            migrationBuilder.Sql("""
                UPDATE Users
                SET Username = 'user_' || lower(hex(Id))
                WHERE Username IS NULL OR Username = '';
                """);

            migrationBuilder.Sql("""
                UPDATE Users
                SET Email = Phone || '@migrated.local'
                WHERE Phone IS NOT NULL AND Phone != '';
                """);

            migrationBuilder.Sql("""
                UPDATE Users
                SET Email = lower(hex(Id)) || '@migrated.local'
                WHERE Email IS NULL OR Email = '';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "Users",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "PlatformRole",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Users",
                type: "TEXT",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Groups",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE Groups
                SET CreatedByUserId = (
                    SELECT m.UserId
                    FROM Members m
                    WHERE m.GroupId = Groups.Id AND m.Role = 1
                    ORDER BY m.CreatedAt
                    LIMIT 1
                );
                """);

            migrationBuilder.Sql("""
                UPDATE Groups
                SET CreatedByUserId = (
                    SELECT m.UserId
                    FROM Members m
                    WHERE m.GroupId = Groups.Id
                    ORDER BY m.CreatedAt
                    LIMIT 1
                )
                WHERE CreatedByUserId IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedByUserId",
                table: "Groups",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_CreatedByUserId",
                table: "Groups",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Users_CreatedByUserId",
                table: "Groups",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Users_CreatedByUserId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Groups_CreatedByUserId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Groups");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Users",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlatformRole",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
