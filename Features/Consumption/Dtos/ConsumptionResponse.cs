namespace SmartEnergy.Api.Features.Consumption.Dtos;

public record ConsumptionResponse(
    Guid HomeId,
    DateTimeOffset From,
    DateTimeOffset To,
    double EnergyKwh,
    double CurrentPowerWatts,
    decimal? EstimatedCost,
    string? Currency,
    bool CostComplete,
    IReadOnlyList<SpaceConsumptionResponse> Spaces);
