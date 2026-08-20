using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartEnergy.Api.Domain.Entities;
using SmartEnergy.Api.Domain.Enums;
using SmartEnergy.Api.Features.Devices.Dtos;
using SmartEnergy.Api.Infrastructure.Persistence;

namespace SmartEnergy.Api.Features.Devices;

public class DeviceService(AppDbContext dbContext)
{
    public Task<DeviceResult> RegisterAsync(
        Guid userId,
        Guid homeId,
        Guid spaceId,
        RegisterDeviceRequest request,
        CancellationToken cancellationToken = default) =>
        GetOrCreateBySerialAsync(
            userId,
            homeId,
            spaceId,
            request.SerialNumber,
            request.Name,
            cancellationToken);

    public async Task<DeviceResolutionResult> ResolveForReadingAsync(
        Guid userId,
        Guid homeId,
        Guid spaceId,
        string serialNumber,
        CancellationToken cancellationToken = default)
    {
        if (await GetMembershipRoleAsync(userId, homeId, cancellationToken) is null)
        {
            return new DeviceResolutionResult(DeviceOperationStatus.NotFound);
        }

        if (!await SpaceBelongsToHomeAsync(spaceId, homeId, cancellationToken))
        {
            return new DeviceResolutionResult(DeviceOperationStatus.NotFound);
        }

        var normalizedSerialNumber = serialNumber.Trim().ToUpperInvariant();
        if (normalizedSerialNumber.Length is 0 or > 100)
        {
            return new DeviceResolutionResult(DeviceOperationStatus.InvalidInput);
        }

        var existingDevice = await dbContext.Devices
            .Include(device => device.Space)
            .SingleOrDefaultAsync(
                device => device.SerialNumber == normalizedSerialNumber,
                cancellationToken);

        if (existingDevice is not null)
        {
            if (existingDevice.SpaceId != spaceId || existingDevice.Space.HomeId != homeId)
            {
                return new DeviceResolutionResult(DeviceOperationStatus.Conflict);
            }

            return existingDevice.IsActive
                ? new DeviceResolutionResult(DeviceOperationStatus.Success, existingDevice)
                : new DeviceResolutionResult(DeviceOperationStatus.Inactive);
        }

        var device = new Device
        {
            Id = Guid.NewGuid(),
            SpaceId = spaceId,
            SerialNumber = normalizedSerialNumber,
            Name = normalizedSerialNumber,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Devices.Add(device);
        return new DeviceResolutionResult(
            DeviceOperationStatus.Success,
            device,
            WasCreated: true);
    }

    public async Task<DeviceResult> GetOrCreateBySerialAsync(
        Guid userId,
        Guid homeId,
        Guid spaceId,
        string serialNumber,
        string? name,
        CancellationToken cancellationToken = default)
    {
        var role = await GetMembershipRoleAsync(userId, homeId, cancellationToken);
        if (role is null)
        {
            return new DeviceResult(DeviceOperationStatus.NotFound);
        }

        if (!CanManageDevices(role.Value))
        {
            return new DeviceResult(DeviceOperationStatus.Forbidden);
        }

        if (!await SpaceBelongsToHomeAsync(spaceId, homeId, cancellationToken))
        {
            return new DeviceResult(DeviceOperationStatus.NotFound);
        }

        var normalizedSerialNumber = serialNumber.Trim().ToUpperInvariant();
        if (normalizedSerialNumber.Length is 0 or > 100)
        {
            return new DeviceResult(DeviceOperationStatus.InvalidInput);
        }

        var existingDevice = await FindBySerialAsync(normalizedSerialNumber, cancellationToken);
        if (existingDevice is not null)
        {
            return ExistingDeviceResult(existingDevice, homeId, spaceId);
        }

        var normalizedName = string.IsNullOrWhiteSpace(name)
            ? normalizedSerialNumber
            : name.Trim();

        if (normalizedName.Length > 150)
        {
            return new DeviceResult(DeviceOperationStatus.InvalidInput);
        }

        var device = new Device
        {
            Id = Guid.NewGuid(),
            SpaceId = spaceId,
            SerialNumber = normalizedSerialNumber,
            Name = normalizedName,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Devices.Add(device);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            dbContext.Entry(device).State = EntityState.Detached;
            existingDevice = await FindBySerialAsync(normalizedSerialNumber, cancellationToken);

            return existingDevice is null
                ? new DeviceResult(DeviceOperationStatus.Conflict)
                : ExistingDeviceResult(existingDevice, homeId, spaceId);
        }

        return new DeviceResult(
            DeviceOperationStatus.Success,
            MapResponse(device, homeId),
            IsCreated: true);
    }

    public async Task<DeviceListResult> GetBySpaceAsync(
        Guid userId,
        Guid homeId,
        Guid spaceId,
        CancellationToken cancellationToken = default)
    {
        if (!await HasMembershipAsync(userId, homeId, cancellationToken) ||
            !await SpaceBelongsToHomeAsync(spaceId, homeId, cancellationToken))
        {
            return new DeviceListResult(DeviceOperationStatus.NotFound);
        }

        var devices = await dbContext.Devices
            .AsNoTracking()
            .Where(device => device.SpaceId == spaceId)
            .OrderBy(device => device.Name)
            .ThenBy(device => device.CreatedAt)
            .Select(device => new DeviceResponse(
                device.Id,
                homeId,
                device.SpaceId,
                device.SerialNumber,
                device.Name,
                device.IsActive,
                device.CreatedAt))
            .ToListAsync(cancellationToken);

        return new DeviceListResult(DeviceOperationStatus.Success, devices);
    }

    public async Task<DeviceResult> GetByIdAsync(
        Guid userId,
        Guid homeId,
        Guid spaceId,
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        if (!await HasMembershipAsync(userId, homeId, cancellationToken))
        {
            return new DeviceResult(DeviceOperationStatus.NotFound);
        }

        var device = await dbContext.Devices
            .AsNoTracking()
            .Where(item =>
                item.Id == deviceId &&
                item.SpaceId == spaceId &&
                item.Space.HomeId == homeId)
            .Select(item => new DeviceResponse(
                item.Id,
                item.Space.HomeId,
                item.SpaceId,
                item.SerialNumber,
                item.Name,
                item.IsActive,
                item.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return device is null
            ? new DeviceResult(DeviceOperationStatus.NotFound)
            : new DeviceResult(DeviceOperationStatus.Success, device);
    }

    public async Task<DeviceResult> UpdateAsync(
        Guid userId,
        Guid homeId,
        Guid spaceId,
        Guid deviceId,
        UpdateDeviceRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = await GetMembershipRoleAsync(userId, homeId, cancellationToken);
        if (role is null)
        {
            return new DeviceResult(DeviceOperationStatus.NotFound);
        }

        var device = await dbContext.Devices
            .Include(item => item.Space)
            .SingleOrDefaultAsync(
                item =>
                    item.Id == deviceId &&
                    item.SpaceId == spaceId &&
                    item.Space.HomeId == homeId,
                cancellationToken);

        if (device is null)
        {
            return new DeviceResult(DeviceOperationStatus.NotFound);
        }

        if (!CanManageDevices(role.Value))
        {
            return new DeviceResult(DeviceOperationStatus.Forbidden);
        }

        var normalizedName = request.Name.Trim();
        if (normalizedName.Length is 0 or > 150)
        {
            return new DeviceResult(DeviceOperationStatus.InvalidInput);
        }

        device.Name = normalizedName;
        device.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DeviceResult(DeviceOperationStatus.Success, MapResponse(device, homeId));
    }

    private async Task<ExistingDevice?> FindBySerialAsync(
        string serialNumber,
        CancellationToken cancellationToken) =>
        await dbContext.Devices
            .AsNoTracking()
            .Where(device => device.SerialNumber == serialNumber)
            .Select(device => new ExistingDevice(
                device.Id,
                device.Space.HomeId,
                device.SpaceId,
                device.SerialNumber,
                device.Name,
                device.IsActive,
                device.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

    private static DeviceResult ExistingDeviceResult(
        ExistingDevice device,
        Guid homeId,
        Guid spaceId) =>
        device.HomeId == homeId && device.SpaceId == spaceId
            ? new DeviceResult(DeviceOperationStatus.Success, device.ToResponse())
            : new DeviceResult(DeviceOperationStatus.Conflict);

    private async Task<HomeRole?> GetMembershipRoleAsync(
        Guid userId,
        Guid homeId,
        CancellationToken cancellationToken) =>
        await dbContext.HomeMembers
            .AsNoTracking()
            .Where(member => member.UserId == userId && member.HomeId == homeId)
            .Select(member => (HomeRole?)member.Role)
            .SingleOrDefaultAsync(cancellationToken);

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

    private static bool CanManageDevices(HomeRole role) =>
        role is HomeRole.Owner or HomeRole.Admin;

    internal static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };

    private static DeviceResponse MapResponse(Device device, Guid homeId) =>
        new(
            device.Id,
            homeId,
            device.SpaceId,
            device.SerialNumber,
            device.Name,
            device.IsActive,
            device.CreatedAt);

    private sealed record ExistingDevice(
        Guid Id,
        Guid HomeId,
        Guid SpaceId,
        string SerialNumber,
        string Name,
        bool IsActive,
        DateTimeOffset CreatedAt)
    {
        public DeviceResponse ToResponse() =>
            new(Id, HomeId, SpaceId, SerialNumber, Name, IsActive, CreatedAt);
    }
}

public enum DeviceOperationStatus
{
    Success,
    NotFound,
    Forbidden,
    Conflict,
    Inactive,
    InvalidInput
}

public record DeviceResult(
    DeviceOperationStatus Status,
    DeviceResponse? Device = null,
    bool IsCreated = false);

public record DeviceListResult(
    DeviceOperationStatus Status,
    IReadOnlyList<DeviceResponse>? Devices = null);

public record DeviceResolutionResult(
    DeviceOperationStatus Status,
    Device? Device = null,
    bool WasCreated = false);
