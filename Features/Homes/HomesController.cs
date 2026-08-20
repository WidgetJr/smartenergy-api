using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEnergy.Api.Common.Extensions;
using SmartEnergy.Api.Features.Homes.Dtos;

namespace SmartEnergy.Api.Features.Homes;

[ApiController]
[Authorize]
[Route("api/homes")]
public class HomesController(HomeService homeService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<HomeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<HomeResponse>> Create(
        CreateHomeRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var response = await homeService.CreateAsync(userId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { homeId = response.Id }, response);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<HomeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<HomeResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var response = await homeService.GetAccessibleAsync(userId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{homeId:guid}")]
    [ProducesResponseType<HomeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HomeResponse>> GetById(
        Guid homeId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var response = await homeService.GetByIdAsync(userId, homeId, cancellationToken);
        return response is null ? HomeNotFound() : Ok(response);
    }

    [HttpPut("{homeId:guid}")]
    [ProducesResponseType<HomeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HomeResponse>> Update(
        Guid homeId,
        UpdateHomeRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await homeService.UpdateAsync(userId, homeId, request, cancellationToken);
        return result.Status switch
        {
            UpdateHomeStatus.Success => Ok(result.Home),
            UpdateHomeStatus.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "You do not have permission to update this home."
                }),
            _ => HomeNotFound()
        };
    }

    private NotFoundObjectResult HomeNotFound() =>
        NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Home not found."
        });
}
