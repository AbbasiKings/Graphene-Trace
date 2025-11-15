using GrapheneTrace.Core.DTOs.Admin;

namespace GrapheneTrace.Api.Interfaces;

public interface IAdminService
{
    Task<DashboardKpiDto> GetDashboardKpisAsync(CancellationToken cancellationToken = default);
}

