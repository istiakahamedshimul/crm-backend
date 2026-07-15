using backend.Models;

namespace backend.Dtos;

public record CreateProjectRequest(string Name, int SubGroupId, ProjectType Type, string Location, string? Address, string? Description, ProjectStatus Status);
public record CreateSubGroupRequest(string Name, string? Description);
