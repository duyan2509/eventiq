using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventiq.Api.Controllers;

[Authorize]
public class PaymentController : BaseController
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpPost("create-url")]
    public async Task<ActionResult<PaymentUrlResponse>> CreatePaymentUrl([FromBody] CreatePaymentUrlRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var returnUrl = string.IsNullOrEmpty(request.ReturnUrl) 
                ? $"{baseUrl}/payment/return" 
                : request.ReturnUrl;
            var ipnUrl = $"{baseUrl}/api/payment/ipn";

            var result = await _paymentService.CreatePaymentUrlAsync(
                request.CheckoutId, 
                userId, 
                returnUrl, 
                ipnUrl);
            
            return Ok(result);
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
            _logger.LogError(ex, "Error creating payment URL");
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [AcceptVerbs("GET", "POST")]
    [Route("ipn")]
    public async Task<IActionResult> IpnCallback()
    {
        try
        {
            _logger.LogInformation("VNPAY IPN callback received - Method: {Method}, ContentType: {ContentType}, QueryString: {QueryString}", 
                Request.Method, Request.ContentType, Request.QueryString);

          
            var queryParams = new Dictionary<string, string>();
            
            foreach (var kvp in Request.Query)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    queryParams[kvp.Key] = kvp.Value.ToString();
                }
            }
            
            if (Request.HasFormContentType)
            {
                foreach (var kvp in Request.Form)
                {
                    if (!string.IsNullOrEmpty(kvp.Value))
                    {
                        queryParams[kvp.Key] = kvp.Value.ToString();
                    }
                }
            }

            if (queryParams.Count == 0)
            {
                _logger.LogWarning("VNPAY IPN callback received with no parameters");
                return BadRequest(new { RspCode = "99", Message = "No parameters received" });
            }

            _logger.LogInformation("VNPAY IPN callback - Processing {Count} parameters: {Parameters}", 
                queryParams.Count, string.Join(", ", queryParams.Keys));

            var success = await _paymentService.HandleIpnCallbackAsync(queryParams);
            
            if (success)
            {
                _logger.LogInformation("VNPAY IPN callback processed successfully");
                return Ok(new { RspCode = "00", Message = "Confirm Success" });
            }
            else
            {
                _logger.LogWarning("VNPAY IPN callback processing failed - checksum or validation failed");
                return BadRequest(new { RspCode = "97", Message = "Checksum failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VNPAY IPN callback - Exception: {ExceptionMessage}", ex.Message);
            return BadRequest(new { RspCode = "99", Message = $"Unknown error: {ex.Message}" });
        }
    }


    [HttpGet("{paymentId}")]
    public async Task<ActionResult<PaymentDto>> GetPayment([FromRoute] Guid paymentId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var payment = await _paymentService.GetPaymentAsync(paymentId, userId);
            if (payment == null)
            {
                return NotFound(new { message = "Payment not found" });
            }

            return Ok(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment");
            return BadRequest(new { message = ex.Message });
        }
    }
}

