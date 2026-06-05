using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySociety.Domain.Entities;

namespace MySociety.Infrastructure.Persistence.Configurations;

public class ContributionConfiguration : IEntityTypeConfiguration<Contribution>
{
    public void Configure(EntityTypeBuilder<Contribution> builder)
    {
        builder.ToTable("Contributions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Period).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.MemberId, x.Period }).IsUnique();

        builder.HasOne(x => x.Member)
            .WithMany(x => x.Contributions)
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Group)
            .WithMany(x => x.Contributions)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
