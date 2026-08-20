namespace SmartEnergy.Api.Features.EnergyReadings.Dtos;

public record EnergyReadingResponse(
    long Id,
    Guid HomeId,
    Guid SpaceId,
    Guid DeviceId,
    string SerialNumber,
    double Voltage,
    double Current,
    double Power,
    double EnergyTotalKwh,
    DateTimeOffset RecordedAt);

public record PagedEnergyReadingsResponse(
    IReadOnlyList<EnergyReadingResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
