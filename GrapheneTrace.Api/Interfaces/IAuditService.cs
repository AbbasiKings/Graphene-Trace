namespace GrapheneTrace.Api.Interfaces;

public interface IAuditService
{
    Task LogActionAsync(Guid? userId, string action, string? targetEntity = null, Guid? targetEntityId = null, string? metadataJson = null, CancellationToken cancellationToken = default);
}






