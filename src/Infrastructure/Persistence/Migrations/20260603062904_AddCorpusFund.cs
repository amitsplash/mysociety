using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySociety.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCorpusFund : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FundType",
                table: "SocietyExpenses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CorpusAmount",
                table: "Members",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CorpusPaidAt",
                table: "Members",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OpeningCorpusBalance",
                table: "Groups",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FundType",
                table: "SocietyExpenses");

            migrationBuilder.DropColumn(
                name: "CorpusAmount",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "CorpusPaidAt",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "OpeningCorpusBalance",
                table: "Groups");
        }
    }
}
