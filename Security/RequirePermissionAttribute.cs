using backend.Data;
using backend.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : TypeFilterAttribute
{
    public string Code { get; }

    public RequirePermissionAttribute(string code) : base(typeof(PermissionFilter))
    {
        Code = code;
        Arguments = [code];
    }
}

public sealed class PermissionFilter(CrmDbContext db, string code) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!(context.HttpContext.User.Identity?.IsAuthenticated ?? false)) { context.Result = new UnauthorizedResult(); return; }
        if (context.HttpContext.User.IsInRole("SuperAdmin")) return;
        // Sales customer access is a mandatory baseline capability. Individual
        // customer queries still enforce AssignedToId ownership in controllers.
        // This also keeps existing production Sales roles working if their
        // seeded role-permission row predates the permission migration.
        if (code == PermissionCodes.CustomersView && context.HttpContext.User.IsInRole("SalesExecutive")) return;
        if (context.HttpContext.User.IsInRole("CA") && code is PermissionCodes.CustomersView or PermissionCodes.PaymentsView or PermissionCodes.PaymentsRecord or PermissionCodes.PaymentsApprove or PermissionCodes.PaymentsReverse or PermissionCodes.ReportsView) return;
        if (context.HttpContext.User.IsInRole("CS") && code is PermissionCodes.CustomersView or PermissionCodes.AgreementsManage or PermissionCodes.EmiManage) return;
        if (context.HttpContext.User.IsInRole("Admin") && code is not PermissionCodes.UsersManage and not PermissionCodes.RolesManage and not PermissionCodes.PermissionsManage) return;
        var userId = context.HttpContext.User.UserId();
        var allowed = await db.UserPermissions.AnyAsync(x => x.UserId == userId && x.Permission.Code == code) ||
                      await db.Users.Where(x => x.Id == userId).SelectMany(x => x.Role.RolePermissions).AnyAsync(x => x.Permission.Code == code);
        if (!allowed) context.Result = new ForbidResult();
    }
}
