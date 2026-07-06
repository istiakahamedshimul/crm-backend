using backend.Models;

namespace backend.Services;

public interface ILeadAssignmentService
{
    Task<User?> GetActiveSalesExecutiveAsync(int salesExecutiveId);
    Task<string?> GetAssignmentConflictAsync(string phone, string? email, int salesExecutiveId, int? ignoreLeadId = null, int? ignoreCustomerId = null);
}
