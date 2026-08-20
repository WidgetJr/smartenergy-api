namespace SmartEnergy.Api.Features.Spaces.Dtos;

public record SpaceResponse(
    Guid Id,
    Guid HomeId,
    string Name,
    DateTimeOffset CreatedAt);
