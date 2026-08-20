using Microsoft.EntityFrameworkCore;
using SmartEnergy.Api.Domain.Entities;
using SmartEnergy.Api.Domain.Enums;
using SmartEnergy.Api.Features.EnergyTariffs.Dtos;
using SmartEnergy.Api.Infrastructure.Persistence;

namespace SmartEnergy.Api.Features.EnergyTariffs;

public class EnergyTariffService(AppDbContext dbContext)
{
    public async Task<EnergyTariffResult> CreateAsync(
        Guid userId,
        Guid homeId,
        CreateEnergyTariffRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = await GetMembershipRoleAsync(userId, homeId, cancellationToken);
        if (role is null)
        {
            return new EnergyTariffResult(EnergyTariffStatus.NotFound);
        }

        if (role.Value is not HomeRole.Owner and not HomeRole.Admin)
        {
            return new EnergyTariffResult(EnergyTariffStatus.Forbidden);
        }

        var currency = request.Currency.Trim().ToUpperInvariant();
        if (currency.Length != 3 || !currency.All(char.IsAsciiLetter))
        {
            return new EnergyTariffResult(EnergyTariffStatus.InvalidInput);
        }

        if (request.EffectiveFrom == default || request.EffectiveFrom.Offset != TimeSpan.Zero)
        {
            return new EnergyTariffResult(EnergyTariffStatus.InvalidInput);
        }

        var openTariff = await dbContext.EnergyTariffs
            .SingleOrDefaultAsync(
                tariff => tariff.HomeId == homeId && tariff.EffectiveTo == null,
                cancellationToken);

        if (openTariff is not null &&
            request.EffectiveFrom <= openTariff.EffectiveFrom)
        {
            return new EnergyTariffResult(EnergyTariffStatus.Conflict);
        }

        if (openTariff is not null)
        {
            openTariff.EffectiveTo = request.EffectiveFrom;
        }

        var tariff = new EnergyTariff
        {
            Id = Guid.NewGuid(),
            HomeId = homeId,
            PricePerKWh = request.PricePerKWh,
            Currency = currency,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = null,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.EnergyTariffs.Add(tariff);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new EnergyTariffResult(
            EnergyTariffStatus.Success,
            MapResponse(tariff));
    }

    public async Task<EnergyTariffListResult> GetHistoryAsync(
        Guid userId,
        Guid homeId,
        CancellationToken cancellationToken = default)
    {
        if (!await HasMembershipAsync(userId, homeId, cancellationToken))
        {
            return new EnergyTariffListResult(EnergyTariffStatus.NotFound);
        }

        var tariffs = await dbContext.EnergyTariffs
            .AsNoTracking()
            .Where(tariff => tariff.HomeId == homeId)
            .OrderByDescending(tariff => tariff.EffectiveFrom)
            .Select(tariff => new EnergyTariffResponse(
                tariff.Id,
                tariff.HomeId,
                tariff.PricePerKWh,
                tariff.Currency,
                tariff.EffectiveFrom,
                tariff.EffectiveTo,
                tariff.CreatedAt,
                tariff.EffectiveTo == null))
            .ToListAsync(cancellationToken);

        return new EnergyTariffListResult(EnergyTariffStatus.Success, tariffs);
    }

    public async Task<EnergyTariffResult> GetCurrentAsync(
        Guid userId,
        Guid homeId,
        CancellationToken cancellationToken = default)
    {
        if (!await HasMembershipAsync(userId, homeId, cancellationToken))
        {
            return new EnergyTariffResult(EnergyTariffStatus.NotFound);
        }

        var tariff = await dbContext.EnergyTariffs
            .AsNoTracking()
            .Where(tariff => tariff.HomeId == homeId && tariff.EffectiveTo == null)
            .Select(tariff => new EnergyTariffResponse(
                tariff.Id,
                tariff.HomeId,
                tariff.PricePerKWh,
                tariff.Currency,
                tariff.EffectiveFrom,
                tariff.EffectiveTo,
                tariff.CreatedAt,
                true))
            .SingleOrDefaultAsync(cancellationToken);

        return tariff is null
            ? new EnergyTariffResult(EnergyTariffStatus.NoCurrentTariff)
            : new EnergyTariffResult(EnergyTariffStatus.Success, tariff);
    }

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

    private static EnergyTariffResponse MapResponse(EnergyTariff tariff) =>
        new(
            tariff.Id,
            tariff.HomeId,
            tariff.PricePerKWh,
            tariff.Currency,
            tariff.EffectiveFrom,
            tariff.EffectiveTo,
            tariff.CreatedAt,
            tariff.EffectiveTo == null);
}

public enum EnergyTariffStatus
{
    Success,
    NotFound,
    Forbidden,
    InvalidInput,
    Conflict,
    NoCurrentTariff
}

public record EnergyTariffResult(
    EnergyTariffStatus Status,
    EnergyTariffResponse? Tariff = null);

public record EnergyTariffListResult(
    EnergyTariffStatus Status,
    IReadOnlyList<EnergyTariffResponse>? Tariffs = null);
