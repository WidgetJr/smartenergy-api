using Microsoft.EntityFrameworkCore;
using SmartEnergy.Api.Domain.Entities;
using SmartEnergy.Api.Features.Devices;
using SmartEnergy.Api.Features.EnergyReadings.Dtos;
using SmartEnergy.Api.Infrastructure.Persistence;

namespace SmartEnergy.Api.Features.EnergyReadings;

public class EnergyReadingService(
    AppDbContext dbContext,
    DeviceService deviceService)
{
    public async Task<EnergyReadingResult> CreateAsync(
        Guid userId,
        Guid homeId,
        Guid spaceId,
        CreateEnergyReadingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!MeasurementsAreValid(request))
        {
            return new EnergyReadingResult(EnergyReadingStatus.InvalidInput);
        }

        var recordedAt = request.RecordedAt ?? DateTimeOffset.UtcNow;
        if (recordedAt.Offset != TimeSpan.Zero)
        {
            return new EnergyReadingResult(EnergyReadingStatus.InvalidInput);
        }

        var resolution = await deviceService.ResolveForReadingAsync(
            userId,
            homeId,
            spaceId,
            request.SerialNumber,
            cancellationToken);

        var resolutionError = MapDeviceStatus(resolution.Status);
        if (resolutionError is not null)
        {
            return new EnergyReadingResult(resolutionError.Value);
        }

        var reading = CreateReading(resolution.Device!, request, recordedAt);
        dbContext.EnergyReadings.Add(reading);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (resolution.WasCreated && DeviceService.IsUniqueViolation(exception))
        {
            dbContext.Entry(reading).State = EntityState.Detached;
            dbContext.Entry(resolution.Device!).State = EntityState.Detached;

            resolution = await deviceService.ResolveForReadingAsync(
                userId,
                homeId,
                spaceId,
                request.SerialNumber,
                cancellationToken);

            resolutionError = MapDeviceStatus(resolution.Status);
            if (resolutionError is not null)
            {
                return new EnergyReadingResult(resolutionError.Value);
            }

            reading = CreateReading(resolution.Device!, request, recordedAt);
            dbContext.EnergyReadings.Add(reading);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new EnergyReadingResult(
            EnergyReadingStatus.Success,
            MapResponse(reading, resolution.Device!, homeId, spaceId));
    }

    public async Task<EnergyReadingListResult> GetBySpaceAsync(
        Guid userId,
        Guid homeId,
        Guid spaceId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (!FiltersAreValid(from, to, page, pageSize))
        {
            return new EnergyReadingListResult(EnergyReadingStatus.InvalidInput);
        }

        if (!await HasMembershipAsync(userId, homeId, cancellationToken) ||
            !await SpaceBelongsToHomeAsync(spaceId, homeId, cancellationToken))
        {
            return new EnergyReadingListResult(EnergyReadingStatus.NotFound);
        }

        var query = dbContext.EnergyReadings
            .AsNoTracking()
            .Where(reading =>
                reading.Device.SpaceId == spaceId &&
                reading.Device.Space.HomeId == homeId);

        query = ApplyDateFilters(query, from, to);
        return await ToPagedResultAsync(query, homeId, spaceId, page, pageSize, cancellationToken);
    }

    public async Task<EnergyReadingListResult> GetByDeviceAsync(
        Guid userId,
        Guid homeId,
        Guid spaceId,
        Guid deviceId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (!FiltersAreValid(from, to, page, pageSize))
        {
            return new EnergyReadingListResult(EnergyReadingStatus.InvalidInput);
        }

        if (!await HasMembershipAsync(userId, homeId, cancellationToken) ||
            !await DeviceBelongsToResourceAsync(deviceId, spaceId, homeId, cancellationToken))
        {
            return new EnergyReadingListResult(EnergyReadingStatus.NotFound);
        }

        var query = dbContext.EnergyReadings
            .AsNoTracking()
            .Where(reading =>
                reading.DeviceId == deviceId &&
                reading.Device.SpaceId == spaceId &&
                reading.Device.Space.HomeId == homeId);

        query = ApplyDateFilters(query, from, to);
        return await ToPagedResultAsync(query, homeId, spaceId, page, pageSize, cancellationToken);
    }

    public async Task<EnergyReadingResult> GetLatestAsync(
        Guid userId,
        Guid homeId,
        Guid spaceId,
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        if (!await HasMembershipAsync(userId, homeId, cancellationToken) ||
            !await DeviceBelongsToResourceAsync(deviceId, spaceId, homeId, cancellationToken))
        {
            return new EnergyReadingResult(EnergyReadingStatus.NotFound);
        }

        var reading = await dbContext.EnergyReadings
            .AsNoTracking()
            .Where(item =>
                item.DeviceId == deviceId &&
                item.Device.SpaceId == spaceId &&
                item.Device.Space.HomeId == homeId)
            .OrderByDescending(item => item.RecordedAt)
            .ThenByDescending(item => item.Id)
            .Select(item => new EnergyReadingResponse(
                item.Id,
                item.Device.Space.HomeId,
                item.Device.SpaceId,
                item.DeviceId,
                item.Device.SerialNumber,
                item.Voltage,
                item.Current,
                item.Power,
                item.EnergyTotalKwh,
                item.RecordedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return reading is null
            ? new EnergyReadingResult(EnergyReadingStatus.NotFound)
            : new EnergyReadingResult(EnergyReadingStatus.Success, reading);
    }

    private async Task<EnergyReadingListResult> ToPagedResultAsync(
        IQueryable<EnergyReading> query,
        Guid homeId,
        Guid spaceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(reading => reading.RecordedAt)
            .ThenByDescending(reading => reading.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(reading => new EnergyReadingResponse(
                reading.Id,
                homeId,
                spaceId,
                reading.DeviceId,
                reading.Device.SerialNumber,
                reading.Voltage,
                reading.Current,
                reading.Power,
                reading.EnergyTotalKwh,
                reading.RecordedAt))
            .ToListAsync(cancellationToken);

        return new EnergyReadingListResult(
            EnergyReadingStatus.Success,
            new PagedEnergyReadingsResponse(items, page, pageSize, totalCount));
    }

    private static IQueryable<EnergyReading> ApplyDateFilters(
        IQueryable<EnergyReading> query,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        if (from is not null)
        {
            query = query.Where(reading => reading.RecordedAt >= from.Value);
        }

        if (to is not null)
        {
            query = query.Where(reading => reading.RecordedAt <= to.Value);
        }

        return query;
    }

    private async Task<bool> HasMembershipAsync(
        Guid userId,
        Guid homeId,
        CancellationToken cancellationToken) =>
        await dbContext.HomeMembers
            .AsNoTracking()
            .AnyAsync(
                member => member.UserId == userId && member.HomeId == homeId,
                cancellationToken);

    private async Task<bool> SpaceBelongsToHomeAsync(
        Guid spaceId,
        Guid homeId,
        CancellationToken cancellationToken) =>
        await dbContext.Spaces
            .AsNoTracking()
            .AnyAsync(
                space => space.Id == spaceId && space.HomeId == homeId,
                cancellationToken);

    private async Task<bool> DeviceBelongsToResourceAsync(
        Guid deviceId,
        Guid spaceId,
        Guid homeId,
        CancellationToken cancellationToken) =>
        await dbContext.Devices
            .AsNoTracking()
            .AnyAsync(
                device =>
                    device.Id == deviceId &&
                    device.SpaceId == spaceId &&
                    device.Space.HomeId == homeId,
                cancellationToken);

    private static bool MeasurementsAreValid(CreateEnergyReadingRequest request) =>
        !string.IsNullOrWhiteSpace(request.SerialNumber) &&
        request.SerialNumber.Trim().Length <= 100 &&
        IsNonNegativeFinite(request.Voltage) &&
        IsNonNegativeFinite(request.Current) &&
        IsNonNegativeFinite(request.Power) &&
        IsNonNegativeFinite(request.EnergyTotalKwh);

    private static bool IsNonNegativeFinite(double value) =>
        double.IsFinite(value) && value >= 0;

    private static bool FiltersAreValid(
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize) =>
        (from is null || from.Value.Offset == TimeSpan.Zero) &&
        (to is null || to.Value.Offset == TimeSpan.Zero) &&
        (from is null || to is null || from <= to) &&
        page >= 1 &&
        pageSize is >= 1 and <= 500 &&
        ((long)page - 1) * pageSize <= int.MaxValue;

    private static EnergyReadingStatus? MapDeviceStatus(DeviceOperationStatus status) =>
        status switch
        {
            DeviceOperationStatus.Success => null,
            DeviceOperationStatus.NotFound => EnergyReadingStatus.NotFound,
            DeviceOperationStatus.InvalidInput => EnergyReadingStatus.InvalidInput,
            DeviceOperationStatus.Inactive => EnergyReadingStatus.DeviceInactive,
            _ => EnergyReadingStatus.DeviceConflict
        };

    private static EnergyReading CreateReading(
        Device device,
        CreateEnergyReadingRequest request,
        DateTimeOffset recordedAt) =>
        new()
        {
            DeviceId = device.Id,
            Device = device,
            Voltage = request.Voltage,
            Current = request.Current,
            Power = request.Power,
            EnergyTotalKwh = request.EnergyTotalKwh,
            RecordedAt = recordedAt
        };

    private static EnergyReadingResponse MapResponse(
        EnergyReading reading,
        Device device,
        Guid homeId,
        Guid spaceId) =>
        new(
            reading.Id,
            homeId,
            spaceId,
            reading.DeviceId,
            device.SerialNumber,
            reading.Voltage,
            reading.Current,
            reading.Power,
            reading.EnergyTotalKwh,
            reading.RecordedAt);
}

public enum EnergyReadingStatus
{
    Success,
    NotFound,
    InvalidInput,
    DeviceConflict,
    DeviceInactive
}

public record EnergyReadingResult(
    EnergyReadingStatus Status,
    EnergyReadingResponse? Reading = null);

public record EnergyReadingListResult(
    EnergyReadingStatus Status,
    PagedEnergyReadingsResponse? Readings = null);
