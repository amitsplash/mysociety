using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySociety.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommitteeAndMeetings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommitteeMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitteeMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommitteeMembers_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommitteeMembers_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Meetings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MeetingDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedByMemberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meetings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Meetings_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Meetings_Members_CreatedByMemberId",
                        column: x => x.CreatedByMemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MeetingDiscussionPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeetingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedToMemberId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                name: "IX_CommitteeMembers_GroupId_MemberId",
                table: "CommitteeMembers",
                columns: new[] { "GroupId", "MemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeMembers_MemberId",
                table: "CommitteeMembers",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingDiscussionPoints_AssignedToMemberId",
                table: "MeetingDiscussionPoints",
                column: "AssignedToMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingDiscussionPoints_MeetingId",
                table: "MeetingDiscussionPoints",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_CreatedByMemberId",
                table: "Meetings",
                column: "CreatedByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_GroupId_MeetingDate",
                table: "Meetings",
                columns: new[] { "GroupId", "MeetingDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommitteeMembers");

            migrationBuilder.DropTable(
                name: "MeetingDiscussionPoints");

            migrationBuilder.DropTable(
                name: "Meetings");
        }
    }
}
