namespace GrapheneTrace.Core.Models;

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = string.Empty;
}

