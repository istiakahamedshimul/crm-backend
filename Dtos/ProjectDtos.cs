using backend.Models;

namespace backend.Dtos;

public record CreateProjectRequest(string Name, int SubGroupId, ProjectType Type, string Location, string? Address, string? Description, ProjectStatus Status);
public record CreateSubGroupRequest(string Name, string? Description);
public record CreateUnitRequest(int ProjectId, string? TowerOrBlock, int? FloorNumber, string UnitNumber, decimal SizeSqft, int? Bedrooms, int? Bathrooms, string? FacingDirection, decimal BasePrice, decimal FinalPrice, decimal BookingMoney);
