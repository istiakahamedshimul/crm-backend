namespace backend.Models;

public class SalesGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int GroupLeaderId { get; set; }
    public User GroupLeader { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SalesTeam
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SalesGroupId { get; set; }
    public SalesGroup SalesGroup { get; set; } = null!;
    public int? ParentTeamId { get; set; }
    public SalesTeam? ParentTeam { get; set; }
    public int? TeamLeaderId { get; set; }
    public User? TeamLeader { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SalesGroupTarget
{
    public int Id { get; set; }
    public int SalesGroupId { get; set; }
    public SalesGroup SalesGroup { get; set; } = null!;
    public DateOnly Month { get; set; }
    public int UnitTarget { get; set; }
    public decimal CollectionTarget { get; set; }
    public int UpdatedById { get; set; }
    public User UpdatedBy { get; set; } = null!;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class SalesTeamTarget
{
    public int Id { get; set; }
    public int SalesTeamId { get; set; }
    public SalesTeam SalesTeam { get; set; } = null!;
    public DateOnly Month { get; set; }
    public int UnitTarget { get; set; }
    public decimal CollectionTarget { get; set; }
    public int UpdatedById { get; set; }
    public User UpdatedBy { get; set; } = null!;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
