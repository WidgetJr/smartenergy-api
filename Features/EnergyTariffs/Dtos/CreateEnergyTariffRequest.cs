using System.ComponentModel.DataAnnotations;

namespace SmartEnergy.Api.Features.EnergyTariffs.Dtos;

public class CreateEnergyTariffRequest
{
    [Range(typeof(decimal), "0.0001", "99999999.9999")]
    public decimal PricePerKWh { get; set; }

    [Required]
    [MaxLength(16)]
    public string Currency { get; set; } = string.Empty;

    public DateTimeOffset EffectiveFrom { get; set; }
}
