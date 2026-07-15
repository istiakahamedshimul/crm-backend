namespace backend.Models;

public class SubGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string CompanyName { get; set; } = "Real Capital Group";
    public string? Description { get; set; }
    public List<Project> Projects { get; set; } = [];
}
