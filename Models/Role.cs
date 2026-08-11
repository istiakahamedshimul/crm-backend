namespace backend.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Department { get; set; }
    public bool IsActive { get; set; } = true;
    public List<RolePermission> RolePermissions { get; set; } = [];
}
