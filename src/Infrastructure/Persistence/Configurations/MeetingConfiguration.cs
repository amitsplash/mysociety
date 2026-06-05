using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySociety.Domain.Entities;

namespace MySociety.Infrastructure.Persistence.Configurations;

public class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.ToTable("Meetings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Location).HasMaxLength(200);
        builder.Property(x => x.Summary).HasMaxLength(2000);
        builder.Property(x => x.StartTime);
        builder.Property(x => x.EndTime);

        builder.HasIndex(x => new { x.GroupId, x.MeetingDate });

        builder.HasOne(x => x.Group)
            .WithMany(x => x.Meetings)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedByMember)
            .WithMany()
            .HasForeignKey(x => x.CreatedByMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
