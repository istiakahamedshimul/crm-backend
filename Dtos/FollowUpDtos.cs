using backend.Models;

namespace backend.Dtos;

public record CreateFollowUpRequest(int LeadId, int? CustomerId, FollowUpType Type, string Summary, string? CustomerResponse, DateTime? NextFollowUpAt, LeadStatus? NewLeadStatus, List<ProofRequest> Proofs);
public record ProofRequest(ProofType ProofType, string FileUrl);
