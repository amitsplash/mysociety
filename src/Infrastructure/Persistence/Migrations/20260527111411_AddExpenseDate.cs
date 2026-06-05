using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySociety.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpenseDate",
                table: "SocietyExpenses",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpenseDate",
                table: "Expenses",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpenseDate",
                table: "SocietyExpenses");

            migrationBuilder.DropColumn(
                name: "ExpenseDate",
                table: "Expenses");
        }
    }
}
