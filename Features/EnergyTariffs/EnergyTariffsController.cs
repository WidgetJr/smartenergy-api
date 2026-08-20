using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEnergy.Api.Common.Extensions;
using SmartEnergy.Api.Features.EnergyTariffs.Dtos;

namespace SmartEnergy.Api.Features.EnergyTariffs;

[ApiController]
[Authorize]
[Route("api/homes/{homeId:guid}/energy-tariffs")]
public class EnergyTariffsController(EnergyTariffService energyTariffService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<EnergyTariffResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnergyTariffResponse>> Create(
        Guid homeId,
        CreateEnergyTariffRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await energyTariffService.CreateAsync(
            userId,
            homeId,
            request,
            cancellationToken);

        return result.Status switch
        {
            EnergyTariffStatus.Success => StatusCode(
                StatusCodes.Status201Created,
                result.Tariff),
            EnergyTariffStatus.Forbidden => ForbiddenResult(),
            EnergyTariffStatus.InvalidInput => InvalidInputResult(),
            EnergyTariffStatus.Conflict => ConflictResult(),
            _ => HomeNotFound()
        };
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EnergyTariffResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<EnergyTariffResponse>>> GetHistory(
        Guid homeId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await energyTariffService.GetHistoryAsync(
            userId,
            homeId,
            cancellationToken);

        return result.Status == EnergyTariffStatus.Success
            ? Ok(result.Tariffs)
            : HomeNotFound();
    }

    [HttpGet("current")]
    [ProducesResponseType<EnergyTariffResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnergyTariffResponse>> GetCurrent(
        Guid homeId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await energyTariffService.GetCurrentAsync(
            userId,
            homeId,
            cancellationToken);

        return result.Status switch
        {
            EnergyTariffStatus.Success => Ok(result.Tariff),
            EnergyTariffStatus.NoCurrentTariff => NoCurrentTariff(),
            _ => HomeNotFound()
        };
    }

    private ObjectResult ForbiddenResult() =>
        StatusCode(
            StatusCodes.Status403Forbidden,
            new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "You do not have permission to manage energy tariffs in this home."
            });

    private BadRequestObjectResult InvalidInputResult() =>
        BadRequest(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Currency must contain exactly three letters and EffectiveFrom must be a UTC timestamp."
        });

    private ConflictObjectResult ConflictResult() =>
        Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "EffectiveFrom must be later than the current tariff's EffectiveFrom."
        });

    private NotFoundObjectResult HomeNotFound() =>
        NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Home not found."
        });

    private NotFoundObjectResult NoCurrentTariff() =>
        NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "No current energy tariff is configured."
        });
}
