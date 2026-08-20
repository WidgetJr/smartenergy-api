namespace SmartEnergy.Api.Domain.Entities;

public class EnergyReading
{
    public long Id { get; set; }
    public Guid DeviceId { get; set; }
    public double Voltage { get; set; }
    public double Current { get; set; }
    public double Power { get; set; }
    public double EnergyTotalKwh { get; set; }
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
    public Device Device { get; set; } = null!;
}
