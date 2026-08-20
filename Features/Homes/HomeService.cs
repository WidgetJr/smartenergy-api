using Microsoft.EntityFrameworkCore;
using SmartEnergy.Api.Domain.Entities;
using SmartEnergy.Api.Domain.Enums;
using SmartEnergy.Api.Features.Homes.Dtos;
using SmartEnergy.Api.Infrastructure.Persistence;

namespace SmartEnergy.Api.Features.Homes;

public class HomeService(AppDbContext dbContext)
{
    public async Task<HomeResponse> CreateAsync(
        Guid userId,
        CreateHomeRequest request,
        CancellationToken cancellationToken = default)
    {
        var createdAt = DateTimeOffset.UtcNow;
        var home = new Home
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            CreatedAt = createdAt
        };
        var ownerMembership = new HomeMember
        {
            HomeId = home.Id,
            UserId = userId,
            Role = HomeRole.Owner,
            JoinedAt = createdAt
        };

        dbContext.Homes.Add(home);
        dbContext.HomeMembers.Add(ownerMembership);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapResponse(home, ownerMembership.Role);
    }

    public async Task<IReadOnlyList<HomeResponse>> GetAccessibleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var memberships = await dbContext.HomeMembers
            .AsNoTracking()
            .Where(member => member.UserId == userId)
            .OrderBy(member => member.Home.CreatedAt)
            .Select(member => new
            {
                member.Home.Id,
                member.Home.Name,
                member.Role,
                member.Home.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return memberships
            .Select(member => new HomeResponse(
                member.Id,
                member.Name,
                member.Role.ToString(),
                member.CreatedAt))
            .ToList();
    }

    public async Task<HomeResponse?> GetByIdAsync(
        Guid userId,
        Guid homeId,
        CancellationToken cancellationToken = default)
    {
        var membership = await dbContext.HomeMembers
            .AsNoTracking()
            .Where(member => member.UserId == userId && member.HomeId == homeId)
            .Select(member => new
            {
                member.Home.Id,
                member.Home.Name,
                member.Role,
                member.Home.CreatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        return membership is null
            ? null
            : new HomeResponse(
                membership.Id,
                membership.Name,
                membership.Role.ToString(),
                membership.CreatedAt);
    }

    public async Task<UpdateHomeResult> UpdateAsync(
        Guid userId,
        Guid homeId,
        UpdateHomeRequest request,
        CancellationToken cancellationToken = default)
    {
        var membership = await dbContext.HomeMembers
            .Include(member => member.Home)
            .SingleOrDefaultAsync(
                member => member.UserId == userId && member.HomeId == homeId,
                cancellationToken);

        if (membership is null)
        {
            return new UpdateHomeResult(UpdateHomeStatus.NotFound);
        }

        if (membership.Role is not HomeRole.Owner and not HomeRole.Admin)
        {
            return new UpdateHomeResult(UpdateHomeStatus.Forbidden);
        }

        membership.Home.Name = request.Name.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateHomeResult(
            UpdateHomeStatus.Success,
            MapResponse(membership.Home, membership.Role));
    }

    private static HomeResponse MapResponse(Home home, HomeRole role) =>
        new(home.Id, home.Name, role.ToString(), home.CreatedAt);
}

public enum UpdateHomeStatus
{
    Success,
    NotFound,
    Forbidden
}

public record UpdateHomeResult(UpdateHomeStatus Status, HomeResponse? Home = null);
