using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySociety.Domain.Entities;

namespace MySociety.Infrastructure.Persistence.Configurations;

public class AgendaItemConfiguration : IEntityTypeConfiguration<AgendaItem>
{
    public void Configure(EntityTypeBuilder<AgendaItem> builder)
    {
        builder.ToTable("AgendaItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.DiscussionSummary).HasMaxLength(4000);

        builder.HasIndex(x => new { x.MeetingId, x.DisplayOrder });

        builder.HasOne(x => x.Meeting)
            .WithMany(x => x.AgendaItems)
            .HasForeignKey(x => x.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.OpenMatter)
            .WithMany(x => x.AgendaItems)
            .HasForeignKey(x => x.OpenMatterId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
