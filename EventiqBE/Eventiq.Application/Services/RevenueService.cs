using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Application.Interfaces.Services;
using Eventiq.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Eventiq.Application.Services;

public class RevenueService : IRevenueService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPayoutRepository _payoutRepository;
    private readonly IEventItemRepository _eventItemRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketClassRepository _ticketClassRepository;
    private readonly IEventSeatRepository _eventSeatRepository;
    private readonly ICloudStorageService _cloudStorageService;
    private readonly ILogger<RevenueService> _logger;

    public RevenueService(
        IPaymentRepository paymentRepository,
        IPayoutRepository payoutRepository,
        IEventItemRepository eventItemRepository,
        IEventRepository eventRepository,
        ITicketRepository ticketRepository,
        ITicketClassRepository ticketClassRepository,
        IEventSeatRepository eventSeatRepository,
        ICloudStorageService cloudStorageService,
        ILogger<RevenueService> logger)
    {
        _paymentRepository = paymentRepository;
        _payoutRepository = payoutRepository;
        _eventItemRepository = eventItemRepository;
        _eventRepository = eventRepository;
        _ticketRepository = ticketRepository;
        _ticketClassRepository = ticketClassRepository;
        _eventSeatRepository = eventSeatRepository;
        _cloudStorageService = cloudStorageService;
        _logger = logger;
    }

    public async Task<AdminRevenueReportDto> GetAdminRevenueReportAsync(int? month = null, int? year = null)
    {
        var targetDate = DateTime.UtcNow;
        if (year.HasValue)
        {
            targetDate = new DateTime(year.Value, month ?? 1, 1);
        }

        var startDate = new DateTime(targetDate.Year, month ?? targetDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddMilliseconds(-1); // Last moment of the last day of the month (23:59:59.999)

        _logger.LogInformation("Getting admin revenue report for period: {StartDate} to {EndDate}", startDate, endDate);

        // Get all successful payments in the period
        var allPayments = await _paymentRepository.GetAllAsync();
        var successfulPayments = allPayments.Where(p => p.Status == PaymentStatus.SUCCESS).ToList();
        
        _logger.LogInformation("Total payments: {Total}, Successful payments: {Successful}", 
            allPayments.Count(), successfulPayments.Count);
        
        // Filter by date - use PaidAt if available, otherwise use CreatedAt as fallback
        var paymentsInPeriod = successfulPayments
            .Where(p => 
            {
                var paymentDate = p.PaidAt ?? p.CreatedAt;
                return paymentDate >= startDate && paymentDate <= endDate;
            })
            .ToList();

        _logger.LogInformation("Found {Count} successful payments in period {StartDate} to {EndDate}", 
            paymentsInPeriod.Count, startDate, endDate);
        
        foreach (var payment in successfulPayments)
        {
            _logger.LogInformation("Payment {PaymentId}: Status={Status}, PaidAt={PaidAt}, CreatedAt={CreatedAt}, GrossAmount={GrossAmount}",
                payment.PaymentId, payment.Status, payment.PaidAt, payment.CreatedAt, payment.GrossAmount);
        }

        var totalMonthlyRevenue = paymentsInPeriod.Sum(p => p.GrossAmount);
        var totalPlatformFee = paymentsInPeriod.Sum(p => p.PlatformFee);
        var totalOrgAmount = paymentsInPeriod.Sum(p => p.OrgAmount);
        var totalTicketsSold = paymentsInPeriod.Count;

        var eventItemIds = paymentsInPeriod.Select(p => p.EventItemId).Distinct();
        var eventItems = new List<EventItemRevenueDto>();

        foreach (var eventItemId in eventItemIds)
        {
            var eventItemPayments = paymentsInPeriod.Where(p => p.EventItemId == eventItemId).ToList();
            var eventItem = await _eventItemRepository.GetByDetailByIdAsync(eventItemId);
            
            if (eventItem != null)
            {
                var payout = await _payoutRepository.GetByEventItemIdAsync(eventItemId);
                
                eventItems.Add(new EventItemRevenueDto
                {
                    EventItemId = eventItem.Id,
                    EventItemName = eventItem.Name,
                    EventId = eventItem.EventId,
                    EventName = eventItem.Event?.Name ?? "",
                    TicketsSold = eventItemPayments.Count,
                    GrossRevenue = eventItemPayments.Sum(p => p.GrossAmount),
                    PlatformFee = eventItemPayments.Sum(p => p.PlatformFee),
                    OrgAmount = eventItemPayments.Sum(p => p.OrgAmount),
                    PayoutStatus = payout?.Status.ToString() ?? "PENDING",
                    PaidAt = payout?.PaidAt
                });
            }
        }

        // Group by Event for Revenue tab
        var events = new List<EventRevenueDto>();
        var eventGroups = new Dictionary<Guid, List<Payment>>();

        // Group payments by EventId
        foreach (var payment in paymentsInPeriod)
        {
            var eventItem = await _eventItemRepository.GetByDetailByIdAsync(payment.EventItemId);
            if (eventItem != null && eventItem.EventId != Guid.Empty)
            {
                if (!eventGroups.ContainsKey(eventItem.EventId))
                {
                    eventGroups[eventItem.EventId] = new List<Payment>();
                }
                eventGroups[eventItem.EventId].Add(payment);
            }
        }

        foreach (var kvp in eventGroups)
        {
            var eventId = kvp.Key;
            var eventPayments = kvp.Value;
            var eventEntity = await _eventRepository.GetByIdAsync(eventId);
            
            if (eventEntity != null)
            {
                var firstEventItem = await _eventItemRepository.GetByDetailByIdAsync(eventPayments.First().EventItemId);
                var totalTickets = 0;
                if (firstEventItem != null)
                {
                    var seats = await _eventSeatRepository.GetByChartIdAsync(firstEventItem.ChartId);
                    totalTickets = seats.Count();
                }
                var ticketsSold = eventPayments.Count;

                events.Add(new EventRevenueDto
                {
                    EventId = eventId,
                    EventName = eventEntity.Name,
                    TicketsSold = ticketsSold,
                    TotalTickets = totalTickets,
                    PlatformFee = eventPayments.Sum(p => p.PlatformFee)
                });
            }
        }

        var pendingPayoutEventsCount = await GetPendingPayoutEventsCountAsync(month, year);

        return new AdminRevenueReportDto
        {
            TotalMonthlyRevenue = totalMonthlyRevenue,
            TotalPlatformFee = totalPlatformFee,
            TotalOrgAmount = totalOrgAmount,
            TotalTicketsSold = totalTicketsSold,
            PendingPayoutEventsCount = pendingPayoutEventsCount,
            EventItems = eventItems.OrderByDescending(ei => ei.GrossRevenue).ToList(),
            Events = events.OrderByDescending(e => e.PlatformFee).ToList()
        };
    }

    public async Task<OrgRevenueReportDto> GetOrgRevenueReportAsync(Guid eventId, Guid organizationId)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId);
        if (eventEntity == null)
        {
            throw new UnauthorizedAccessException("Event not found");
        }
        

        if (organizationId != Guid.Empty && eventEntity.OrganizationId != organizationId)
        {
            throw new UnauthorizedAccessException("Event does not belong to organization");
        }
        
        organizationId = eventEntity.OrganizationId;

        var eventItems = await _eventItemRepository.GetAllByEventIdAsync(eventId);
        var eventItemReports = new List<OrgEventItemReportDto>();

        foreach (var eventItem in eventItems)
        {
            var payments = await _paymentRepository.GetSuccessfulPaymentsByEventItemIdAsync(eventItem.Id);
            var tickets = await _ticketRepository.GetByEventItemIdAsync(eventItem.Id);
            var ticketClasses = await _ticketRepository.GetTicketClassesByEventItemIdAsync(eventItem.Id);
            var totalTicketsFromClasses = ticketClasses.Sum(tc => tc.TotalQuantity);
            int totalTickets;
            if (totalTicketsFromClasses > 0)
            {
                totalTickets = totalTicketsFromClasses;
            }
            else
            {
                var seats = await _eventSeatRepository.GetByChartIdAsync(eventItem.ChartId);
                totalTickets = seats.Count();
            }
            var ticketsSold = tickets.Count();
            var grossRevenue = payments.Sum(p => p.GrossAmount);
            var orgAmount = payments.Sum(p => p.OrgAmount);
            
            var payout = await _payoutRepository.GetByEventItemIdAsync(eventItem.Id);

            eventItemReports.Add(new OrgEventItemReportDto
            {
                EventItemId = eventItem.Id,
                EventItemName = eventItem.Name,
                TotalTickets = totalTickets,
                TicketsSold = ticketsSold,
                TicketsRemaining = totalTickets - ticketsSold,
                GrossRevenue = grossRevenue,
                OrgAmount = orgAmount,
                PayoutStatus = payout?.Status.ToString() ?? "PENDING",
                ProofImageUrl = payout?.ProofImageUrl,
                PaidAt = payout?.PaidAt
            });
        }

        return new OrgRevenueReportDto
        {
            EventId = eventId,
            EventName = eventEntity.Name,
            EventItems = eventItemReports,
            TotalGrossRevenue = eventItemReports.Sum(ei => ei.GrossRevenue),
            TotalOrgAmount = eventItemReports.Sum(ei => ei.OrgAmount),
            TotalTicketsSold = eventItemReports.Sum(ei => ei.TicketsSold)
        };
    }

    public async Task<OrgRevenueStatsDto> GetOrgRevenueStatsAsync(Guid eventId, Guid organizationId)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId);
        if (eventEntity == null)
        {
            throw new UnauthorizedAccessException("Event not found");
        }

        if (organizationId != Guid.Empty && eventEntity.OrganizationId != organizationId)
        {
            throw new UnauthorizedAccessException("Event does not belong to organization");
        }

        organizationId = eventEntity.OrganizationId;

        var eventItems = await _eventItemRepository.GetAllByEventIdAsync(eventId);
        var allPayments = new List<Payment>();
        var allTickets = new List<Ticket>();
        var ticketClassRevenues = new Dictionary<Guid, TicketClassRevenueDto>();
        var ticketClassCache = new Dictionary<Guid, TicketClass>();

        foreach (var eventItem in eventItems)
        {
            var payments = await _paymentRepository.GetSuccessfulPaymentsByEventItemIdAsync(eventItem.Id);
            var tickets = await _ticketRepository.GetByEventItemIdAsync(eventItem.Id);
            allPayments.AddRange(payments);
            allTickets.AddRange(tickets);
        }

        var ticketClassIds = allTickets.Select(t => t.TicketClassId).Distinct().ToList();
        
        foreach (var ticketClassId in ticketClassIds)
        {
            if (!ticketClassCache.ContainsKey(ticketClassId))
            {
                var ticketClass = await _ticketClassRepository.GetByIdAsync(ticketClassId);
                if (ticketClass != null)
                {
                    ticketClassCache[ticketClassId] = ticketClass;
                }
            }
        }

        foreach (var ticket in allTickets)
        {
            if (!ticketClassRevenues.ContainsKey(ticket.TicketClassId))
            {
                var tc = ticketClassCache.GetValueOrDefault(ticket.TicketClassId);
                ticketClassRevenues[ticket.TicketClassId] = new TicketClassRevenueDto
                {
                    TicketClassId = ticket.TicketClassId,
                    TicketClassName = tc?.Name ?? "Unknown",
                    Revenue = 0,
                    TicketsSold = 0
                };
            }
            
            var ticketClassEntity = ticketClassCache.GetValueOrDefault(ticket.TicketClassId);
            if (ticketClassEntity != null)
            {
                ticketClassRevenues[ticket.TicketClassId].Revenue += ticketClassEntity.Price * 0.8m; // 80% for org
                ticketClassRevenues[ticket.TicketClassId].TicketsSold += 1;
            }
        }

        var firstEventItem = eventItems.FirstOrDefault();
        var payout = firstEventItem != null ? await _payoutRepository.GetByEventItemIdAsync(firstEventItem.Id) : null;

        return new OrgRevenueStatsDto
        {
            TotalSoldTickets = allTickets.Count,
            TotalGrossRevenue = allPayments.Sum(p => p.GrossAmount),
            TotalOrganizationAmount = allPayments.Sum(p => p.OrgAmount),
            PayoutStatus = payout?.Status.ToString() ?? "PENDING",
            PayoutDate = payout?.PaidAt,
            ProofImageUrl = payout?.ProofImageUrl,
            TicketClassRevenues = ticketClassRevenues.Values.ToList()
        };
    }

    public async Task<PaginatedResult<OrgRevenueTableDto>> GetOrgRevenueTableAsync(
        Guid eventId, 
        Guid organizationId, 
        Guid? eventItemId = null, 
        Guid? ticketClassId = null, 
        int page = 1, 
        int size = 10)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(eventId);
        if (eventEntity == null)
        {
            throw new UnauthorizedAccessException("Event not found");
        }

        if (organizationId != Guid.Empty && eventEntity.OrganizationId != organizationId)
        {
            throw new UnauthorizedAccessException("Event does not belong to organization");
        }

        organizationId = eventEntity.OrganizationId;

        var eventItems = await _eventItemRepository.GetAllByEventIdAsync(eventId);
        var tableData = new List<OrgRevenueTableDto>();

        foreach (var eventItem in eventItems)
        {
            if (eventItemId.HasValue && eventItem.Id != eventItemId.Value)
                continue;

            var payments = await _paymentRepository.GetSuccessfulPaymentsByEventItemIdAsync(eventItem.Id);
            var tickets = await _ticketRepository.GetByEventItemIdAsync(eventItem.Id);
            var ticketClasses = await _ticketRepository.GetTicketClassesByEventItemIdAsync(eventItem.Id);

            foreach (var ticketClass in ticketClasses)
            {
                if (ticketClassId.HasValue && ticketClass.Id != ticketClassId.Value)
                    continue;

                var classTickets = tickets.Where(t => t.TicketClassId == ticketClass.Id).ToList();
                var ticketsSold = classTickets.Count;
                var totalTicketsFromClass = ticketClass.TotalQuantity;
                var totalTickets = totalTicketsFromClass > 0 
                    ? totalTicketsFromClass 
                    : (await _eventSeatRepository.GetByChartIdAsync(eventItem.ChartId)).Count(s => s.CategoryKey == ticketClass.Name);
                
                var grossRevenue = ticketClass.Price * ticketsSold;
                var orgAmount = grossRevenue * 0.8m; 

                tableData.Add(new OrgRevenueTableDto
                {
                    EventItemId = eventItem.Id,
                    EventItemName = eventItem.Name,
                    TicketClassId = ticketClass.Id,
                    TicketClassName = ticketClass.Name,
                    TotalTickets = totalTickets,
                    SoldTickets = ticketsSold,
                    GrossRevenue = grossRevenue,
                    OrganizationAmount = orgAmount
                });
            }
        }

        var total = tableData.Count;
        var pagedData = tableData
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();

        return new PaginatedResult<OrgRevenueTableDto>
        {
            Data = pagedData,
            Total = total,
            Page = page,
            Size = size
        };
    }

    public async Task<List<UserTicketDto>> GetUserTicketsAsync(Guid userId)
    {
        var tickets = await _ticketRepository.GetByUserIdAsync(userId.ToString());
        var ticketDtos = new List<UserTicketDto>();

        foreach (var ticket in tickets)
        {
            var eventItem = await _eventItemRepository.GetByDetailByIdAsync(ticket.EventItemId);
            if (eventItem == null) continue;

            var ticketClass = await _ticketClassRepository.GetByIdAsync(ticket.TicketClassId);
            if (ticketClass == null) continue;

            var eventEntity = eventItem.Event;
            if (eventEntity == null) continue;

            var status = eventItem.End < DateTime.UtcNow ? "EXPIRED" : "UPCOMING";

            ticketDtos.Add(new UserTicketDto
            {
                TicketId = ticket.Id,
                EventItemId = ticket.EventItemId,
                EventItemName = eventItem.Name,
                EventId = eventEntity.Id,
                EventName = eventEntity.Name,
                TicketClassName = ticketClass.Name,
                Price = ticketClass.Price,
                SeatLabel = "", 
                PurchaseDate = ticket.CreatedAt,
                EventStartDate = eventItem.Start,
                EventEndDate = eventItem.End,
                Status = status,
                TicketCode = ticket.TicketCode ?? string.Empty,
                TicketStatus = ticket.Status.ToString()
            });
        }

        return ticketDtos.OrderByDescending(t => t.PurchaseDate).ToList();
    }

    public async Task<List<PayoutDto>> GetPendingPayoutsAsync()
    {
        var payouts = await _payoutRepository.GetPendingPayoutsAsync();
        return payouts.Select(p => MapToPayoutDto(p)).ToList();
    }

    public async Task<PayoutDto?> GetPayoutByEventItemIdAsync(Guid eventItemId)
    {
        var payout = await _payoutRepository.GetByEventItemIdAsync(eventItemId);
        return payout != null ? MapToPayoutDto(payout) : null;
    }

    public async Task<PayoutDto> UpdatePayoutAsync(Guid payoutId, UpdatePayoutDto request, string adminUserId, Stream? proofImageStream = null, string? proofImageFileName = null)
    {
        var payout = await _payoutRepository.GetByIdAsync(payoutId);
        if (payout == null)
        {
            throw new KeyNotFoundException($"Payout {payoutId} not found");
        }

        if (proofImageStream != null && !string.IsNullOrEmpty(proofImageFileName))
        {
            var imageUrl = await _cloudStorageService.UploadAsync(proofImageStream, proofImageFileName);
            if (!string.IsNullOrEmpty(imageUrl))
            {
                payout.ProofImageUrl = imageUrl;
            }
        }
        else if (!string.IsNullOrEmpty(request.ProofImageUrl))
        {
            payout.ProofImageUrl = request.ProofImageUrl;
        }

        if (!string.IsNullOrEmpty(request.Notes))
        {
            payout.Notes = request.Notes;
        }

        payout.Status = PayoutStatus.PAID;
        payout.PaidAt = DateTime.UtcNow;
        payout.PaidByUserId = adminUserId;

        payout = await _payoutRepository.UpdateAsync(payout);
        return MapToPayoutDto(payout);
    }

    public async Task<PaginatedResult<PayoutDto>> GetPayoutsByFiltersAsync(PayoutStatus? status, int? month, int? year, int page = 1, int size = 10)
    {
        var allPayouts = await _payoutRepository.GetPayoutsByFiltersAsync(status, month, year);
        var total = allPayouts.Count();
        var payouts = allPayouts
            .Skip((page - 1) * size)
            .Take(size)
            .ToList();
        
        var payoutDtos = payouts.Select(p => MapToPayoutDto(p)).ToList();
        
        return new PaginatedResult<PayoutDto>
        {
            Data = payoutDtos,
            Total = total,
            Page = page,
            Size = size
        };
    }

    public async Task<List<PayoutDto>> GetPayoutHistoryByOrganizationIdAsync(Guid organizationId)
    {
        var payouts = await _payoutRepository.GetByOrganizationIdAsync(organizationId);
        return payouts.Select(p => MapToPayoutDto(p)).ToList();
    }

    public async Task<int> GetPendingPayoutEventsCountAsync(int? month = null, int? year = null)
    {
        var pendingPayouts = await _payoutRepository.GetPayoutsByFiltersAsync(PayoutStatus.PENDING, month, year);
        var distinctEventIds = pendingPayouts.Select(p => p.EventId).Distinct();
        return distinctEventIds.Count();
    }

    private PayoutDto MapToPayoutDto(Payout payout)
    {
        return new PayoutDto
        {
            Id = payout.Id,
            EventId = payout.EventId,
            EventItemId = payout.EventItemId,
            OrganizationId = payout.OrganizationId,
            EventName = payout.Event?.Name ?? "",
            EventItemName = payout.EventItem?.Name ?? "",
            OrganizationName = payout.Organization?.Name ?? "",
            GrossRevenue = payout.GrossRevenue,
            PlatformFee = payout.PlatformFee,
            OrgAmount = payout.OrgAmount,
            Status = payout.Status.ToString(),
            ProofImageUrl = payout.ProofImageUrl,
            Notes = payout.Notes,
            PaidAt = payout.PaidAt,
            PaidByUserId = payout.PaidByUserId,
            CreatedAt = payout.CreatedAt,
            BankCode = payout.Event?.BankCode,
            AccountNumber = payout.Event?.AccountNumber,
            AccountName = payout.Event?.AccountName
        };
    }
}

