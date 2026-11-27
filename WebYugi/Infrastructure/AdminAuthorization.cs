using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace WebYugi.Infrastructure;

public sealed class AdminOnlyRequirement : IAuthorizationRequirement;

public sealed class AdminOnlyHandler(AppDbContext db) : AuthorizationHandler<AdminOnlyRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminOnlyRequirement requirement)
    {
        if (!int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return;
        var isAdmin = await db.Users.AsNoTracking().AnyAsync(user => user.Id == userId && user.IsAdmin);
        if (isAdmin) context.Succeed(requirement);
    }
}
