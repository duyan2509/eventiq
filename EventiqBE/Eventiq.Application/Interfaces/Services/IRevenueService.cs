using Eventiq.Application.Dtos;
using Eventiq.Domain.Entities;

namespace Eventiq.Application.Interfaces.Services;

public interface IRevenueService
{

    Task<AdminRevenueReportDto> GetAdminRevenueReportAsync(int? month = null, int? year = null);
    Task<OrgRevenueReportDto> GetOrgRevenueReportAsync(Guid eventId, Guid organizationId);
    Task<OrgRevenueStatsDto> GetOrgRevenueStatsAsync(Guid eventId, Guid organizationId);
    Task<PaginatedResult<OrgRevenueTableDto>> GetOrgRevenueTableAsync(
        Guid eventId, 
        Guid organizationId, 
        Guid? eventItemId = null, 
        Guid? ticketClassId = null, 
        int page = 1, 
        int size = 10);
    Task<List<UserTicketDto>> GetUserTicketsAsync(Guid userId);
    Task<List<PayoutDto>> GetPendingPayoutsAsync();
    Task<PayoutDto?> GetPayoutByEventItemIdAsync(Guid eventItemId);
    Task<PayoutDto> UpdatePayoutAsync(Guid payoutId, UpdatePayoutDto request, string adminUserId, Stream? proofImageStream = null, string? proofImageFileName = null);
    Task<PaginatedResult<PayoutDto>> GetPayoutsByFiltersAsync(PayoutStatus? status, int? month, int? year, int page = 1, int size = 10);
    Task<List<PayoutDto>> GetPayoutHistoryByOrganizationIdAsync(Guid organizationId);
    Task<int> GetPendingPayoutEventsCountAsync(int? month = null, int? year = null);
}

