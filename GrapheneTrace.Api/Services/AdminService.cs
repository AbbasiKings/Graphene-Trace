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
            .Where(u => u.DeletedAt == null) // Filter out deleted users
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
        user.DateOfBirth = data.DateOfBirth;
        user.PhoneNumber = data.PhoneNumber;
        user.Address = data.Address;
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
            // Soft delete: deactivate and mark as deleted
            user.IsActive = false;
            user.DeletedAt = DateTime.UtcNow;
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

    public async Task<SystemConfigurationDto> GetSystemConfigurationAsync(CancellationToken cancellationToken = default)
    {
        // For now, return default configuration
        // In production, this would load from database or configuration store
        return new SystemConfigurationDto
        {
            Settings = new List<ConfigurationSettingDto>
            {
                new ConfigurationSettingDto
                {
                    Name = "Peak Pressure Index Minimum",
                    Description = "Default minimum value used when calculating PPI across patient datasets.",
                    Value = 10.5,
                    Min = 5,
                    Max = 20,
                    Suffix = "px"
                },
                new ConfigurationSettingDto
                {
                    Name = "Contact Area Threshold",
                    Description = "Lower threshold used when determining contact area percentage.",
                    Value = 32,
                    Min = 10,
                    Max = 60,
                    Suffix = "%"
                },
                new ConfigurationSettingDto
                {
                    Name = "Daily Alert Cap",
                    Description = "Maximum alerts per patient before automatic escalation is triggered.",
                    Value = 12,
                    Min = 5,
                    Max = 20,
                    Suffix = "alerts"
                }
            },
            AlertConfig = new AlertConfigurationDto
            {
                AlertingEnabled = true,
                AlertSensitivity = 68,
                EscalationWindow = "15 minutes",
                NotificationChannel = "Email"
            },
            DatabaseStatuses = await GetDatabaseStatusesAsync(cancellationToken),
            ContentTemplates = new List<ContentTemplateDto>
            {
                new ContentTemplateDto
                {
                    Id = "AlertEmail",
                    Description = "Urgent Alert Email",
                    Subject = "Action Required: Elevated Pressure Detected",
                    Body = "Hello {{ClinicianName}},\n\nThe system detected a sustained pressure reading above threshold for {{PatientName}} at {{Timestamp}}.\n\nRecommended next steps:\n• Review the latest frames in the clinician dashboard\n• Initiate outreach within 30 minutes if the readings persist\n\nRegards,\nGraphene Trace Platform"
                },
                new ContentTemplateDto
                {
                    Id = "Onboarding",
                    Description = "Patient Onboarding Email",
                    Subject = "Welcome to Graphene Trace",
                    Body = "Hello {{PatientName}},\n\nWelcome to the Graphene Trace platform. Your clinician {{ClinicianName}} has invited you to start monitoring pressure data using the Graphene sole sensors.\n\nTo get started:\n1. Download the Graphene Trace mobile app.\n2. Sign in using the email address you provided during enrollment.\n3. Follow the guided calibration steps.\n\nNeed help? Reply to this email or view our onboarding resources in the app.\n\nBest,\nThe Graphene Trace Team"
                },
                new ContentTemplateDto
                {
                    Id = "SuspensionNotice",
                    Description = "Account Suspension Notice",
                    Subject = "Notice: Account Suspension",
                    Body = "Hello {{UserName}},\n\nYour account has been temporarily suspended due to security policies. If this suspension was unexpected, please contact the system administrator at security@graphenetrace.com.\n\nRegards,\nSecurity Operations"
                }
            }
        };
    }

    public async Task<bool> UpdateSystemConfigurationAsync(UpdateConfigurationDto config, CancellationToken cancellationToken = default)
    {
        // For now, just return true (configuration would be saved to database in production)
        // In production, you would save to a Configuration table or appsettings
        await Task.CompletedTask;
        return true;
    }

    private async Task<List<DatabaseStatusDto>> GetDatabaseStatusesAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var statuses = new List<DatabaseStatusDto>();
        
        try
        {
            // Check database connectivity
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            
            int totalUsers = 0;
            int totalDataPoints = 0;
            
            if (canConnect)
            {
                try
                {
                    totalUsers = await _dbContext.Users.CountAsync(cancellationToken);
                }
                catch
                {
                    // Ignore count errors
                }
                
                try
                {
                    totalDataPoints = await _dbContext.PatientData.CountAsync(cancellationToken);
                }
                catch
                {
                    // Ignore count errors
                }
            }

            statuses.Add(new DatabaseStatusDto
            {
                System = "Primary Database",
                Status = canConnect ? "Operational" : "Degraded",
                LastUpdated = now,
                Description = canConnect 
                    ? $"Connected - {totalUsers} users, {totalDataPoints} data points"
                    : "Connection issues detected"
            });
        }
        catch (Exception ex)
        {
            statuses.Add(new DatabaseStatusDto
            {
                System = "Primary Database",
                Status = "Degraded",
                LastUpdated = now,
                Description = $"Error: {ex.Message}"
            });
        }

        // Add other statuses (these are mock statuses)
        statuses.Add(new DatabaseStatusDto
        {
            System = "Analytics Warehouse",
            Status = "Operational",
            LastUpdated = now.AddMinutes(-12),
            Description = "ETL sync completed"
        });
        
        statuses.Add(new DatabaseStatusDto
        {
            System = "Disaster Recovery",
            Status = "Standby",
            LastUpdated = now.AddHours(-1),
            Description = "Ready for failover - last replication 59 minutes ago"
        });
        
        statuses.Add(new DatabaseStatusDto
        {
            System = "Realtime Queue",
            Status = "Operational",
            LastUpdated = now.AddMinutes(-2),
            Description = "Normal operation"
        });
        
        statuses.Add(new DatabaseStatusDto
        {
            System = "Archive Storage",
            Status = "Operational",
            LastUpdated = now.AddMinutes(-19),
            Description = "New partitions allocated for current datasets"
        });
        
        statuses.Add(new DatabaseStatusDto
        {
            System = "Audit Store",
            Status = "Operational",
            LastUpdated = now.AddMinutes(-25),
            Description = "Write amplification within policy"
        });

        return statuses;
    }
}

