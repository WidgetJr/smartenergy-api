using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEnergy.Api.Common.Extensions;
using SmartEnergy.Api.Features.Consumption.Dtos;

namespace SmartEnergy.Api.Features.Consumption;

[ApiController]
[Authorize]
[Route("api/homes/{homeId:guid}")]
public class ConsumptionController(ConsumptionService consumptionService) : ControllerBase
{
    [HttpGet("consumption")]
    [ProducesResponseType<ConsumptionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConsumptionResponse>> GetHome(
        Guid homeId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (from is null || to is null)
        {
            return InvalidRange();
        }

        var result = await consumptionService.GetHomeAsync(
            userId,
            homeId,
            from.Value,
            to.Value,
            cancellationToken);

        return result.Status switch
        {
            ConsumptionStatus.Success => Ok(result.Home),
            ConsumptionStatus.InvalidInput => InvalidRange(),
            _ => ResourceNotFound()
        };
    }

    [HttpGet("spaces/{spaceId:guid}/consumption")]
    [ProducesResponseType<SpaceConsumptionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpaceConsumptionResponse>> GetSpace(
        Guid homeId,
        Guid spaceId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (from is null || to is null)
        {
            return InvalidRange();
        }

        var result = await consumptionService.GetSpaceAsync(
            userId,
            homeId,
            spaceId,
            from.Value,
            to.Value,
            cancellationToken);

        return result.Status switch
        {
            ConsumptionStatus.Success => Ok(result.Space),
            ConsumptionStatus.InvalidInput => InvalidRange(),
            _ => ResourceNotFound()
        };
    }

    [HttpGet("spaces/{spaceId:guid}/devices/{deviceId:guid}/consumption")]
    [ProducesResponseType<DeviceConsumptionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceConsumptionResponse>> GetDevice(
        Guid homeId,
        Guid spaceId,
        Guid deviceId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (from is null || to is null)
        {
            return InvalidRange();
        }

        var result = await consumptionService.GetDeviceAsync(
            userId,
            homeId,
            spaceId,
            deviceId,
            from.Value,
            to.Value,
            cancellationToken);

        return result.Status switch
        {
            ConsumptionStatus.Success => Ok(result.Device),
            ConsumptionStatus.InvalidInput => InvalidRange(),
            _ => ResourceNotFound()
        };
    }

    private BadRequestObjectResult InvalidRange() =>
        BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "The required UTC range must satisfy from < to."
        });

    private NotFoundObjectResult ResourceNotFound() =>
        NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Home, space, or device not found."
        });
}
