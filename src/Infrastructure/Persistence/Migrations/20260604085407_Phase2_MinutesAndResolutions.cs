using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySociety.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_MinutesAndResolutions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Minutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgendaItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DiscussionSummary = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    DecisionTaken = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    BudgetApproved = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Minutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Minutes_AgendaItems_AgendaItemId",
                        column: x => x.AgendaItemId,
                        principalTable: "AgendaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Resolutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeetingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgendaItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OpenMatterId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ResolutionNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ResolutionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ApprovedBudget = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedByMemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resolutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resolutions_AgendaItems_AgendaItemId",
                        column: x => x.AgendaItemId,
                        principalTable: "AgendaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Resolutions_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Resolutions_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Resolutions_Members_CreatedByMemberId",
                        column: x => x.CreatedByMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Resolutions_OpenMatters_OpenMatterId",
                        column: x => x.OpenMatterId,
                        principalTable: "OpenMatters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Minutes_AgendaItemId",
                table: "Minutes",
                column: "AgendaItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resolutions_AgendaItemId",
                table: "Resolutions",
                column: "AgendaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Resolutions_CreatedByMemberId",
                table: "Resolutions",
                column: "CreatedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Resolutions_GroupId_ResolutionNumber",
                table: "Resolutions",
                columns: new[] { "GroupId", "ResolutionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resolutions_MeetingId",
                table: "Resolutions",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_Resolutions_OpenMatterId",
                table: "Resolutions",
                column: "OpenMatterId");

            migrationBuilder.Sql("""
                INSERT INTO Minutes (Id, AgendaItemId, DiscussionSummary, CreatedAt)
                SELECT Id, Id, DiscussionSummary, CreatedAt
                FROM AgendaItems
                WHERE DiscussionSummary IS NOT NULL AND trim(DiscussionSummary) <> '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Minutes");

            migrationBuilder.DropTable(
                name: "Resolutions");
        }
    }
}
