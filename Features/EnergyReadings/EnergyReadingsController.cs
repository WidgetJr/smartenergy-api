using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEnergy.Api.Common.Extensions;
using SmartEnergy.Api.Features.EnergyReadings.Dtos;

namespace SmartEnergy.Api.Features.EnergyReadings;

[ApiController]
[Authorize]
[Route("api/homes/{homeId:guid}/spaces/{spaceId:guid}")]
public class EnergyReadingsController(EnergyReadingService energyReadingService) : ControllerBase
{
    [HttpPost("energy-readings")]
    [ProducesResponseType<EnergyReadingResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnergyReadingResponse>> Create(
        Guid homeId,
        Guid spaceId,
        CreateEnergyReadingRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await energyReadingService.CreateAsync(
            userId,
            homeId,
            spaceId,
            request,
            cancellationToken);

        return result.Status switch
        {
            EnergyReadingStatus.Success => StatusCode(
                StatusCodes.Status201Created,
                result.Reading),
            EnergyReadingStatus.InvalidInput => InvalidInput(),
            EnergyReadingStatus.DeviceInactive => DeviceInactive(),
            EnergyReadingStatus.DeviceConflict => DeviceConflict(),
            _ => ResourceNotFound()
        };
    }

    [HttpGet("energy-readings")]
    [ProducesResponseType<PagedEnergyReadingsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedEnergyReadingsResponse>> GetBySpace(
        Guid homeId,
        Guid spaceId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await energyReadingService.GetBySpaceAsync(
            userId,
            homeId,
            spaceId,
            from,
            to,
            page,
            pageSize,
            cancellationToken);

        return ListResponse(result);
    }

    [HttpGet("devices/{deviceId:guid}/energy-readings")]
    [ProducesResponseType<PagedEnergyReadingsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedEnergyReadingsResponse>> GetByDevice(
        Guid homeId,
        Guid spaceId,
        Guid deviceId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await energyReadingService.GetByDeviceAsync(
            userId,
            homeId,
            spaceId,
            deviceId,
            from,
            to,
            page,
            pageSize,
            cancellationToken);

        return ListResponse(result);
    }

    [HttpGet("devices/{deviceId:guid}/energy-readings/latest")]
    [ProducesResponseType<EnergyReadingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnergyReadingResponse>> GetLatest(
        Guid homeId,
        Guid spaceId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await energyReadingService.GetLatestAsync(
            userId,
            homeId,
            spaceId,
            deviceId,
            cancellationToken);

        return result.Status == EnergyReadingStatus.Success
            ? Ok(result.Reading)
            : ResourceNotFound();
    }

    private ActionResult<PagedEnergyReadingsResponse> ListResponse(
        EnergyReadingListResult result) =>
        result.Status switch
        {
            EnergyReadingStatus.Success => Ok(result.Readings),
            EnergyReadingStatus.InvalidInput => InvalidInput(),
            _ => ResourceNotFound()
        };

    private BadRequestObjectResult InvalidInput() =>
        BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Measurements, UTC date filters, or pagination values are invalid."
        });

    private ConflictObjectResult DeviceConflict() =>
        Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "The device is already registered and cannot be used in the requested resource."
        });

    private ConflictObjectResult DeviceInactive() =>
        Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "The device is not enabled to register measurements."
        });

    private NotFoundObjectResult ResourceNotFound() =>
        NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Home, space, device, or energy reading not found."
        });
}
