using System.ComponentModel.DataAnnotations;

namespace SmartEnergy.Api.Features.Spaces.Dtos;

public class UpdateSpaceRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;
}
