using System.ComponentModel.DataAnnotations;

namespace SmartEnergy.Api.Features.EnergyReadings.Dtos;

public class CreateEnergyReadingRequest
{
    [Required]
    [MaxLength(100)]
    public string SerialNumber { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public double Voltage { get; set; }

    [Range(0, double.MaxValue)]
    public double Current { get; set; }

    [Range(0, double.MaxValue)]
    public double Power { get; set; }

    [Range(0, double.MaxValue)]
    public double EnergyTotalKwh { get; set; }

    public DateTimeOffset? RecordedAt { get; set; }
}
