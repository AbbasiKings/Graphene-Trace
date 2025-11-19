using GrapheneTrace.Core.DTOs.Admin;

namespace GrapheneTrace.Api.Interfaces;

public interface IAdminService
{
    Task<DashboardKpiDto> GetDashboardKpisAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminUserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<AdminUserDto?> UpdateUserAsync(Guid userId, UpdateUserDto data, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemAuditLogDto>> GetSystemAuditLogsAsync(int limit = 100, CancellationToken cancellationToken = default);
}

