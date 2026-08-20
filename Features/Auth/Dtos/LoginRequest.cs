using System.ComponentModel.DataAnnotations;

namespace SmartEnergy.Api.Features.Auth.Dtos;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;
}
