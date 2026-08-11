using backend.Models;
using backend.Services;

namespace backend.Data;

public static class SeedData
{
    public static void EnsureSeeded(CrmDbContext db)
    {
        var roleNames = new[] { "SuperAdmin", "Admin", "Manager", "SalesExecutive", "Accountant", "Customer", "CS", "CA", "VehicleDepartment" };
        foreach (var roleName in roleNames)
        {
            if (!db.Roles.Any(x => x.Name == roleName))
            {
                db.Roles.Add(new Role { Name = roleName });
            }
        }

        db.SaveChanges();

        var definitions = new Dictionary<string, string[]>
        {
            ["User Management"] = [PermissionCodes.UsersManage], ["Role Management"] = [PermissionCodes.RolesManage], ["Permission Management"] = [PermissionCodes.PermissionsManage],
            ["Lead Management"] = [PermissionCodes.LeadsManage], ["Booking Management"] = [PermissionCodes.BookingsManage], ["Customer Management"] = [PermissionCodes.CustomersView], ["Payment Management"] = [PermissionCodes.PaymentsView, PermissionCodes.PaymentsRecord, PermissionCodes.PaymentsApprove, PermissionCodes.PaymentsReverse],
            ["EMI Management"] = [PermissionCodes.AgreementsManage, PermissionCodes.EmiManage], ["Transportation Management"] = [PermissionCodes.TransportationManage], ["Notification Management"] = [PermissionCodes.NotificationsManage], ["Reports"] = [PermissionCodes.ReportsView]
        };
        foreach (var definition in definitions)
        {
            var group = db.PermissionGroups.FirstOrDefault(x => x.Name == definition.Key) ?? new PermissionGroup { Name = definition.Key };
            if (group.Id == 0) db.PermissionGroups.Add(group);
            foreach (var code in definition.Value)
                if (!db.Permissions.Any(x => x.Code == code)) db.Permissions.Add(new Permission { Code = code, Name = code.Replace('.', ' '), PermissionGroup = group });
        }
        db.SaveChanges();
        void Grant(string roleName, params string[] codes) {
            var role = db.Roles.First(x => x.Name == roleName); foreach (var permission in db.Permissions.Where(x => codes.Contains(x.Code)))
                if (!db.RolePermissions.Any(x => x.RoleId == role.Id && x.PermissionId == permission.Id)) db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        }
        Grant("CS", PermissionCodes.CustomersView, PermissionCodes.AgreementsManage, PermissionCodes.EmiManage);
        Grant("CA", PermissionCodes.CustomersView, PermissionCodes.PaymentsView, PermissionCodes.PaymentsRecord, PermissionCodes.PaymentsApprove, PermissionCodes.PaymentsReverse, PermissionCodes.ReportsView);
        Grant("VehicleDepartment", PermissionCodes.TransportationManage);
        Grant("SalesExecutive", PermissionCodes.CustomersView);
        Grant("Admin", PermissionCodes.LeadsManage, PermissionCodes.CustomersView, PermissionCodes.PaymentsView, PermissionCodes.PaymentsRecord, PermissionCodes.PaymentsApprove, PermissionCodes.AgreementsManage, PermissionCodes.EmiManage, PermissionCodes.TransportationManage, PermissionCodes.NotificationsManage, PermissionCodes.ReportsView);
        db.SaveChanges();

        // Repair legacy Booked leads that predate automatic conversion. This is
        // idempotent because Customers.LeadId is unique and existing links are skipped.
        var convertedLeadIds = db.Customers.Where(x => x.LeadId.HasValue).Select(x => x.LeadId!.Value).ToHashSet();
        foreach (var lead in db.Leads.Where(x => x.Status == LeadStatus.Booked && !convertedLeadIds.Contains(x.Id)))
        {
            db.Customers.Add(new Customer
            {
                LeadId = lead.Id, Name = lead.CustomerName, Phone = lead.Phone,
                AlternativePhone = lead.AlternativePhone, Email = lead.Email, Address = lead.Address,
                AssignedToId = lead.AssignedToId, ProjectId = lead.ProjectId, PaymentStatus = "Unpaid",
                BookedAt = lead.CreatedAt, BookedById = lead.CreatedById
            });
        }
        db.SaveChanges();

        var adminRole = db.Roles.First(x => x.Name == "SuperAdmin");
        var salesRole = db.Roles.First(x => x.Name == "SalesExecutive");

        if (!db.Users.Any(x => x.Email == "admin@crm.local"))
        {
            db.Users.Add(new User
            {
                FullName = "CRM Admin",
                Email = "admin@crm.local",
                Phone = "01700000000",
                RoleId = adminRole.Id,
                PasswordHash = PasswordHash.Create("Admin@12345")
            });
        }

        if (!db.Users.Any(x => x.Email == "sales@crm.local"))
        {
            db.Users.Add(new User
            {
                FullName = "Demo Sales Executive",
                Email = "sales@crm.local",
                Phone = "01800000000",
                RoleId = salesRole.Id,
                PasswordHash = PasswordHash.Create("Sales@12345")
            });
        }

        if (!db.CommissionRules.Any())
        {
            db.CommissionRules.Add(new CommissionRule { Name = "Default 7%", Percentage = 7m, IsActive = true });
        }
        else
        {
            foreach (var rule in db.CommissionRules.Where(x => x.IsActive))
            {
                rule.Name = "Default 7%";
                rule.Percentage = 7m;
            }
        }

        db.SaveChanges();
    }
}
