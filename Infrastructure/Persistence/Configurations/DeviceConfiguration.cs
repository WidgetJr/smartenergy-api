using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEnergy.Api.Domain.Entities;

namespace SmartEnergy.Api.Infrastructure.Persistence.Configurations;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Devices");
        builder.HasKey(device => device.Id);
        builder.Property(device => device.SerialNumber).HasMaxLength(100).IsRequired();
        builder.Property(device => device.Name).HasMaxLength(150).IsRequired();
        builder.Property(device => device.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(device => device.CreatedAt).IsRequired();
        builder.HasIndex(device => device.SerialNumber).IsUnique();
        builder.HasIndex(device => device.SpaceId);

        builder.HasOne(device => device.Space)
            .WithMany(space => space.Devices)
            .HasForeignKey(device => device.SpaceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
