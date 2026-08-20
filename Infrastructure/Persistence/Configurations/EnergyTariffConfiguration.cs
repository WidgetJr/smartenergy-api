using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEnergy.Api.Domain.Entities;

namespace SmartEnergy.Api.Infrastructure.Persistence.Configurations;

public class EnergyTariffConfiguration : IEntityTypeConfiguration<EnergyTariff>
{
    public void Configure(EntityTypeBuilder<EnergyTariff> builder)
    {
        builder.ToTable("EnergyTariffs");
        builder.HasKey(tariff => tariff.Id);
        builder.Property(tariff => tariff.PricePerKWh).HasPrecision(12, 4).IsRequired();
        builder.Property(tariff => tariff.Currency).HasMaxLength(3).IsRequired();
        builder.Property(tariff => tariff.EffectiveFrom).IsRequired();
        builder.Property(tariff => tariff.EffectiveTo);
        builder.Property(tariff => tariff.CreatedAt).IsRequired();
        builder.HasIndex(tariff => new { tariff.HomeId, tariff.EffectiveFrom });

        builder.HasOne(tariff => tariff.Home)
            .WithMany(home => home.EnergyTariffs)
            .HasForeignKey(tariff => tariff.HomeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
