using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySociety.Domain.Entities;

namespace MySociety.Infrastructure.Persistence.Configurations;

public class GroupIncomeConfiguration : IEntityTypeConfiguration<GroupIncome>
{
    public void Configure(EntityTypeBuilder<GroupIncome> builder)
    {
        builder.ToTable("GroupIncomes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.HasIndex(x => x.GroupId);

        builder.HasOne(x => x.Group)
            .WithMany(x => x.GroupIncomes)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedByMember)
            .WithMany(x => x.GroupIncomesCreated)
            .HasForeignKey(x => x.CreatedByMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
