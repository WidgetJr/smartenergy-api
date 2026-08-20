namespace SmartEnergy.Api.Domain.Entities;

public class Space
{
    public Guid Id { get; set; }
    public Guid HomeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Home Home { get; set; } = null!;
    public ICollection<Device> Devices { get; set; } = [];
}
