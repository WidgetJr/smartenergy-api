namespace SmartEnergy.Api.Features.Consumption.Dtos;

public record DeviceConsumptionResponse(
    Guid DeviceId,
    string SerialNumber,
    string Name,
    DateTimeOffset From,
    DateTimeOffset To,
    double EnergyKwh,
    double CurrentPowerWatts,
    decimal? EstimatedCost,
    string? Currency,
    bool CostComplete);
