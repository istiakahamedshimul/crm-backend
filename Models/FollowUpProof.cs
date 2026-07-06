namespace backend.Models;

public class FollowUpProof
{
    public int Id { get; set; }
    public int FollowUpId { get; set; }
    public FollowUp FollowUp { get; set; } = null!;
    public ProofType ProofType { get; set; }
    public string FileUrl { get; set; } = "";
}
