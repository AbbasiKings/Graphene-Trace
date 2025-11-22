namespace GrapheneTrace.Core.DTOs.Admin;

public class SystemAuditLogDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? UserId { get; set; }
    public string? ActorName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public Guid? TargetEntityId { get; set; }
    public string MetadataJson { get; set; } = string.Empty;
}





