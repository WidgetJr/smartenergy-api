namespace SmartEnergy.Api.Domain.Entities;

public class Home
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<HomeMember> HomeMembers { get; set; } = [];
    public ICollection<Space> Spaces { get; set; } = [];
    public ICollection<EnergyTariff> EnergyTariffs { get; set; } = [];
}
