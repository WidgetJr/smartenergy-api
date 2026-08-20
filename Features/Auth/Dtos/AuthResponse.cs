namespace SmartEnergy.Api.Features.Auth.Dtos;

public record AuthResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string AccessToken,
    DateTimeOffset ExpiresAt);
