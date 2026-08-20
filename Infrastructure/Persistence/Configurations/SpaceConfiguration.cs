using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEnergy.Api.Domain.Entities;

namespace SmartEnergy.Api.Infrastructure.Persistence.Configurations;

public class SpaceConfiguration : IEntityTypeConfiguration<Space>
{
    public void Configure(EntityTypeBuilder<Space> builder)
    {
        builder.ToTable("Spaces");
        builder.HasKey(space => space.Id);
        builder.Property(space => space.Name).HasMaxLength(150).IsRequired();
        builder.Property(space => space.CreatedAt).IsRequired();
        builder.HasIndex(space => space.HomeId);

        builder.HasOne(space => space.Home)
            .WithMany(home => home.Spaces)
            .HasForeignKey(space => space.HomeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
