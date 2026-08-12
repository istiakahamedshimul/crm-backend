namespace backend.Models;

public class MonthlySalesTarget
{
    public int Id { get; set; }
    public int SalesExecutiveId { get; set; }
    public User SalesExecutive { get; set; } = null!;
    public DateOnly Month { get; set; }
    public int MinimumSalesUnits { get; set; }
    public decimal MinimumCollectionAmount { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int UpdatedById { get; set; }
    public User UpdatedBy { get; set; } = null!;
}
