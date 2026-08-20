using System.ComponentModel.DataAnnotations;

namespace SmartEnergy.Api.Features.Homes.Dtos;

public class UpdateHomeRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;
}
