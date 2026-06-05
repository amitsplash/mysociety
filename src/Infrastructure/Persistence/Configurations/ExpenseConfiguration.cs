using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySociety.Domain.Entities;

namespace MySociety.Infrastructure.Persistence.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.HasIndex(x => x.GroupId);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Group)
            .WithMany(x => x.Expenses)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedByMember)
            .WithMany(x => x.ExpensesCreated)
            .HasForeignKey(x => x.CreatedByMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ApprovedByMember)
            .WithMany(x => x.ExpensesApproved)
            .HasForeignKey(x => x.ApprovedByMemberId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
