using System.Security.Claims;

namespace SmartEnergy.Api.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
    {
        var subject = principal.FindFirst("sub")?.Value;
        return Guid.TryParse(subject, out userId);
    }
}
