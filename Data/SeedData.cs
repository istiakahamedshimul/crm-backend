using backend.Models;
using backend.Services;

namespace backend.Data;

public static class SeedData
{
    public static void EnsureSeeded(CrmDbContext db)
    {
        var roleNames = new[] { "SuperAdmin", "Admin", "Manager", "SalesExecutive", "Accountant", "Customer" };
        foreach (var roleName in roleNames)
        {
            if (!db.Roles.Any(x => x.Name == roleName))
            {
                db.Roles.Add(new Role { Name = roleName });
            }
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
            db.CommissionRules.Add(new CommissionRule { Name = "Default 2%", Percentage = 2m, IsActive = true });
        }

        db.SaveChanges();
    }
}
