namespace backend.Models;

public class PermissionGroup { public int Id { get; set; } public string Name { get; set; } = ""; public string? Description { get; set; } public List<Permission> Permissions { get; set; } = []; }
public class Permission { public int Id { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public int PermissionGroupId { get; set; } public PermissionGroup PermissionGroup { get; set; } = null!; }
public class RolePermission { public int RoleId { get; set; } public Role Role { get; set; } = null!; public int PermissionId { get; set; } public Permission Permission { get; set; } = null!; }
public class UserPermission { public int UserId { get; set; } public User User { get; set; } = null!; public int PermissionId { get; set; } public Permission Permission { get; set; } = null!; }

public static class PermissionCodes
{
    public const string UsersManage = "users.manage"; public const string RolesManage = "roles.manage"; public const string PermissionsManage = "permissions.manage";
    public const string LeadsManage = "leads.manage"; public const string BookingsManage = "bookings.manage"; public const string CustomersView = "customers.view"; public const string AgreementsManage = "agreements.manage";
    public const string PaymentsView = "payments.view"; public const string PaymentsRecord = "payments.record"; public const string PaymentsApprove = "payments.approve"; public const string PaymentsReverse = "payments.reverse";
    public const string EmiManage = "emi.manage"; public const string TransportationManage = "transportation.manage"; public const string NotificationsManage = "notifications.manage"; public const string ReportsView = "reports.view";
}
