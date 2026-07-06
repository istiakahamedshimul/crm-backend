namespace backend.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ProjectType Type { get; set; } = ProjectType.Apartment;
    public string Location { get; set; } = "";
    public string? Address { get; set; }
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Ongoing;
    public List<PropertyUnit> Units { get; set; } = [];
}
