using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySociety.Domain.Entities;

namespace MySociety.Infrastructure.Persistence.Configurations;

public class GroupExpenseConfiguration : IEntityTypeConfiguration<GroupExpense>
{
    public void Configure(EntityTypeBuilder<GroupExpense> builder)
    {
        builder.ToTable("GroupExpenses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.FundType).HasConversion<int>();

        builder.HasIndex(x => x.GroupId);

        builder.HasOne(x => x.Group)
            .WithMany(x => x.GroupExpenses)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedByMember)
            .WithMany(x => x.GroupExpensesCreated)
            .HasForeignKey(x => x.CreatedByMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
