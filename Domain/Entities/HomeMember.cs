using SmartEnergy.Api.Domain.Enums;

namespace SmartEnergy.Api.Domain.Entities;

public class HomeMember
{
    public Guid HomeId { get; set; }
    public Guid UserId { get; set; }
    public HomeRole Role { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    public Home Home { get; set; } = null!;
    public User User { get; set; } = null!;
}
