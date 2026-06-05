using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MySociety.Domain.Entities;



namespace MySociety.Infrastructure.Persistence.Configurations;



public class GroupConfiguration : IEntityTypeConfiguration<Group>

{

    public void Configure(EntityTypeBuilder<Group> builder)

    {

        builder.ToTable("Groups");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();

        builder.Property(x => x.ContributionAmount).HasPrecision(18, 2);

        builder.Property(x => x.OpeningMaintenanceBalance).HasPrecision(18, 2);
        builder.Property(x => x.OpeningCorpusBalance).HasPrecision(18, 2);

        builder.HasIndex(x => x.Name);

        builder.HasOne(x => x.CreatedByUser)

            .WithMany()

            .HasForeignKey(x => x.CreatedByUserId)

            .OnDelete(DeleteBehavior.Restrict);

    }

}

