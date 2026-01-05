using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces;
using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Application.Interfaces.Services;
using Eventiq.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Eventiq.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ICheckoutRepository _checkoutRepository;
    private readonly IEventItemRepository _eventItemRepository;
    private readonly ITicketClassRepository _ticketClassRepository;
    private readonly IVnPayService _vnPayService;
    private readonly ISeatService _seatService;
    private readonly ITicketRepository _ticketRepository;
    private readonly IEventSeatRepository _eventSeatRepository;
    private readonly IEventSeatStateRepository _eventSeatStateRepository;
    private readonly IRedisService _redisService;
    private readonly IPayoutRepository _payoutRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        ICheckoutRepository checkoutRepository,
        IEventItemRepository eventItemRepository,
        ITicketClassRepository ticketClassRepository,
        IVnPayService vnPayService,
        ISeatService seatService,
        ITicketRepository ticketRepository,
        IEventSeatRepository eventSeatRepository,
        IEventSeatStateRepository eventSeatStateRepository,
        IRedisService redisService,
        IPayoutRepository payoutRepository,
        IUnitOfWork unitOfWork,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _checkoutRepository = checkoutRepository;
        _eventItemRepository = eventItemRepository;
        _ticketClassRepository = ticketClassRepository;
        _vnPayService = vnPayService;
        _seatService = seatService;
        _ticketRepository = ticketRepository;
        _eventSeatRepository = eventSeatRepository;
        _eventSeatStateRepository = eventSeatStateRepository;
        _redisService = redisService;
        _payoutRepository = payoutRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<PaymentUrlResponse> CreatePaymentUrlAsync(Guid checkoutId, Guid userId, string returnUrl, string ipnUrl)
    {
        _logger.LogInformation("Creating payment URL for checkout {CheckoutId}", checkoutId);

        var checkout = await _checkoutRepository.GetByCheckoutIdWithEventItemAsync(checkoutId);
        if (checkout == null)
        {
            throw new KeyNotFoundException($"Checkout {checkoutId} not found");
        }

        if (checkout.UserId != userId)
        {
            throw new UnauthorizedAccessException("Checkout does not belong to this user");
        }

        if (checkout.Status != "INIT")
        {
            throw new InvalidOperationException($"Checkout is already {checkout.Status}");
        }

        var existingPayment = await _paymentRepository.GetByCheckoutIdAsync(checkoutId);
        if (existingPayment != null && existingPayment.Status == PaymentStatus.SUCCESS)
        {
            throw new InvalidOperationException("Payment already completed");
        }

        var eventItem = checkout.EventItem;
        if (eventItem == null)
        {
            throw new InvalidOperationException("EventItem not found");
        }

        var ticketClasses = await _ticketRepository.GetTicketClassesByEventItemIdAsync(eventItem.Id);
        var seatIds = checkout.GetSeatIds();
        
        if (!ticketClasses.Any())
        {
            throw new InvalidOperationException("No ticket class found for event item");
        }

        var seats = await _eventSeatRepository.GetSeatsByLabelsAsync(eventItem.ChartId, seatIds);
        if (seats.Count() != seatIds.Count)
        {
            throw new InvalidOperationException($"Some seats not found. Expected {seatIds.Count}, found {seats.Count()}");
        }

        decimal grossAmount = 0;
        foreach (var seat in seats)
        {
            TicketClass? matchingTicketClass = null;
            
            if (!string.IsNullOrEmpty(seat.CategoryKey))
            {
                matchingTicketClass = ticketClasses.FirstOrDefault(tc => tc.Name == seat.CategoryKey);
            }
            
            if (matchingTicketClass == null)
            {
                matchingTicketClass = ticketClasses.FirstOrDefault();
            }
            
            if (matchingTicketClass == null)
            {
                throw new InvalidOperationException($"No ticket class found for seat {seat.Label}");
            }
            
            grossAmount += matchingTicketClass.Price;
        }
        var platformFee = grossAmount * 0.20m; // 20%
        var orgAmount = grossAmount * 0.80m; // 80%

        // Generate payment ID
        var paymentId = $"EVT{checkoutId.ToString("N").Substring(0, 12)}{DateTime.UtcNow:yyyyMMddHHmmss}";

        Payment payment;
        if (existingPayment != null)
        {
            payment = existingPayment;
            payment.PaymentId = paymentId;
            payment.GrossAmount = grossAmount;
            payment.PlatformFee = platformFee;
            payment.OrgAmount = orgAmount;
            payment.Status = PaymentStatus.PENDING;
            payment = await _paymentRepository.UpdateAsync(payment);
        }
        else
        {
            payment = new Payment
            {
                CheckoutId = checkoutId,
                UserId = userId,
                EventItemId = eventItem.Id,
                PaymentId = paymentId,
                GrossAmount = grossAmount,
                PlatformFee = platformFee,
                OrgAmount = orgAmount,
                Status = PaymentStatus.PENDING
            };
            payment = await _paymentRepository.AddAsync(payment);
        }

        var paymentLockTtl = TimeSpan.FromMinutes(15);
        await _redisService.ExtendSeatLockAsync(checkout.EventItemId, seatIds, paymentLockTtl);
        await _redisService.ExtendCheckoutSessionAsync(checkout.Id.ToString(), paymentLockTtl);
        
        var orderInfo = $"{eventItem.Name}";
        var paymentUrl = _vnPayService.CreatePaymentUrl(paymentId, grossAmount, orderInfo, returnUrl, ipnUrl);

        _logger.LogInformation("Created payment URL for payment {PaymentId}", paymentId);

        return new PaymentUrlResponse
        {
            PaymentUrl = paymentUrl,
            PaymentId = paymentId
        };
    }

    public async Task<bool> HandleIpnCallbackAsync(Dictionary<string, string> vnpayData)
    {
        _logger.LogInformation("Handling VNPAY IPN callback");

        // Parse IPN data
        var ipnResult = _vnPayService.ParseIpnCallback(vnpayData);

        if (string.IsNullOrEmpty(ipnResult.SecureHash))
        {
            _logger.LogWarning("VNPAY IPN callback missing secure hash");
            return false;
        }

        // Verify checksum
        var isValid = _vnPayService.VerifyIpnCallback(vnpayData, ipnResult.SecureHash);
        if (!isValid)
        {
            _logger.LogWarning("VNPAY IPN callback checksum verification failed");
            return false;
        }

        // Find payment by payment ID
        var payment = await _paymentRepository.GetByPaymentIdAsync(ipnResult.PaymentId);
        if (payment == null)
        {
            _logger.LogWarning("Payment not found: {PaymentId}", ipnResult.PaymentId);
            return false;
        }

        if (payment.Status == PaymentStatus.SUCCESS && payment.IsVerified)
        {
            _logger.LogInformation("Payment already processed: {PaymentId}", ipnResult.PaymentId);
            return true;
        }

        if (payment.GrossAmount != ipnResult.Amount)
        {
            _logger.LogWarning("Amount mismatch. Expected: {Expected}, Received: {Received}", 
                payment.GrossAmount, ipnResult.Amount);
            return false;
        }

        if (ipnResult.IsSuccess)
        {
            try
            {
                var checkout = await _checkoutRepository.GetByCheckoutIdWithEventItemAsync(payment.CheckoutId);
                if (checkout == null)
                {
                    _logger.LogError("Checkout not found: {CheckoutId}", payment.CheckoutId);
                    return false;
                }

                if (checkout.Status == "PAID")
                {
                    _logger.LogInformation("Checkout already paid: {CheckoutId}", payment.CheckoutId);
                    return true;
                }

                var seatIds = checkout.GetSeatIds();
                var eventItem = checkout.EventItem;
                if (eventItem == null || string.IsNullOrEmpty(eventItem.EventKey))
                {
                    _logger.LogError("EventItem or EventKey not found");
                    return false;
                }

                // Book seats on Seats.io
                await _seatService.BookSeatsAsync(eventItem.EventKey, seatIds);
                _logger.LogInformation("Booked seats on Seats.io for checkout {CheckoutId}", payment.CheckoutId);

                var ticketClasses = await _ticketRepository.GetTicketClassesByEventItemIdAsync(eventItem.Id);
                var tickets = new List<Ticket>();
                var seatTicketMap = new Dictionary<Guid, Ticket>();

                foreach (var seatLabel in seatIds)
                {
                    var seat = await _eventSeatRepository.GetSeatByLabelAsync(eventItem.ChartId, seatLabel);
                    if (seat == null)
                    {
                        _logger.LogWarning("Seat not found: {SeatLabel} for checkout {CheckoutId}", seatLabel, payment.CheckoutId);
                        continue;
                    }

                    TicketClass? matchingTicketClass = null;
                    
                    if (!string.IsNullOrEmpty(seat.CategoryKey))
                    {
                        matchingTicketClass = ticketClasses.FirstOrDefault(tc => tc.Name == seat.CategoryKey);
                    }
                    
                    if (matchingTicketClass == null)
                    {
                        matchingTicketClass = ticketClasses.FirstOrDefault();
                    }

                    if (matchingTicketClass == null)
                    {
                        _logger.LogWarning("No ticket class found for seat {SeatLabel} in checkout {CheckoutId}", seatLabel, payment.CheckoutId);
                        continue;
                    }

                    var ticket = new Ticket
                    {
                        TicketClassId = matchingTicketClass.Id,
                        EventItemId = eventItem.Id,
                        UserId = payment.UserId.ToString()
                    };
                    tickets.Add(ticket);
                    seatTicketMap[seat.Id] = ticket;
                }

                if (tickets.Count > 0)
                {
                    await _ticketRepository.AddRangeAsync(tickets);
                    _logger.LogInformation("Created {Count} tickets for payment {PaymentId}", tickets.Count, ipnResult.PaymentId);
                    
                    foreach (var kvp in seatTicketMap)
                    {
                        var seatId = kvp.Key;
                        var ticket = kvp.Value;
                        
                        var seatState = await _eventSeatStateRepository.GetByEventItemAndSeatAsync(eventItem.Id, seatId);
                        if (seatState != null)
                        {
                            seatState.Status = SeatStatus.Paid;
                            seatState.TicketId = ticket.Id;
                            seatState.UpdatedAt = DateTime.UtcNow;
                            await _eventSeatStateRepository.UpdateAsync(seatState);
                            _logger.LogInformation("Updated EventSeatState for seat {SeatId} to Paid with ticket {TicketId}", seatId, ticket.Id);
                        }
                        else
                        {
                            _logger.LogWarning("EventSeatState not found for eventItem {EventItemId} and seat {SeatId}", eventItem.Id, seatId);
                        }
                    }
                }

                // Update payment
                payment.Status = PaymentStatus.SUCCESS;
                payment.VnpTransactionNo = ipnResult.TransactionNo;
                payment.VnpResponseCode = ipnResult.ResponseCode;
                payment.VnpSecureHash = ipnResult.SecureHash;
                payment.BankCode = ipnResult.BankCode;
                payment.CardType = ipnResult.CardType;
                // Ensure DateTime is UTC for PostgreSQL
                payment.PaidAt = ipnResult.PayDate.HasValue 
                    ? (ipnResult.PayDate.Value.Kind == DateTimeKind.Utc 
                        ? ipnResult.PayDate.Value 
                        : ipnResult.PayDate.Value.ToUniversalTime())
                    : DateTime.UtcNow;
                payment.IsVerified = true;
                payment.VerifiedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);

                // Update checkout
                checkout.Status = "PAID";
                await _checkoutRepository.UpdateAsync(checkout);

                await _redisService.ReleaseSeatsAsync(checkout.EventItemId, seatIds);
                await _redisService.DeleteCheckoutSessionAsync(checkout.Id.ToString());

                await CreateOrUpdatePayoutAsync(eventItem, payment);

                _logger.LogInformation("Payment processed successfully: {PaymentId}", ipnResult.PaymentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment: {PaymentId}", ipnResult.PaymentId);
                
                payment.Status = PaymentStatus.FAILED;
                await _paymentRepository.UpdateAsync(payment);
                
                return false;
            }
        }
        else
        {
            // Payment failed
            payment.Status = PaymentStatus.FAILED;
            payment.VnpResponseCode = ipnResult.ResponseCode;
            await _paymentRepository.UpdateAsync(payment);
            
            _logger.LogWarning("Payment failed: {PaymentId}, ResponseCode: {ResponseCode}", 
                ipnResult.PaymentId, ipnResult.ResponseCode);
            
            return false;
        }
    }

    private async Task CreateOrUpdatePayoutAsync(EventItem eventItem, Payment payment)
    {
        var existingPayout = await _payoutRepository.GetByEventItemIdAsync(eventItem.Id);

        if (existingPayout != null)
        {
            existingPayout.GrossRevenue += payment.GrossAmount;
            existingPayout.PlatformFee += payment.PlatformFee;
            existingPayout.OrgAmount += payment.OrgAmount;
            await _payoutRepository.UpdateAsync(existingPayout);
        }
        else
        {
            var eventEntity = eventItem.Event;
            if (eventEntity != null)
            {
                var payout = new Payout
                {
                    EventId = eventEntity.Id,
                    EventItemId = eventItem.Id,
                    OrganizationId = eventEntity.OrganizationId,
                    GrossRevenue = payment.GrossAmount,
                    PlatformFee = payment.PlatformFee,
                    OrgAmount = payment.OrgAmount,
                    Status = PayoutStatus.PENDING
                };
                await _payoutRepository.AddAsync(payout);
            }
        }
    }

    public async Task<PaymentDto?> GetPaymentAsync(Guid paymentId, Guid userId)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment == null || payment.UserId != userId)
        {
            return null;
        }

        return new PaymentDto
        {
            Id = payment.Id,
            CheckoutId = payment.CheckoutId,
            UserId = payment.UserId,
            EventItemId = payment.EventItemId,
            PaymentId = payment.PaymentId,
            VnpTransactionNo = payment.VnpTransactionNo,
            VnpResponseCode = payment.VnpResponseCode,
            GrossAmount = payment.GrossAmount,
            PlatformFee = payment.PlatformFee,
            OrgAmount = payment.OrgAmount,
            Status = payment.Status.ToString(),
            PaidAt = payment.PaidAt,
            BankCode = payment.BankCode,
            CardType = payment.CardType,
            IsVerified = payment.IsVerified,
            CreatedAt = payment.CreatedAt
        };
    }
}

