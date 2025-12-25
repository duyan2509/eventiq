using Eventiq.Api.Request;
using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Eventiq.Api.Hubs;

namespace Eventiq.Api.Controllers;

[Authorize]
public class StaffController : BaseController
{
    private readonly IStaffService _staffService;
    private readonly IHubContext<NotificationHub> _hubContext;

    public StaffController(IStaffService staffService, IHubContext<NotificationHub> hubContext)
    {
        _staffService = staffService;
        _hubContext = hubContext;
    }

    [HttpGet("events/{eventId}/staffs")]
    public async Task<ActionResult<StaffListDto>> GetEventStaffs([FromRoute] Guid eventId)
    {
        Console.WriteLine($"[DEBUG] GetEventStaffs called with eventId: {eventId}");
        
        try
        {
            var userId = GetCurrentUserId();
            Console.WriteLine($"[DEBUG] UserId: {userId}");
            
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }
            
            var result = await _staffService.GetEventStaffsAsync(userId, eventId);
            Console.WriteLine($"[DEBUG] GetEventStaffs result: {result?.Staffs?.Count ?? 0} staffs");
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
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("invitations")]
    public async Task<ActionResult<StaffInvitationDto>> InviteStaff([FromBody] CreateStaffInvitationRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var dto = new CreateStaffInvitationDto
            {
                EventId = request.EventId,
                OrganizationId = request.OrganizationId,
                InvitedUserEmail = request.InvitedUserEmail,
                EventStartTime = request.EventStartTime,
                EventEndTime = request.EventEndTime,
                InviteExpiredAt = request.InviteExpiredAt
            };
            var result = await _staffService.InviteStaffAsync(userId, dto);
            
            // Send SignalR notification to invited user
            await _hubContext.Clients.Group($"user_{result.InvitedUserId}").SendAsync("StaffInvited", new
            {
                InvitationId = result.Id,
                EventId = result.EventId,
                EventName = result.EventName,
                OrganizationName = result.OrganizationName
            });
            
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
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("invitations/{invitationId}/respond")]
    public async Task<ActionResult<StaffInvitationDto>> RespondToInvitation(
        [FromRoute] Guid invitationId,
        [FromBody] RespondToInvitationRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var dto = new RespondToInvitationDto
            {
                InvitationId = invitationId,
                Accept = request.Accept
            };
            var result = await _staffService.RespondToInvitationAsync(userId, dto);
            
            // Send SignalR notification
            await _hubContext.Clients.Group($"user_{userId}").SendAsync("StaffInvitationResponded", new
            {
                InvitationId = result.Id,
                Status = result.Status,
                EventId = result.EventId
            });
            
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
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("invitations/my-invitations")]
    public async Task<ActionResult<IEnumerable<StaffInvitationDto>>> GetMyInvitations()
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _staffService.GetMyInvitationsAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("events/{eventId}/invitations")]
    public async Task<ActionResult<IEnumerable<StaffInvitationDto>>> GetEventInvitations([FromRoute] Guid eventId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _staffService.GetEventInvitationsAsync(userId, eventId);
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
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("tasks")]
    public async Task<ActionResult<EventTaskDto>> CreateTask([FromBody] CreateEventTaskRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var dto = new CreateEventTaskDto
            {
                EventId = request.EventId,
                Name = request.Name,
                Description = request.Description,
                Options = request.Options ?? new List<string>()
            };
            var result =  await _staffService.CreateTaskAsync(userId, dto);
            
            // Send SignalR notification
            await _hubContext.Clients.Group($"event_{request.EventId}").SendAsync("TaskCreated", new
            {
                TaskId = result.Id,
                EventId = request.EventId
            });
            
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
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("events/{eventId}/tasks/{taskId}")]
    public async Task<ActionResult<EventTaskDto>> UpdateTask(
        [FromRoute] Guid eventId,
        [FromRoute] Guid taskId,
        [FromBody] UpdateEventTaskRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var dto = new UpdateEventTaskDto
            {
                Name = request.Name,
                Description = request.Description
            };
            var result = await _staffService.UpdateTaskAsync(userId, eventId, taskId, dto);
            
            // Send SignalR notification
            await _hubContext.Clients.Group($"event_{eventId}").SendAsync("TaskUpdated", new
            {
                TaskId = taskId,
                EventId = eventId
            });
            
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
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("events/{eventId}/tasks/{taskId}")]
    public async Task<ActionResult> DeleteTask([FromRoute] Guid eventId, [FromRoute] Guid taskId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _staffService.DeleteTaskAsync(userId, eventId, taskId);
            
            // Send SignalR notification
            await _hubContext.Clients.Group($"event_{eventId}").SendAsync("TaskDeleted", new
            {
                TaskId = taskId,
                EventId = eventId
            });
            
            return Ok(new { message = "Task deleted successfully" });
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

    [HttpGet("events/{eventId}/tasks")]
    public async Task<ActionResult<IEnumerable<EventTaskDto>>> GetEventTasks([FromRoute] Guid eventId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _staffService.GetEventTasksAsync(userId, eventId);
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
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("events/{eventId}/tasks/assign")]
    public async Task<ActionResult> AssignTaskToStaff(
        [FromRoute] Guid eventId,
        [FromBody] AssignTaskToStaffRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var dto = new AssignTaskToStaffDto
            {
                TaskId = request.TaskId,
                OptionId = request.OptionId,
                StaffId = request.StaffId
            };
            await _staffService.AssignTaskToStaffAsync(userId, eventId, dto);
            
            // Send SignalR notification
            await _hubContext.Clients.Group($"event_{eventId}").SendAsync("TaskAssigned", new
            {
                TaskId = dto.TaskId,
                OptionId = dto.OptionId,
                StaffId = dto.StaffId,
                EventId = eventId
            });
            
            return Ok(new { message = "Task assigned successfully" });
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

    [HttpDelete("events/{eventId}/tasks/{taskId}/options/{optionId}/staffs/{staffId}")]
    public async Task<ActionResult> UnassignTaskFromStaff(
        [FromRoute] Guid eventId,
        [FromRoute] Guid taskId,
        [FromRoute] Guid optionId,
        [FromRoute] Guid staffId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _staffService.UnassignTaskFromStaffAsync(userId, eventId, taskId, optionId, staffId);
            
            // Send SignalR notification
            await _hubContext.Clients.Group($"event_{eventId}").SendAsync("TaskUnassigned", new
            {
                TaskId = taskId,
                OptionId = optionId,
                StaffId = staffId,
                EventId = eventId
            });
            
            return Ok(new { message = "Task unassigned successfully" });
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

    [HttpPost("events/{eventId}/tasks/{taskId}/options")]
    public async Task<ActionResult<TaskOptionDto>> CreateTaskOption(
        [FromRoute] Guid eventId,
        [FromRoute] Guid taskId,
        [FromBody] CreateTaskOptionRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var dto = new CreateTaskOptionDto
            {
                TaskId = taskId,
                OptionName = request.OptionName
            };
            var result = await _staffService.CreateTaskOptionAsync(userId, eventId, taskId, dto);
            
            // Send SignalR notification
            await _hubContext.Clients.Group($"event_{eventId}").SendAsync("TaskOptionCreated", new
            {
                OptionId = result.Id,
                TaskId = taskId,
                EventId = eventId
            });
            
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
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("events/{eventId}/tasks/{taskId}/options/{optionId}")]
    public async Task<ActionResult<TaskOptionDto>> UpdateTaskOption(
        [FromRoute] Guid eventId,
        [FromRoute] Guid taskId,
        [FromRoute] Guid optionId,
        [FromBody] UpdateTaskOptionRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _staffService.UpdateTaskOptionAsync(userId, eventId, taskId, optionId, request.OptionName);
            
            // Send SignalR notification
            await _hubContext.Clients.Group($"event_{eventId}").SendAsync("TaskOptionUpdated", new
            {
                OptionId = optionId,
                TaskId = taskId,
                EventId = eventId
            });
            
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
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("events/{eventId}/tasks/{taskId}/options/{optionId}")]
    public async Task<ActionResult> DeleteTaskOption(
        [FromRoute] Guid eventId,
        [FromRoute] Guid taskId,
        [FromRoute] Guid optionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _staffService.DeleteTaskOptionAsync(userId, eventId, taskId, optionId);
            
            // Send SignalR notification
            await _hubContext.Clients.Group($"event_{eventId}").SendAsync("TaskOptionDeleted", new
            {
                OptionId = optionId,
                TaskId = taskId,
                EventId = eventId
            });
            
            return Ok(new { message = "Task option deleted successfully" });
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

    [HttpGet("events/{eventId}/tasks/{taskId}/options")]
    public async Task<ActionResult<IEnumerable<TaskOptionDto>>> GetTaskOptions(
        [FromRoute] Guid eventId,
        [FromRoute] Guid taskId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _staffService.GetTaskOptionsAsync(userId, eventId, taskId);
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
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

