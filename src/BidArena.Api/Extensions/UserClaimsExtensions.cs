using System.Security.Claims;

namespace BidArena.Api.Extensions;

public static class UserClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirst("sub")?.Value;

        return Guid.TryParse(sub, out var id)
            ? id
            : throw new UnauthorizedAccessException("Token ne sadrži važeći ID korisnika");
    }

    // Za endpointe koji rade i bez prijave: vraca null ako nema validnog tokena.
    public static Guid? TryGetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}