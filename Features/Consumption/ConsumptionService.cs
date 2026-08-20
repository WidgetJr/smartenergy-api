using Microsoft.EntityFrameworkCore;
using SmartEnergy.Api.Features.Consumption.Dtos;
using SmartEnergy.Api.Infrastructure.Persistence;

namespace SmartEnergy.Api.Features.Consumption;

public class ConsumptionService(AppDbContext dbContext)
{
    public async Task<ConsumptionResult> GetHomeAsync(
        Guid userId,
        Guid homeId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        await CalculateAsync(userId, homeId, null, null, from, to, cancellationToken);

    public async Task<ConsumptionResult> GetSpaceAsync(
        Guid userId,
        Guid homeId,
        Guid spaceId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        await CalculateAsync(userId, homeId, spaceId, null, from, to, cancellationToken);

    public async Task<ConsumptionResult> GetDeviceAsync(
        Guid userId,
        Guid homeId,
        Guid spaceId,
        Guid deviceId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        await CalculateAsync(userId, homeId, spaceId, deviceId, from, to, cancellationToken);

    private async Task<ConsumptionResult> CalculateAsync(
        Guid userId,
        Guid homeId,
        Guid? spaceId,
        Guid? deviceId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        if (!RangeIsValid(from, to))
        {
            return new ConsumptionResult(ConsumptionStatus.InvalidInput);
        }

        var hasMembership = await dbContext.HomeMembers
            .AsNoTracking()
            .AnyAsync(
                member => member.UserId == userId && member.HomeId == homeId,
                cancellationToken);

        if (!hasMembership)
        {
            return new ConsumptionResult(ConsumptionStatus.NotFound);
        }

        var spaces = await dbContext.Spaces
            .AsNoTracking()
            .Where(space =>
                space.HomeId == homeId &&
                (spaceId == null || space.Id == spaceId))
            .OrderBy(space => space.Name)
            .Select(space => new SpaceData(space.Id, space.Name))
            .ToListAsync(cancellationToken);

        if (spaceId is not null && spaces.Count == 0)
        {
            return new ConsumptionResult(ConsumptionStatus.NotFound);
        }

        var spaceIds = spaces.Select(space => space.Id).ToList();
        var devices = await dbContext.Devices
            .AsNoTracking()
            .Where(device =>
                spaceIds.Contains(device.SpaceId) &&
                (deviceId == null || device.Id == deviceId))
            .OrderBy(device => device.Name)
            .Select(device => new DeviceData(
                device.Id,
                device.SpaceId,
                device.SerialNumber,
                device.Name,
                device.IsActive))
            .ToListAsync(cancellationToken);

        if (deviceId is not null && devices.Count == 0)
        {
            return new ConsumptionResult(ConsumptionStatus.NotFound);
        }

        var deviceIds = devices.Select(device => device.Id).ToList();
        var rangeReadings = deviceIds.Count == 0
            ? []
            : await dbContext.EnergyReadings
                .AsNoTracking()
                .Where(reading =>
                    deviceIds.Contains(reading.DeviceId) &&
                    reading.RecordedAt >= from &&
                    reading.RecordedAt <= to)
                .OrderBy(reading => reading.DeviceId)
                .ThenBy(reading => reading.RecordedAt)
                .ThenBy(reading => reading.Id)
                .Select(reading => new ReadingData(
                    reading.Id,
                    reading.DeviceId,
                    reading.EnergyTotalKwh,
                    reading.Power,
                    reading.RecordedAt))
                .ToListAsync(cancellationToken);

        var baselines = deviceIds.Count == 0
            ? []
            : await dbContext.EnergyReadings
                .AsNoTracking()
                .Where(reading =>
                    deviceIds.Contains(reading.DeviceId) &&
                    reading.RecordedAt < from &&
                    reading.Id == dbContext.EnergyReadings
                        .Where(candidate =>
                            candidate.DeviceId == reading.DeviceId &&
                            candidate.RecordedAt < from)
                        .OrderByDescending(candidate => candidate.RecordedAt)
                        .ThenByDescending(candidate => candidate.Id)
                        .Select(candidate => candidate.Id)
                        .First())
                .Select(reading => new ReadingData(
                    reading.Id,
                    reading.DeviceId,
                    reading.EnergyTotalKwh,
                    reading.Power,
                    reading.RecordedAt))
                .ToListAsync(cancellationToken);

        var tariffs = await dbContext.EnergyTariffs
            .AsNoTracking()
            .Where(tariff =>
                tariff.HomeId == homeId &&
                tariff.EffectiveFrom <= to &&
                (tariff.EffectiveTo == null || tariff.EffectiveTo > from))
            .OrderBy(tariff => tariff.EffectiveFrom)
            .Select(tariff => new TariffData(
                tariff.PricePerKWh,
                tariff.Currency,
                tariff.EffectiveFrom,
                tariff.EffectiveTo))
            .ToListAsync(cancellationToken);

        return BuildResult(
            homeId,
            spaceId,
            deviceId,
            from,
            to,
            spaces,
            devices,
            rangeReadings,
            baselines,
            tariffs);
    }

    private static ConsumptionResult BuildResult(
        Guid homeId,
        Guid? requestedSpaceId,
        Guid? requestedDeviceId,
        DateTimeOffset from,
        DateTimeOffset to,
        IReadOnlyList<SpaceData> spaces,
        IReadOnlyList<DeviceData> devices,
        IReadOnlyList<ReadingData> rangeReadings,
        IReadOnlyList<ReadingData> baselines,
        IReadOnlyList<TariffData> tariffs)
    {
        var readingsByDevice = rangeReadings
            .GroupBy(reading => reading.DeviceId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var baselineByDevice = baselines.ToDictionary(reading => reading.DeviceId);
        var deviceCalculations = new Dictionary<Guid, DeviceCalculation>();

        foreach (var device in devices)
        {
            readingsByDevice.TryGetValue(device.Id, out var readings);
            baselineByDevice.TryGetValue(device.Id, out var baseline);
            deviceCalculations[device.Id] = CalculateDevice(
                device,
                readings ?? [],
                baseline,
                tariffs,
                from,
                to);
        }

        var spaceCalculations = new List<SpaceCalculation>();
        foreach (var space in spaces)
        {
            var childDevices = devices
                .Where(device => device.SpaceId == space.Id)
                .Select(device => deviceCalculations[device.Id])
                .ToList();
            var cost = MergeCosts(childDevices.Select(device => device.Cost));
            var costView = BuildCostView(cost);

            var response = new SpaceConsumptionResponse(
                space.Id,
                space.Name,
                from,
                to,
                childDevices.Sum(device => device.Response.EnergyKwh),
                childDevices.Sum(device => device.Response.CurrentPowerWatts),
                costView.EstimatedCost,
                costView.Currency,
                costView.CostComplete,
                childDevices.Select(device => device.Response).ToList());

            spaceCalculations.Add(new SpaceCalculation(response, cost));
        }

        if (requestedDeviceId is not null)
        {
            return new ConsumptionResult(
                ConsumptionStatus.Success,
                Device: deviceCalculations[requestedDeviceId.Value].Response);
        }

        if (requestedSpaceId is not null)
        {
            return new ConsumptionResult(
                ConsumptionStatus.Success,
                Space: spaceCalculations.Single(space =>
                    space.Response.SpaceId == requestedSpaceId.Value).Response);
        }

        var homeCost = MergeCosts(spaceCalculations.Select(space => space.Cost));
        var homeCostView = BuildCostView(homeCost);
        var home = new ConsumptionResponse(
            homeId,
            from,
            to,
            spaceCalculations.Sum(space => space.Response.EnergyKwh),
            spaceCalculations.Sum(space => space.Response.CurrentPowerWatts),
            homeCostView.EstimatedCost,
            homeCostView.Currency,
            homeCostView.CostComplete,
            spaceCalculations.Select(space => space.Response).ToList());

        return new ConsumptionResult(ConsumptionStatus.Success, Home: home);
    }

    private static DeviceCalculation CalculateDevice(
        DeviceData device,
        IReadOnlyList<ReadingData> readings,
        ReadingData? baseline,
        IReadOnlyList<TariffData> tariffs,
        DateTimeOffset from,
        DateTimeOffset to)
    {
        var energyKwh = 0d;
        var cost = new CostState();
        var previous = baseline;

        foreach (var current in readings)
        {
            if (previous is null)
            {
                previous = current;
                continue;
            }

            var delta = current.EnergyTotalKwh >= previous.EnergyTotalKwh
                ? current.EnergyTotalKwh - previous.EnergyTotalKwh
                : current.EnergyTotalKwh;
            delta = Math.Max(0, delta);
            energyKwh += delta;

            if (delta > 0)
            {
                var tariff = tariffs.LastOrDefault(item =>
                    item.EffectiveFrom <= current.RecordedAt &&
                    (item.EffectiveTo == null || current.RecordedAt < item.EffectiveTo));

                if (tariff is null || !TryAddCost(cost, delta, tariff))
                {
                    cost.HasUntariffedDelta = true;
                }
            }

            previous = current;
        }

        var latestReading = readings.Count > 0 ? readings[^1] : baseline;
        var currentPowerWatts = device.IsActive && latestReading is not null
            ? latestReading.Power
            : 0;
        var costView = BuildCostView(cost);

        var response = new DeviceConsumptionResponse(
            device.Id,
            device.SerialNumber,
            device.Name,
            from,
            to,
            energyKwh,
            currentPowerWatts,
            costView.EstimatedCost,
            costView.Currency,
            costView.CostComplete);

        return new DeviceCalculation(response, cost);
    }

    private static bool TryAddCost(CostState cost, double delta, TariffData tariff)
    {
        if (!double.IsFinite(delta) || delta < 0 || delta > (double)decimal.MaxValue)
        {
            return false;
        }

        try
        {
            var deltaCost = (decimal)delta * tariff.PricePerKWh;
            cost.Amounts.TryGetValue(tariff.Currency, out var currentAmount);
            cost.Amounts[tariff.Currency] = currentAmount + deltaCost;
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static CostState MergeCosts(IEnumerable<CostState> costs)
    {
        var merged = new CostState();

        foreach (var cost in costs)
        {
            merged.HasUntariffedDelta |= cost.HasUntariffedDelta;

            foreach (var amount in cost.Amounts)
            {
                try
                {
                    merged.Amounts.TryGetValue(amount.Key, out var currentAmount);
                    merged.Amounts[amount.Key] = currentAmount + amount.Value;
                }
                catch (OverflowException)
                {
                    merged.HasUntariffedDelta = true;
                    merged.Amounts.Clear();
                    return merged;
                }
            }
        }

        return merged;
    }

    private static CostView BuildCostView(CostState cost)
    {
        if (cost.Amounts.Count != 1)
        {
            return new CostView(null, null, false);
        }

        var amount = cost.Amounts.Single();
        return new CostView(
            amount.Value,
            amount.Key,
            !cost.HasUntariffedDelta);
    }

    private static bool RangeIsValid(DateTimeOffset from, DateTimeOffset to) =>
        from.Offset == TimeSpan.Zero &&
        to.Offset == TimeSpan.Zero &&
        from < to;

    private sealed record SpaceData(Guid Id, string Name);

    private sealed record DeviceData(
        Guid Id,
        Guid SpaceId,
        string SerialNumber,
        string Name,
        bool IsActive);

    private sealed record ReadingData(
        long Id,
        Guid DeviceId,
        double EnergyTotalKwh,
        double Power,
        DateTimeOffset RecordedAt);

    private sealed record TariffData(
        decimal PricePerKWh,
        string Currency,
        DateTimeOffset EffectiveFrom,
        DateTimeOffset? EffectiveTo);

    private sealed record DeviceCalculation(
        DeviceConsumptionResponse Response,
        CostState Cost);

    private sealed record SpaceCalculation(
        SpaceConsumptionResponse Response,
        CostState Cost);

    private sealed class CostState
    {
        public Dictionary<string, decimal> Amounts { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public bool HasUntariffedDelta { get; set; }
    }

    private sealed record CostView(
        decimal? EstimatedCost,
        string? Currency,
        bool CostComplete);
}

public enum ConsumptionStatus
{
    Success,
    NotFound,
    InvalidInput
}

public record ConsumptionResult(
    ConsumptionStatus Status,
    ConsumptionResponse? Home = null,
    SpaceConsumptionResponse? Space = null,
    DeviceConsumptionResponse? Device = null);
