using backend.Data;
using backend.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace backend.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(string code) : TypeFilterAttribute(typeof(PermissionFilter))
{
    public string Code { get; } = code;
    public RequirePermissionAttribute(string code, bool marker = true) : this(code) { Arguments = [code]; }
}

public sealed class PermissionFilter(CrmDbContext db, string code) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!(context.HttpContext.User.Identity?.IsAuthenticated ?? false)) { context.Result = new UnauthorizedResult(); return; }
        if (context.HttpContext.User.IsInRole("SuperAdmin")) return;
        var userId = context.HttpContext.User.UserId();
        var allowed = await db.UserPermissions.AnyAsync(x => x.UserId == userId && x.Permission.Code == code) ||
                      await db.Users.Where(x => x.Id == userId).SelectMany(x => x.Role.RolePermissions).AnyAsync(x => x.Permission.Code == code);
        if (!allowed) context.Result = new ForbidResult();
    }
}
