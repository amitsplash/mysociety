using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySociety.Domain.Entities;

namespace MySociety.Infrastructure.Persistence.Configurations;

public class OpenMatterConfiguration : IEntityTypeConfiguration<OpenMatter>
{
    public void Configure(EntityTypeBuilder<OpenMatter> builder)
    {
        builder.ToTable("OpenMatters");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);

        builder.HasIndex(x => new { x.GroupId, x.Status });

        builder.HasOne(x => x.Group)
            .WithMany(x => x.OpenMatters)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedByMember)
            .WithMany()
            .HasForeignKey(x => x.CreatedByMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LastDiscussedInMeeting)
            .WithMany()
            .HasForeignKey(x => x.LastDiscussedInMeetingId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
