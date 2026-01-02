using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces;
using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Application.Interfaces.Services;
using Eventiq.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Eventiq.Application.Services;

public class CheckoutService : ICheckoutService
{
    private readonly ICheckoutRepository _checkoutRepository;
    private readonly IEventItemRepository _eventItemRepository;
    private readonly IEventSeatRepository _eventSeatRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IRedisService _redisService;
    private readonly ISeatService _seatService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(
        ICheckoutRepository checkoutRepository,
        IEventItemRepository eventItemRepository,
        IEventSeatRepository eventSeatRepository,
        ITicketRepository ticketRepository,
        IRedisService redisService,
        ISeatService seatService,
        IUnitOfWork unitOfWork,
        ILogger<CheckoutService> logger)
    {
        _checkoutRepository = checkoutRepository;
        _eventItemRepository = eventItemRepository;
        _eventSeatRepository = eventSeatRepository;
        _ticketRepository = ticketRepository;
        _redisService = redisService;
        _seatService = seatService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CheckoutDto> CreateCheckoutAsync(Guid userId, Guid eventItemId, List<string> seatIds)
    {
        _logger.LogInformation("Creating checkout for user {UserId}, eventItem {EventItemId}, seats {SeatIds}", 
            userId, eventItemId, string.Join(",", seatIds));

        // Validate eventItem and seatIds exist in DB, check seat status
        var eventItem = await _eventItemRepository.GetByDetailByIdAsync(eventItemId);
        if (eventItem == null)
        {
            throw new KeyNotFoundException($"EventItem {eventItemId} not found");
        }

        if (string.IsNullOrEmpty(eventItem.EventKey))
        {
            throw new InvalidOperationException($"EventItem {eventItemId} does not have Seats.io event key");
        }

        // Validate seats exist - 
        var seats = await _eventSeatRepository.GetSeatsByLabelsAsync(eventItem.ChartId, seatIds);
        if (seats.Count() != seatIds.Count)
        {
            throw new InvalidOperationException($"Some seats not found. Expected {seatIds.Count}, found {seats.Count()}. SeatIds: {string.Join(", ", seatIds)}");
        }

        var lockTtl = TimeSpan.FromMinutes(5);
        var lockSuccess = await _redisService.LockSeatsAsync(eventItemId, seatIds, lockTtl);
        
        if (!lockSuccess)
        {
            _logger.LogWarning("Failed to lock seats - some seats may already be locked");
            throw new InvalidOperationException("Some seats are already locked by another user");
        }

        try
        {
            // Create checkout session in DB
            var checkout = new Checkout
            {
                UserId = userId,
                EventItemId = eventItemId,
                Status = "INIT",
                EventKey = eventItem.EventKey
            };
            checkout.SetSeatIds(seatIds);

            checkout = await _checkoutRepository.AddAsync(checkout);
            _logger.LogInformation("Created checkout {CheckoutId}", checkout.Id);

            // Create hold token , reserve seats on Seats.io
            const int holdTokenExpiresInMinutes = 15 ;
            var holdTokenDto = await _seatService.CreateHoldTokenAsync(holdTokenExpiresInMinutes);
            checkout.HoldToken = holdTokenDto.HoldToken;
            checkout.HoldTokenExpiresAt = holdTokenDto.ExpiresAt;
            await _checkoutRepository.UpdateAsync(checkout);

            // Reserve seats on Seats.io
            try
            {
                await _seatService.HoldSeatsAsync(eventItem.EventKey, seatIds, holdTokenDto.HoldToken);
                _logger.LogInformation("Reserved seats on Seats.io for checkout {CheckoutId}", checkout.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reserve seats on Seats.io, rolling back Redis locks");
                // Rollback Redis locks
                await _redisService.ReleaseSeatsAsync(eventItemId, seatIds);
                throw new Exception($"Failed to reserve seats on Seats.io: {ex.Message}", ex);
            }

            // Save checkout session to Redis
            var checkoutData = System.Text.Json.JsonSerializer.Serialize(new
            {
                checkoutId = checkout.Id,
                userId = userId,
                eventItemId = eventItemId,
                seatIds = seatIds
            });
            await _redisService.SetCheckoutSessionAsync(checkout.Id.ToString(), checkoutData, lockTtl);

            return new CheckoutDto
            {
                Id = checkout.Id,
                UserId = checkout.UserId,
                EventItemId = checkout.EventItemId,
                Status = checkout.Status,
                SeatIds = checkout.GetSeatIds(),
                HoldToken = checkout.HoldToken,
                HoldTokenExpiresAt = checkout.HoldTokenExpiresAt,
                EventKey = checkout.EventKey,
                CreatedAt = checkout.CreatedAt
            };
        }
        catch (Exception ex)
        {
            // Rollback Redis locks on any error
            await _redisService.ReleaseSeatsAsync(eventItemId, seatIds);
            _logger.LogError(ex, "Error creating checkout, released Redis locks");
            throw;
        }
    }

    public async Task<CheckoutDto> ConfirmCheckoutAsync(Guid checkoutId, Guid userId)
    {
        _logger.LogInformation("Confirming checkout {CheckoutId} for user {UserId}", checkoutId, userId);

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

        // Verify checkout is still valid (check Redis)
        var checkoutData = await _redisService.GetCheckoutSessionAsync(checkoutId.ToString());
        if (checkoutData == null)
        {
            throw new InvalidOperationException("Checkout session has expired");
        }

        var seatIds = checkout.GetSeatIds();
        if (seatIds.Count == 0)
        {
            throw new InvalidOperationException("No seats in checkout");
        }

        // Verify seats are still locked by this user
        foreach (var seatId in seatIds)
        {
            var isLocked = await _redisService.IsSeatLockedAsync(checkout.EventItemId, seatId);
            if (!isLocked)
            {
                throw new InvalidOperationException($"Seat {seatId} is no longer locked");
            }
        }

        try
        {
            // Book seats on Seats.io
            await _seatService.BookSeatsAsync(checkout.EventKey!, seatIds);
            _logger.LogInformation("Booked seats on Seats.io for checkout {CheckoutId}", checkoutId);

            var eventItem = checkout.EventItem;
            var tickets = new List<Ticket>();
            
            var ticketClasses = await _ticketRepository.GetTicketClassesByEventItemIdAsync(eventItem.Id);
            
            foreach (var seatLabel in seatIds)
            {
                // Find seat by Label 
                var seat = await _eventSeatRepository.GetSeatByLabelAsync(eventItem.ChartId, seatLabel);
                var ticketClass = ticketClasses.FirstOrDefault(); 
                
                if (ticketClass != null && seat != null)
                {
                    var ticket = new Ticket
                    {
                        TicketClassId = ticketClass.Id,
                        EventItemId = eventItem.Id,
                        UserId = userId.ToString(),
                        TicketCode = GenerateTicketCode()
                    };
                    tickets.Add(ticket);
                }
            }

            if (tickets.Count > 0)
            {
                await _ticketRepository.AddRangeAsync(tickets);
                _logger.LogInformation("Created {Count} tickets for checkout {CheckoutId}", tickets.Count, checkoutId);
            }

            checkout.Status = "SUCCESS";
            checkout = await _checkoutRepository.UpdateAsync(checkout);

            await _redisService.ReleaseSeatsAsync(checkout.EventItemId, seatIds);
            
            await _redisService.DeleteCheckoutSessionAsync(checkoutId.ToString());

            return new CheckoutDto
            {
                Id = checkout.Id,
                UserId = checkout.UserId,
                EventItemId = checkout.EventItemId,
                Status = checkout.Status,
                SeatIds = checkout.GetSeatIds(),
                HoldToken = checkout.HoldToken,
                HoldTokenExpiresAt = checkout.HoldTokenExpiresAt,
                EventKey = checkout.EventKey,
                CreatedAt = checkout.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming checkout {CheckoutId}", checkoutId);
            throw;
        }
    }

    public async Task<bool> CancelCheckoutAsync(Guid checkoutId, Guid userId)
    {
        _logger.LogInformation("Canceling checkout {CheckoutId} for user {UserId}", checkoutId, userId);

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
            _logger.LogWarning("Checkout {CheckoutId} is already {Status}, cannot cancel", checkoutId, checkout.Status);
            return false;
        }

        var seatIds = checkout.GetSeatIds();

        try
        {
            if (!string.IsNullOrEmpty(checkout.EventKey) && seatIds.Count > 0)
            {
                await _seatService.ReleaseSeatsAsync(checkout.EventKey, seatIds);
                _logger.LogInformation("Released seats on Seats.io for checkout {CheckoutId}", checkoutId);
            }

            await _redisService.ReleaseSeatsAsync(checkout.EventItemId, seatIds);

            checkout.Status = "CANCELED";
            await _checkoutRepository.UpdateAsync(checkout);

            await _redisService.DeleteCheckoutSessionAsync(checkoutId.ToString());

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling checkout {CheckoutId}", checkoutId);
            throw;
        }
    }

    public async Task<CheckoutDto?> GetCheckoutAsync(Guid checkoutId, Guid userId)
    {
        var checkout = await _checkoutRepository.GetByCheckoutIdWithEventItemAsync(checkoutId);
        if (checkout == null || checkout.UserId != userId)
        {
            return null;
        }

        return new CheckoutDto
        {
            Id = checkout.Id,
            UserId = checkout.UserId,
            EventItemId = checkout.EventItemId,
            Status = checkout.Status,
            SeatIds = checkout.GetSeatIds(),
            HoldToken = checkout.HoldToken,
            HoldTokenExpiresAt = checkout.HoldTokenExpiresAt,
            EventKey = checkout.EventKey,
            CreatedAt = checkout.CreatedAt
        };
    }

    private string GenerateTicketCode()
    {
        var guid = Guid.NewGuid().ToString("N").ToUpper();
        return guid.Substring(0, 8);
    }
}

