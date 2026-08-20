namespace SmartEnergy.Api.Domain.Entities;

public class Device
{
    public Guid Id { get; set; }
    public Guid SpaceId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Space Space { get; set; } = null!;
    public ICollection<EnergyReading> EnergyReadings { get; set; } = [];
}
