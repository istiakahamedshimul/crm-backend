namespace backend.Models;

public class CommissionRule
{
    public int Id { get; set; }
    public string Name { get; set; } = "Default";
    public decimal Percentage { get; set; } = 2m;
    public bool IsActive { get; set; } = true;
}
