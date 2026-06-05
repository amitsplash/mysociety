using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySociety.Domain.Entities;

namespace MySociety.Infrastructure.Persistence.Configurations;

public class ResolutionConfiguration : IEntityTypeConfiguration<Resolution>
{
    public void Configure(EntityTypeBuilder<Resolution> builder)
    {
        builder.ToTable("Resolutions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ResolutionNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.ApprovedBudget).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.GroupId, x.ResolutionNumber }).IsUnique();

        builder.HasOne(x => x.Group)
            .WithMany(x => x.Resolutions)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Meeting)
            .WithMany(x => x.Resolutions)
            .HasForeignKey(x => x.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.AgendaItem)
            .WithMany()
            .HasForeignKey(x => x.AgendaItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.OpenMatter)
            .WithMany()
            .HasForeignKey(x => x.OpenMatterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.CreatedByMember)
            .WithMany()
            .HasForeignKey(x => x.CreatedByMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
