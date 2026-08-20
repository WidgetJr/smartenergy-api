using Microsoft.EntityFrameworkCore;
using SmartEnergy.Api.Domain.Entities;

namespace SmartEnergy.Api.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Home> Homes => Set<Home>();
    public DbSet<HomeMember> HomeMembers => Set<HomeMember>();
    public DbSet<Space> Spaces => Set<Space>();
    public DbSet<EnergyTariff> EnergyTariffs => Set<EnergyTariff>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<EnergyReading> EnergyReadings => Set<EnergyReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
