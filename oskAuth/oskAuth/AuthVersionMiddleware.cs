using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace oskAuth;

public sealed class AuthVersionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var userIdClaim = context.User.FindFirstValue("sub");
        var versionClaim = context.User.FindFirstValue("auth_version");

        if (!Guid.TryParse(userIdClaim, out var userId)
            || !long.TryParse(versionClaim, out var tokenVersion))
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            await next(context);
            return;
        }

        var user = await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(appUser => appUser.Id == userId, context.RequestAborted);

        if (user is null || user.AuthVersion != tokenVersion)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity());
            await next(context);
            return;
        }

        context.Items[typeof(AppUser)] = user;
        await next(context);
    }
}