namespace Eventiq.Application.Interfaces.Services;

public interface IRedisService
{
    Task<bool> LockSeatsAsync(Guid eventItemId, List<string> seatIds, TimeSpan ttl);
    Task ExtendSeatLockAsync(Guid eventItemId, List<string> seatIds, TimeSpan additionalTtl);
    Task ReleaseSeatsAsync(Guid eventItemId, List<string> seatIds);
    Task<bool> IsSeatLockedAsync(Guid eventItemId, string seatId);
    Task SetCheckoutSessionAsync(string checkoutId, string data, TimeSpan ttl);
    Task ExtendCheckoutSessionAsync(string checkoutId, TimeSpan additionalTtl);
    Task<string?> GetCheckoutSessionAsync(string checkoutId);
    Task DeleteCheckoutSessionAsync(string checkoutId);
}

