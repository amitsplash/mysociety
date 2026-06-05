using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySociety.Domain.Entities;

namespace MySociety.Infrastructure.Persistence.Configurations;

public class MeetingAttendeeConfiguration : IEntityTypeConfiguration<MeetingAttendee>
{
    public void Configure(EntityTypeBuilder<MeetingAttendee> builder)
    {
        builder.ToTable("MeetingAttendees");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.MeetingId, x.MemberId }).IsUnique();

        builder.HasOne(x => x.Meeting)
            .WithMany(x => x.Attendees)
            .HasForeignKey(x => x.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Member)
            .WithMany()
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
