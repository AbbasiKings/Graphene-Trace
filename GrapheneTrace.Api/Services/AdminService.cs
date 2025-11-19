using GrapheneTrace.Api.Data;
using GrapheneTrace.Api.Interfaces;
using GrapheneTrace.Core.DTOs.Admin;
using GrapheneTrace.Core.Enums;
using GrapheneTrace.Core.Models;
using GrapheneTrace.Core.Utils;
using Microsoft.EntityFrameworkCore;

namespace GrapheneTrace.Api.Services;

public class AdminService(AppDbContext dbContext, IAuditService auditService) : IAdminService
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly IAuditService _auditService = auditService;

    public async Task<DashboardKpiDto> GetDashboardKpisAsync(CancellationToken cancellationToken = default)
    {
        var totalUsers = await _dbContext.Users.CountAsync(cancellationToken);
        var totalPatients = await _dbContext.Users.CountAsync(u => u.Role == UserRole.Patient, cancellationToken);
        var totalClinicians = await _dbContext.Users.CountAsync(u => u.Role == UserRole.Clinician, cancellationToken);
        
        // Get alert counts by status
        var totalAlerts = await _dbContext.Alerts.CountAsync(cancellationToken);
        var newAlerts = await _dbContext.Alerts.CountAsync(a => a.Status == Core.Enums.AlertStatus.New, cancellationToken);
        var inReviewAlerts = await _dbContext.Alerts.CountAsync(a => a.Status == Core.Enums.AlertStatus.InReview, cancellationToken);
        var resolvedAlerts = await _dbContext.Alerts.CountAsync(a => a.Status == Core.Enums.AlertStatus.Resolved, cancellationToken);
        var autoClearedAlerts = await _dbContext.Alerts.CountAsync(a => a.Status == Core.Enums.AlertStatus.AutoCleared, cancellationToken);
        
        // Active alerts = New + InReview (excluding AutoCleared and Resolved)
        var activeAlerts = newAlerts + inReviewAlerts;
        
        var lastData = await _dbContext.PatientData.OrderByDescending(pd => pd.Timestamp).FirstOrDefaultAsync(cancellationToken);
        var framesStored = await _dbContext.PatientData.LongCountAsync(cancellationToken);

        return new DashboardKpiDto
        {
            TotalUsers = totalUsers,
            TotalPatients = totalPatients,
            TotalClinicians = totalClinicians,
            ActiveAlerts = activeAlerts,
            TotalAlerts = totalAlerts,
            NewAlerts = newAlerts,
            InReviewAlerts = inReviewAlerts,
            ResolvedAlerts = resolvedAlerts,
            AutoClearedAlerts = autoClearedAlerts,
            LastDataReceived = lastData?.Timestamp,
            TotalFramesStored = framesStored,
            Labels = new DashboardLabelsDto
            {
                PageTitle = "Admin Dashboard",
                PageSubtitle = "Monitoring system health, usage, and recent administrative activity.",
                ManageAdminsButton = "Manage Admins",
                
                ActivePatientsTitle = "Active Patients",
                ActivePatientsDescription = "Total registered patients",
                CliniciansTitle = "Clinicians",
                CliniciansDescription = "Licensed practitioners",
                DataVolumeTitle = "Data Volume",
                DataVolumeDescription = "Total pressure frames stored",
                ActiveAlertsTitle = "Active Alerts",
                ActiveAlertsDescription = "Unresolved alerts in system",
                
                SystemOverviewTitle = "System Overview",
                SystemOverviewSubtitle = "Total users and system statistics",
                TotalUsersLabel = "Total Users in System",
                LastDataReceivedLabel = "Last Data Received: ",
                
                SystemStatusTitle = "System Status",
                SystemStatusSubtitle = "Current system health indicators",
                TotalUsersStatusLabel = "Total Users",
                ActiveAlertsStatusLabel = "Active Alerts",
                DataFramesStatusLabel = "Data Frames",
                
                RecentAuditActivityTitle = "Recent Audit Activity",
                RecentAuditActivitySubtitle = "Ten most recent administrative and security events",
                ViewAuditConsoleButton = "View Audit Console",
                NoAuditLogsMessage = "No audit logs available.",
                
                TableHeaderWhen = "When",
                TableHeaderUser = "User",
                TableHeaderAction = "Action",
                TableHeaderTarget = "Target"
            }
        };
    }

    public async Task<IReadOnlyList<AdminUserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .Include(u => u.AssignedClinician)
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);

        return users.Select(u => new AdminUserDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role,
            IsActive = u.IsActive,
            LastLoginAt = u.LastLoginAt,
            AssignedClinicianId = u.AssignedClinicianId,
            AssignedClinicianName = u.AssignedClinician?.FullName
        }).ToList();
    }

    public async Task<AdminUserDto?> UpdateUserAsync(Guid userId, UpdateUserDto data, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.AssignedClinician)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        // Check if email is being changed and if it's already taken
        if (user.Email.ToLower() != data.Email.ToLower())
        {
            var emailExists = await _dbContext.Users
                .AnyAsync(u => u.Id != userId && u.Email.ToLower() == data.Email.ToLower(), cancellationToken);
            
            if (emailExists)
            {
                return null; // Email already exists
            }
        }

        // Update user properties
        user.FullName = data.FullName;
        user.Email = data.Email;
        user.Role = data.Role;
        user.IsActive = data.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        // Handle password update (only if provided)
        if (!string.IsNullOrWhiteSpace(data.Password))
        {
            user.PasswordHash = SecurityUtils.HashPassword(data.Password);
        }

        // Handle assigned clinician (only for patients)
        if (data.Role == UserRole.Patient)
        {
            // Validate clinician exists if assigned
            if (data.AssignedClinicianId.HasValue)
            {
                var clinicianExists = await _dbContext.Users
                    .AnyAsync(u => u.Id == data.AssignedClinicianId.Value && u.Role == UserRole.Clinician, cancellationToken);
                
                if (!clinicianExists)
                {
                    return null; // Invalid clinician
                }
            }
            user.AssignedClinicianId = data.AssignedClinicianId;
        }
        else
        {
            user.AssignedClinicianId = null; // Clear assignment for non-patients
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AdminUserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            AssignedClinicianId = user.AssignedClinicianId,
            AssignedClinicianName = user.AssignedClinician?.FullName
        };
    }

    public async Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return false;
        }

        // Check if user has associated data
        var hasPatientData = await _dbContext.PatientData.AnyAsync(pd => pd.PatientId == userId, cancellationToken);
        var hasAlerts = await _dbContext.Alerts.AnyAsync(a => a.PatientId == userId, cancellationToken);
        var hasComments = await _dbContext.Comments.AnyAsync(c => c.PatientId == userId || c.AuthorId == userId, cancellationToken);
        var hasAssignedPatients = user.Role == UserRole.Clinician && 
            await _dbContext.Users.AnyAsync(u => u.AssignedClinicianId == userId, cancellationToken);

        if (hasPatientData || hasAlerts || hasComments || hasAssignedPatients)
        {
            // Soft delete: deactivate instead of hard delete
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        // Hard delete if no associated data
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<SystemAuditLogDto>> GetSystemAuditLogsAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        var logs = await _dbContext.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return logs.Select(log =>
        {
            // Parse metadata to extract target entity info
            string targetEntity = "System";
            Guid? targetEntityId = null;

            try
            {
                if (!string.IsNullOrEmpty(log.MetadataJson))
                {
                    // Simple parsing - in production, use proper JSON deserialization
                    if (log.Action.Contains("User"))
                    {
                        targetEntity = "User";
                        // Extract ID from metadata if available
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return new SystemAuditLogDto
            {
                Id = log.Id,
                CreatedAt = log.CreatedAt,
                UserId = log.UserId,
                ActorName = log.User?.FullName ?? "System",
                Action = log.Action,
                TargetEntity = targetEntity,
                TargetEntityId = targetEntityId,
                MetadataJson = log.MetadataJson
            };
        }).ToList();
    }
}

