namespace SmartEnergy.Api.Domain.Entities;

public class EnergyTariff
{
    public Guid Id { get; set; }
    public Guid HomeId { get; set; }
    public decimal PricePerKWh { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Home Home { get; set; } = null!;
}
