using System.ComponentModel.DataAnnotations;

namespace SmartEnergy.Api.Features.Spaces.Dtos;

public class CreateSpaceRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;
}
