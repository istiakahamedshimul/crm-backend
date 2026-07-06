using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class LeadAssignmentService(CrmDbContext db) : ILeadAssignmentService
{
    public async Task<User?> GetActiveSalesExecutiveAsync(int salesExecutiveId)
    {
        return await db.Users.Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == salesExecutiveId && x.IsActive && x.Role.Name == "SalesExecutive");
    }

    public async Task<string?> GetAssignmentConflictAsync(
        string phone,
        string? email,
        int salesExecutiveId,
        int? ignoreLeadId = null,
        int? ignoreCustomerId = null)
    {
        var normalizedPhone = phone.Trim();
        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLower();

        var conflictingLead = await db.Leads
            .Where(x => x.Id != ignoreLeadId)
            .Where(x => x.AssignedToId.HasValue && x.AssignedToId != salesExecutiveId)
            .Where(x =>
                x.Phone == normalizedPhone ||
                x.AlternativePhone == normalizedPhone ||
                (normalizedEmail != null && x.Email != null && x.Email.ToLower() == normalizedEmail))
            .Select(x => new { x.Id, x.CustomerName, x.AssignedToId })
            .FirstOrDefaultAsync();

        if (conflictingLead is not null)
        {
            return $"Customer already exists as lead #{conflictingLead.Id} and is assigned to another sales executive.";
        }

        var conflictingCustomer = await db.Customers
            .Where(x => x.Id != ignoreCustomerId)
            .Where(x => x.AssignedToId.HasValue && x.AssignedToId != salesExecutiveId)
            .Where(x =>
                x.Phone == normalizedPhone ||
                x.AlternativePhone == normalizedPhone ||
                (normalizedEmail != null && x.Email != null && x.Email.ToLower() == normalizedEmail))
            .Select(x => new { x.Id, x.Name, x.AssignedToId })
            .FirstOrDefaultAsync();

        if (conflictingCustomer is not null)
        {
            return $"Customer profile #{conflictingCustomer.Id} is already assigned to another sales executive.";
        }

        return null;
    }
}
