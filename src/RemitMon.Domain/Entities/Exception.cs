using RemitMon.Domain.Enums;

namespace RemitMon.Domain.Entities;

public class Exception : AuditableEntity
{
    public Guid DecisionId { get; set; }
    public ExceptionStatus Status { get; set; } = ExceptionStatus.Open;
    public Guid? AssignedToUserId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ResolutionNote { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    
    public Decision? Decision { get; set; }
}
