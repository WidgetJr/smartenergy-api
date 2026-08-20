using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartEnergy.Api.Common.Extensions;
using SmartEnergy.Api.Features.Spaces.Dtos;

namespace SmartEnergy.Api.Features.Spaces;

[ApiController]
[Authorize]
[Route("api/homes/{homeId:guid}/spaces")]
public class SpacesController(SpaceService spaceService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<SpaceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpaceResponse>> Create(
        Guid homeId,
        CreateSpaceRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await spaceService.CreateAsync(
            userId,
            homeId,
            request,
            cancellationToken);

        return result.Status switch
        {
            SpaceOperationStatus.Success => CreatedAtAction(
                nameof(GetById),
                new { homeId, spaceId = result.Space!.Id },
                result.Space),
            SpaceOperationStatus.Forbidden => SpaceForbidden(),
            _ => ResourceNotFound()
        };
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SpaceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SpaceResponse>>> GetAll(
        Guid homeId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await spaceService.GetByHomeAsync(userId, homeId, cancellationToken);
        return result.Status == SpaceOperationStatus.Success
            ? Ok(result.Spaces)
            : ResourceNotFound();
    }

    [HttpGet("{spaceId:guid}")]
    [ProducesResponseType<SpaceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpaceResponse>> GetById(
        Guid homeId,
        Guid spaceId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await spaceService.GetByIdAsync(
            userId,
            homeId,
            spaceId,
            cancellationToken);

        return result.Status == SpaceOperationStatus.Success
            ? Ok(result.Space)
            : ResourceNotFound();
    }

    [HttpPut("{spaceId:guid}")]
    [ProducesResponseType<SpaceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpaceResponse>> Update(
        Guid homeId,
        Guid spaceId,
        UpdateSpaceRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await spaceService.UpdateAsync(
            userId,
            homeId,
            spaceId,
            request,
            cancellationToken);

        return result.Status switch
        {
            SpaceOperationStatus.Success => Ok(result.Space),
            SpaceOperationStatus.Forbidden => SpaceForbidden(),
            _ => ResourceNotFound()
        };
    }

    private ObjectResult SpaceForbidden() =>
        StatusCode(
            StatusCodes.Status403Forbidden,
            new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "You do not have permission to manage spaces in this home."
            });

    private NotFoundObjectResult ResourceNotFound() =>
        NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Home or space not found."
        });
}
