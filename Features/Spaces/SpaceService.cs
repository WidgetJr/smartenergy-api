using Microsoft.EntityFrameworkCore;
using SmartEnergy.Api.Domain.Entities;
using SmartEnergy.Api.Domain.Enums;
using SmartEnergy.Api.Features.Spaces.Dtos;
using SmartEnergy.Api.Infrastructure.Persistence;

namespace SmartEnergy.Api.Features.Spaces;

public class SpaceService(AppDbContext dbContext)
{
    public async Task<SpaceResult> CreateAsync(
        Guid userId,
        Guid homeId,
        CreateSpaceRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = await GetMembershipRoleAsync(userId, homeId, cancellationToken);
        if (role is null)
        {
            return new SpaceResult(SpaceOperationStatus.NotFound);
        }

        if (!CanManageSpaces(role.Value))
        {
            return new SpaceResult(SpaceOperationStatus.Forbidden);
        }

        var space = new Space
        {
            Id = Guid.NewGuid(),
            HomeId = homeId,
            Name = request.Name.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Spaces.Add(space);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SpaceResult(SpaceOperationStatus.Success, MapResponse(space));
    }

    public async Task<SpaceListResult> GetByHomeAsync(
        Guid userId,
        Guid homeId,
        CancellationToken cancellationToken = default)
    {
        var hasMembership = await dbContext.HomeMembers
            .AsNoTracking()
            .AnyAsync(
                member => member.UserId == userId && member.HomeId == homeId,
                cancellationToken);

        if (!hasMembership)
        {
            return new SpaceListResult(SpaceOperationStatus.NotFound);
        }

        var spaces = await dbContext.Spaces
            .AsNoTracking()
            .Where(space => space.HomeId == homeId)
            .OrderBy(space => space.Name)
            .ThenBy(space => space.CreatedAt)
            .Select(space => new SpaceResponse(
                space.Id,
                space.HomeId,
                space.Name,
                space.CreatedAt))
            .ToListAsync(cancellationToken);

        return new SpaceListResult(SpaceOperationStatus.Success, spaces);
    }

    public async Task<SpaceResult> GetByIdAsync(
        Guid userId,
        Guid homeId,
        Guid spaceId,
        CancellationToken cancellationToken = default)
    {
        var hasMembership = await dbContext.HomeMembers
            .AsNoTracking()
            .AnyAsync(
                member => member.UserId == userId && member.HomeId == homeId,
                cancellationToken);

        if (!hasMembership)
        {
            return new SpaceResult(SpaceOperationStatus.NotFound);
        }

        var space = await dbContext.Spaces
            .AsNoTracking()
            .Where(space => space.Id == spaceId && space.HomeId == homeId)
            .Select(space => new SpaceResponse(
                space.Id,
                space.HomeId,
                space.Name,
                space.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return space is null
            ? new SpaceResult(SpaceOperationStatus.NotFound)
            : new SpaceResult(SpaceOperationStatus.Success, space);
    }

    public async Task<SpaceResult> UpdateAsync(
        Guid userId,
        Guid homeId,
        Guid spaceId,
        UpdateSpaceRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = await GetMembershipRoleAsync(userId, homeId, cancellationToken);
        if (role is null)
        {
            return new SpaceResult(SpaceOperationStatus.NotFound);
        }

        var space = await dbContext.Spaces.SingleOrDefaultAsync(
            space => space.Id == spaceId && space.HomeId == homeId,
            cancellationToken);

        if (space is null)
        {
            return new SpaceResult(SpaceOperationStatus.NotFound);
        }

        if (!CanManageSpaces(role.Value))
        {
            return new SpaceResult(SpaceOperationStatus.Forbidden);
        }

        space.Name = request.Name.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SpaceResult(SpaceOperationStatus.Success, MapResponse(space));
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

    private static bool CanManageSpaces(HomeRole role) =>
        role is HomeRole.Owner or HomeRole.Admin;

    private static SpaceResponse MapResponse(Space space) =>
        new(space.Id, space.HomeId, space.Name, space.CreatedAt);
}

public enum SpaceOperationStatus
{
    Success,
    NotFound,
    Forbidden
}

public record SpaceResult(SpaceOperationStatus Status, SpaceResponse? Space = null);

public record SpaceListResult(
    SpaceOperationStatus Status,
    IReadOnlyList<SpaceResponse>? Spaces = null);
