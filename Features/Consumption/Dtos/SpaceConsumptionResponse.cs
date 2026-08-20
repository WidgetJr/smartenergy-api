namespace SmartEnergy.Api.Features.Consumption.Dtos;

public record SpaceConsumptionResponse(
    Guid SpaceId,
    string Name,
    DateTimeOffset From,
    DateTimeOffset To,
    double EnergyKwh,
    double CurrentPowerWatts,
    decimal? EstimatedCost,
    string? Currency,
    bool CostComplete,
    IReadOnlyList<DeviceConsumptionResponse> Devices);
