namespace backend.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? Designation { get; set; }
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool LocationTrackingEnabled { get; set; } = true;
    public DateTime? LocationTrackingChangedAtUtc { get; set; }
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public int? SalesTeamId { get; set; }
    public SalesTeam? SalesTeam { get; set; }
    public List<UserPermission> UserPermissions { get; set; } = [];
}
