using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEnergy.Api.Common.Extensions;
using SmartEnergy.Api.Features.Devices.Dtos;

namespace SmartEnergy.Api.Features.Devices;

[ApiController]
[Authorize]
[Route("api/homes/{homeId:guid}/spaces/{spaceId:guid}/devices")]
public class DevicesController(DeviceService deviceService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<DeviceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<DeviceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeviceResponse>> Register(
        Guid homeId,
        Guid spaceId,
        RegisterDeviceRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await deviceService.RegisterAsync(
            userId,
            homeId,
            spaceId,
            request,
            cancellationToken);

        return result.Status switch
        {
            DeviceOperationStatus.Success when result.IsCreated => CreatedAtAction(
                nameof(GetById),
                new { homeId, spaceId, deviceId = result.Device!.Id },
                result.Device),
            DeviceOperationStatus.Success => Ok(result.Device),
            DeviceOperationStatus.InvalidInput => InvalidInput(),
            DeviceOperationStatus.Forbidden => DeviceForbidden(),
            DeviceOperationStatus.Conflict => DeviceConflict(),
            _ => ResourceNotFound()
        };
    }
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DeviceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DeviceResponse>>> GetAll(
        Guid homeId,
        Guid spaceId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await deviceService.GetBySpaceAsync(
            userId,
            homeId,
            spaceId,
            cancellationToken);

        return result.Status == DeviceOperationStatus.Success
            ? Ok(result.Devices)
            : ResourceNotFound();
    }

    [HttpGet("{deviceId:guid}")]
    [ProducesResponseType<DeviceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceResponse>> GetById(
        Guid homeId,
        Guid spaceId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await deviceService.GetByIdAsync(
            userId,
            homeId,
            spaceId,
            deviceId,
            cancellationToken);

        return result.Status == DeviceOperationStatus.Success
            ? Ok(result.Device)
            : ResourceNotFound();
    }

    [HttpPut("{deviceId:guid}")]
    [ProducesResponseType<DeviceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceResponse>> Update(
        Guid homeId,
        Guid spaceId,
        Guid deviceId,
        UpdateDeviceRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await deviceService.UpdateAsync(
            userId,
            homeId,
            spaceId,
            deviceId,
            request,
            cancellationToken);

        return result.Status switch
        {
            DeviceOperationStatus.Success => Ok(result.Device),
            DeviceOperationStatus.InvalidInput => InvalidInput(),
            DeviceOperationStatus.Forbidden => DeviceForbidden(),
            _ => ResourceNotFound()
        };
    }

    private BadRequestObjectResult InvalidInput() =>
        BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest });

    private ObjectResult DeviceForbidden() =>
        StatusCode(
            StatusCodes.Status403Forbidden,
            new ProblemDetails { Status = StatusCodes.Status403Forbidden });

    private ConflictObjectResult DeviceConflict() =>
        Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict });

    private NotFoundObjectResult ResourceNotFound() =>
        NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound });
}
