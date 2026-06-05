using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySociety.Domain.Entities;

namespace MySociety.Infrastructure.Persistence.Configurations;

public class MinuteConfiguration : IEntityTypeConfiguration<Minute>
{
    public void Configure(EntityTypeBuilder<Minute> builder)
    {
        builder.ToTable("Minutes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DiscussionSummary).HasMaxLength(4000);
        builder.Property(x => x.DecisionTaken).HasMaxLength(2000);
        builder.Property(x => x.BudgetApproved).HasPrecision(18, 2);

        builder.HasIndex(x => x.AgendaItemId).IsUnique();

        builder.HasOne(x => x.AgendaItem)
            .WithOne(x => x.Minute)
            .HasForeignKey<Minute>(x => x.AgendaItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
