namespace SmartEnergy.Api.Features.Homes.Dtos;

public record HomeResponse(
    Guid Id,
    string Name,
    string Role,
    DateTimeOffset CreatedAt);
