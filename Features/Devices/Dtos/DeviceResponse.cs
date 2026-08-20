namespace SmartEnergy.Api.Features.Devices.Dtos;

public record DeviceResponse(
    Guid Id,
    Guid HomeId,
    Guid SpaceId,
    string SerialNumber,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedAt);
