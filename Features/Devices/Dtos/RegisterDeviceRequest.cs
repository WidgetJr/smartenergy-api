using System.ComponentModel.DataAnnotations;

namespace SmartEnergy.Api.Features.Devices.Dtos;

public class RegisterDeviceRequest
{
    [Required]
    [MaxLength(100)]
    public string SerialNumber { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Name { get; set; }
}
