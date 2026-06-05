using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySociety.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase1_OpenMattersAndAgenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                table: "Meetings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MeetingType",
                table: "Meetings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "Meetings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Meetings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MeetingAttendees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeetingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AttendanceStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingAttendees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingAttendees_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingAttendees_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpenMatters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RaisedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastDiscussedInMeetingId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedByMemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenMatters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenMatters_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OpenMatters_Meetings_LastDiscussedInMeetingId",
                        column: x => x.LastDiscussedInMeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OpenMatters_Members_CreatedByMemberId",
                        column: x => x.CreatedByMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgendaItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeetingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OpenMatterId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AgendaNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Outcome = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscussionSummary = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendaItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgendaItems_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgendaItems_OpenMatters_OpenMatterId",
                        column: x => x.OpenMatterId,
                        principalTable: "OpenMatters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgendaItems_MeetingId_DisplayOrder",
                table: "AgendaItems",
                columns: new[] { "MeetingId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_AgendaItems_OpenMatterId",
                table: "AgendaItems",
                column: "OpenMatterId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendees_MeetingId_MemberId",
                table: "MeetingAttendees",
                columns: new[] { "MeetingId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttendees_MemberId",
                table: "MeetingAttendees",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenMatters_CreatedByMemberId",
                table: "OpenMatters",
                column: "CreatedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenMatters_GroupId_Status",
                table: "OpenMatters",
                columns: new[] { "GroupId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenMatters_LastDiscussedInMeetingId",
                table: "OpenMatters",
                column: "LastDiscussedInMeetingId");

            // Existing meetings remain visible to all members.
            migrationBuilder.Sql("UPDATE Meetings SET Status = 3 WHERE Status = 0;");

            // Migrate legacy discussion points into open matters + agenda items.
            migrationBuilder.Sql("""
                INSERT INTO OpenMatters (Id, GroupId, Title, Description, Status, RaisedAt, LastDiscussedInMeetingId, CreatedByMemberId, CreatedAt)
                SELECT
                    p.Id,
                    m.GroupId,
                    substr(p.Description, 1, 200),
                    p.Description,
                    CASE p.Status WHEN 3 THEN 1 ELSE 0 END,
                    p.CreatedAt,
                    p.MeetingId,
                    m.CreatedByMemberId,
                    p.CreatedAt
                FROM MeetingDiscussionPoints p
                INNER JOIN Meetings m ON m.Id = p.MeetingId;

                INSERT INTO AgendaItems (Id, MeetingId, OpenMatterId, AgendaNumber, Title, Description, DisplayOrder, Source, Outcome, DiscussionSummary, CreatedAt)
                SELECT
                    p.Id,
                    p.MeetingId,
                    p.Id,
                    p.SortOrder + 1,
                    substr(p.Description, 1, 200),
                    p.Description,
                    p.SortOrder,
                    1,
                    CASE p.Status WHEN 0 THEN 0 WHEN 1 THEN 1 WHEN 2 THEN 4 WHEN 3 THEN 2 ELSE 0 END,
                    p.Notes,
                    p.CreatedAt
                FROM MeetingDiscussionPoints p;
                """);

            migrationBuilder.DropTable(
                name: "MeetingDiscussionPoints");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgendaItems");

            migrationBuilder.DropTable(
                name: "MeetingAttendees");

            migrationBuilder.DropTable(
                name: "OpenMatters");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "MeetingType",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Meetings");

            migrationBuilder.CreateTable(
                name: "MeetingDiscussionPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssignedToMemberId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MeetingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingDiscussionPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingDiscussionPoints_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingDiscussionPoints_Members_AssignedToMemberId",
                        column: x => x.AssignedToMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingDiscussionPoints_AssignedToMemberId",
                table: "MeetingDiscussionPoints",
                column: "AssignedToMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingDiscussionPoints_MeetingId",
                table: "MeetingDiscussionPoints",
                column: "MeetingId");
        }
    }
}
