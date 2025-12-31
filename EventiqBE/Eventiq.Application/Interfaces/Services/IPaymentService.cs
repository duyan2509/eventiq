using Eventiq.Application.Dtos;

namespace Eventiq.Application.Interfaces.Services;

public interface IPaymentService
{
    /// <summary>
    /// Create payment and return VNPAY payment URL
    /// </summary>
    Task<PaymentUrlResponse> CreatePaymentUrlAsync(Guid checkoutId, Guid userId, string returnUrl, string ipnUrl);
    
    /// <summary>
    /// Handle VNPAY IPN callback
    /// </summary>
    Task<bool> HandleIpnCallbackAsync(Dictionary<string, string> vnpayData);
    
    /// <summary>
    /// Get payment by ID
    /// </summary>
    Task<PaymentDto?> GetPaymentAsync(Guid paymentId, Guid userId);
}

