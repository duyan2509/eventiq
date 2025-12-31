using Eventiq.Api.Request;
using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Services;
using Eventiq.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventiq.Api.Controllers;

[Authorize]
public class PayoutController : BaseController
{
    private readonly IRevenueService _revenueService;
    private readonly ILogger<PayoutController> _logger;

    public PayoutController(IRevenueService revenueService, ILogger<PayoutController> logger)
    {
        _revenueService = revenueService;
        _logger = logger;
    }


    [HttpGet("pending")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<List<PayoutDto>>> GetPendingPayouts()
    {
        try
        {
            var payouts = await _revenueService.GetPendingPayoutsAsync();
            return Ok(payouts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending payouts");
            return BadRequest(new { message = ex.Message });
        }
    }


    [HttpGet("event-item/{eventItemId}")]
    public async Task<ActionResult<PayoutDto>> GetPayoutByEventItemId([FromRoute] Guid eventItemId)
    {
        try
        {
            var payout = await _revenueService.GetPayoutByEventItemIdAsync(eventItemId);
            if (payout == null)
            {
                return NotFound(new { message = "Payout not found" });
            }
            return Ok(payout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payout");
            return BadRequest(new { message = ex.Message });
        }
    }


    [HttpPut("{payoutId}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<PayoutDto>> UpdatePayout([FromRoute] Guid payoutId, [FromForm] UpdatePayoutRequest request)
    {
        try
        {
            var adminUserId = GetCurrentUserId().ToString();
            Stream? proofImageStream = null;
            string? proofImageFileName = null;

            if (request.ProofImage != null)
            {
                var memStream = new MemoryStream();
                await request.ProofImage.CopyToAsync(memStream);
                memStream.Position = 0;
                proofImageStream = memStream;
                proofImageFileName = request.ProofImage.FileName;
            }

            var updateDto = new UpdatePayoutDto
            {
                ProofImageUrl = request.ProofImageUrl,
                Notes = request.Notes
            };

            var payout = await _revenueService.UpdatePayoutAsync(payoutId, updateDto, adminUserId, proofImageStream, proofImageFileName);
            return Ok(payout);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payout");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<PaginatedResult<PayoutDto>>> GetPayouts(
        [FromQuery] string? status,
        [FromQuery] int? month,
        [FromQuery] int? year,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        try
        {
            PayoutStatus? payoutStatus = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<PayoutStatus>(status, true, out var parsedStatus))
            {
                payoutStatus = parsedStatus;
            }

            var result = await _revenueService.GetPayoutsByFiltersAsync(payoutStatus, month, year, page, size);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payouts");
            return BadRequest(new { message = ex.Message });
        }
    }


    [HttpGet("organization/{organizationId}/history")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<List<PayoutDto>>> GetPayoutHistoryByOrganization([FromRoute] Guid organizationId)
    {
        try
        {
            var payouts = await _revenueService.GetPayoutHistoryByOrganizationIdAsync(organizationId);
            return Ok(payouts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payout history");
            return BadRequest(new { message = ex.Message });
        }
    }


    [HttpGet("pending-events-count")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<int>> GetPendingPayoutEventsCount(
        [FromQuery] int? month,
        [FromQuery] int? year)
    {
        try
        {
            var count = await _revenueService.GetPendingPayoutEventsCountAsync(month, year);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending payout events count");
            return BadRequest(new { message = ex.Message });
        }
    }
}

