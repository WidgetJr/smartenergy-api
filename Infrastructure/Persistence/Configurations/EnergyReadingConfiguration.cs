using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEnergy.Api.Domain.Entities;

namespace SmartEnergy.Api.Infrastructure.Persistence.Configurations;

public class EnergyReadingConfiguration : IEntityTypeConfiguration<EnergyReading>
{
    public void Configure(EntityTypeBuilder<EnergyReading> builder)
    {
        builder.ToTable("EnergyReadings");
        builder.HasKey(reading => reading.Id);
        builder.Property(reading => reading.Id).UseIdentityByDefaultColumn();
        builder.Property(reading => reading.Voltage).IsRequired();
        builder.Property(reading => reading.Current).IsRequired();
        builder.Property(reading => reading.Power).IsRequired();
        builder.Property(reading => reading.EnergyTotalKwh).IsRequired();
        builder.Property(reading => reading.RecordedAt).IsRequired();
        builder.HasIndex(reading => new { reading.DeviceId, reading.RecordedAt });

        builder.HasOne(reading => reading.Device)
            .WithMany(device => device.EnergyReadings)
            .HasForeignKey(reading => reading.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
