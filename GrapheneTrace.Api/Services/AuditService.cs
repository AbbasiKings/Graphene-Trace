using GrapheneTrace.Api.Data;
using GrapheneTrace.Api.Interfaces;
using GrapheneTrace.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GrapheneTrace.Api.Services;

public class AuditService(AppDbContext dbContext) : IAuditService
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task LogActionAsync(Guid? userId, string action, string? targetEntity = null, Guid? targetEntityId = null, string? metadataJson = null, CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            UserId = userId,
            Action = action,
            MetadataJson = metadataJson ?? "{}"
        };

        _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}





