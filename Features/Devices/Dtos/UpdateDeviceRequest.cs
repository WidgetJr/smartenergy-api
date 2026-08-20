using System.ComponentModel.DataAnnotations;

namespace SmartEnergy.Api.Features.Devices.Dtos;

public class UpdateDeviceRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
