using GrapheneTrace.Api.Data;
using GrapheneTrace.Api.Interfaces;
using GrapheneTrace.Core.DTOs.Admin;
using GrapheneTrace.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace GrapheneTrace.Api.Services;

public class AdminService(AppDbContext dbContext) : IAdminService
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<DashboardKpiDto> GetDashboardKpisAsync(CancellationToken cancellationToken = default)
    {
        var totalUsers = await _dbContext.Users.CountAsync(cancellationToken);
        var totalPatients = await _dbContext.Users.CountAsync(u => u.Role == UserRole.Patient, cancellationToken);
        var totalClinicians = await _dbContext.Users.CountAsync(u => u.Role == UserRole.Clinician, cancellationToken);
        var activeAlerts = await _dbContext.Alerts.CountAsync(a => a.Status != Core.Enums.AlertStatus.Resolved, cancellationToken);
        var lastData = await _dbContext.PatientData.OrderByDescending(pd => pd.Timestamp).FirstOrDefaultAsync(cancellationToken);
        var framesStored = await _dbContext.PatientData.LongCountAsync(cancellationToken);

        return new DashboardKpiDto
        {
            TotalUsers = totalUsers,
            TotalPatients = totalPatients,
            TotalClinicians = totalClinicians,
            ActiveAlerts = activeAlerts,
            LastDataReceived = lastData?.Timestamp,
            TotalFramesStored = framesStored
        };
    }
}

