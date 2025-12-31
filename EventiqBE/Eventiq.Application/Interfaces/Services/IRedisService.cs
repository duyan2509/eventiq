namespace Eventiq.Application.Interfaces.Services;

public interface IRedisService
{
    Task<bool> LockSeatsAsync(Guid eventItemId, List<string> seatIds, TimeSpan ttl);
    Task ReleaseSeatsAsync(Guid eventItemId, List<string> seatIds);
    Task<bool> IsSeatLockedAsync(Guid eventItemId, string seatId);
    Task SetCheckoutSessionAsync(string checkoutId, string data, TimeSpan ttl);
    Task<string?> GetCheckoutSessionAsync(string checkoutId);
    Task DeleteCheckoutSessionAsync(string checkoutId);
}

