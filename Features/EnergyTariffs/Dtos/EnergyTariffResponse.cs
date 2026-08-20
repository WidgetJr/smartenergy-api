namespace SmartEnergy.Api.Features.EnergyTariffs.Dtos;

public record EnergyTariffResponse(
    Guid Id,
    Guid HomeId,
    decimal PricePerKWh,
    string Currency,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    DateTimeOffset CreatedAt,
    bool IsCurrent);
