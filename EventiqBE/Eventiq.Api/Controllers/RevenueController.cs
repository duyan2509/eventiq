using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventiq.Api.Controllers;

[Authorize]
public class RevenueController : BaseController
{
    private readonly IRevenueService _revenueService;
    private readonly ILogger<RevenueController> _logger;

    public RevenueController(IRevenueService revenueService, ILogger<RevenueController> logger)
    {
        _revenueService = revenueService;
        _logger = logger;
    }


    [HttpGet("admin")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<AdminRevenueReportDto>> GetAdminRevenueReport([FromQuery] int? month, [FromQuery] int? year)
    {
        try
        {
            var report = await _revenueService.GetAdminRevenueReportAsync(month, year);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin revenue report");
            return BadRequest(new { message = ex.Message });
        }
    }


    [HttpGet("org/{eventId}")]
    public async Task<ActionResult<OrgRevenueReportDto>> GetOrgRevenueReport(
        [FromRoute] Guid eventId,
        [FromQuery] Guid organizationId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            if (organizationId == Guid.Empty)
            {
                return BadRequest(new { message = "OrganizationId is required" });
            }
            
            var report = await _revenueService.GetOrgRevenueReportAsync(eventId, organizationId);
            return Ok(report);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting org revenue report");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get user tickets
    /// </summary>
    [HttpGet("user/tickets")]
    public async Task<ActionResult<List<UserTicketDto>>> GetUserTickets()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var tickets = await _revenueService.GetUserTicketsAsync(userId);
            return Ok(tickets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user tickets");
            return BadRequest(new { message = ex.Message });
        }
    }


    [HttpGet("org/{eventId}/stats")]
    public async Task<ActionResult<OrgRevenueStatsDto>> GetOrgRevenueStats(
        [FromRoute] Guid eventId,
        [FromQuery] Guid organizationId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            if (organizationId == Guid.Empty)
            {
                return BadRequest(new { message = "OrganizationId is required" });
            }

            var stats = await _revenueService.GetOrgRevenueStatsAsync(eventId, organizationId);
            return Ok(stats);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting org revenue stats");
            return BadRequest(new { message = ex.Message });
        }
    }


    [HttpGet("org/{eventId}/table")]
    public async Task<ActionResult<PaginatedResult<OrgRevenueTableDto>>> GetOrgRevenueTable(
        [FromRoute] Guid eventId,
        [FromQuery] Guid organizationId,
        [FromQuery] Guid? eventItemId = null,
        [FromQuery] Guid? ticketClassId = null,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            if (organizationId == Guid.Empty)
            {
                return BadRequest(new { message = "OrganizationId is required" });
            }

            var result = await _revenueService.GetOrgRevenueTableAsync(
                eventId, 
                organizationId, 
                eventItemId, 
                ticketClassId, 
                page, 
                size);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting org revenue table");
            return BadRequest(new { message = ex.Message });
        }
    }
}

