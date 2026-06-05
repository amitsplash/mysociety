using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySociety.Domain.Entities;

namespace MySociety.Infrastructure.Persistence.Configurations;

public class MemberInviteConfiguration : IEntityTypeConfiguration<MemberInvite>
{
    public void Configure(EntityTypeBuilder<MemberInvite> builder)
    {
        builder.ToTable("MemberInvites");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CodeHash).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => x.MemberId);

        builder.HasOne(x => x.Member)
            .WithMany()
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedByMember)
            .WithMany()
            .HasForeignKey(x => x.CreatedByMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
