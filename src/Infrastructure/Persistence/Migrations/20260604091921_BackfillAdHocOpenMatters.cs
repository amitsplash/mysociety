using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MySociety.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillAdHocOpenMatters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO OpenMatters (Id, GroupId, Title, Description, Status, RaisedAt, LastDiscussedInMeetingId, CreatedByMemberId, CreatedAt)
                SELECT
                    a.Id,
                    m.GroupId,
                    a.Title,
                    a.Description,
                    0,
                    a.CreatedAt,
                    a.MeetingId,
                    m.CreatedByMemberId,
                    a.CreatedAt
                FROM AgendaItems a
                INNER JOIN Meetings m ON m.Id = a.MeetingId
                WHERE a.OpenMatterId IS NULL AND a.Source = 1;

                UPDATE AgendaItems
                SET OpenMatterId = Id, Source = 0
                WHERE OpenMatterId IS NULL AND Source = 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
