using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEnergy.Api.Domain.Entities;

namespace SmartEnergy.Api.Infrastructure.Persistence.Configurations;

public class HomeConfiguration : IEntityTypeConfiguration<Home>
{
    public void Configure(EntityTypeBuilder<Home> builder)
    {
        builder.ToTable("Homes");
        builder.HasKey(home => home.Id);
        builder.Property(home => home.Name).HasMaxLength(150).IsRequired();
        builder.Property(home => home.CreatedAt).IsRequired();
    }
}
