using Eventiq.Application.Dtos;

namespace Eventiq.Application.Interfaces.Services;

public interface ICheckoutService
{
    /// <summary>
    /// Create checkout session: lock seats, create checkout, reserve on Seats.io
    /// </summary>
    Task<CheckoutDto> CreateCheckoutAsync(Guid userId, Guid eventItemId, List<string> seatIds);
    
    /// <summary>
    /// Confirm payment: book seats, save tickets, release locks
    /// </summary>
    Task<CheckoutDto> ConfirmCheckoutAsync(Guid checkoutId, Guid userId);
    
    /// <summary>
    /// Cancel checkout: release locks and Seats.io holds
    /// </summary>
    Task<bool> CancelCheckoutAsync(Guid checkoutId, Guid userId);
    
    /// <summary>
    /// Get checkout by ID
    /// </summary>
    Task<CheckoutDto?> GetCheckoutAsync(Guid checkoutId, Guid userId);
}

