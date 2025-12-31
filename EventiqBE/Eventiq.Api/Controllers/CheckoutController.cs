using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventiq.Api.Controllers;

[Authorize]
public class CheckoutController : BaseController
{
    private readonly ICheckoutService _checkoutService;

    public CheckoutController(ICheckoutService checkoutService)
    {
        _checkoutService = checkoutService;
    }


    [HttpPost]
    public async Task<ActionResult<CheckoutDto>> CreateCheckout([FromBody] CreateCheckoutRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var checkout = await _checkoutService.CreateCheckoutAsync(userId, request.EventItemId, request.SeatIds);
            return Ok(checkout);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    [HttpPost("{checkoutId}/confirm")]
    public async Task<ActionResult<CheckoutDto>> ConfirmCheckout([FromRoute] Guid checkoutId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var checkout = await _checkoutService.ConfirmCheckoutAsync(checkoutId, userId);
            return Ok(checkout);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    [HttpPost("{checkoutId}/cancel")]
    public async Task<ActionResult> CancelCheckout([FromRoute] Guid checkoutId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var success = await _checkoutService.CancelCheckoutAsync(checkoutId, userId);
            if (success)
            {
                return Ok(new { message = "Checkout canceled successfully" });
            }
            return BadRequest(new { message = "Failed to cancel checkout" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{checkoutId}")]
    public async Task<ActionResult<CheckoutDto>> GetCheckout([FromRoute] Guid checkoutId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var checkout = await _checkoutService.GetCheckoutAsync(checkoutId, userId);
            if (checkout == null)
            {
                return NotFound(new { message = "Checkout not found" });
            }
            return Ok(checkout);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

