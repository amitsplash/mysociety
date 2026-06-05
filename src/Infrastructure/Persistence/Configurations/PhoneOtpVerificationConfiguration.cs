using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MySociety.Domain.Entities;

namespace MySociety.Infrastructure.Persistence.Configurations;

public class PhoneOtpVerificationConfiguration : IEntityTypeConfiguration<PhoneOtpVerification>
{
    public void Configure(EntityTypeBuilder<PhoneOtpVerification> builder)
    {
        builder.ToTable("PhoneOtpVerifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CodeHash).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.Phone, x.Purpose });
    }
}
