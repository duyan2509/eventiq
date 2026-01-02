using Eventiq.Application.Dtos;

namespace Eventiq.Application.Interfaces.Services;

public interface IStaffService
{
    // Staff Management
    Task<StaffListDto> GetEventStaffsAsync(Guid userId, Guid eventId);
    
    // Invitation Management
    Task<StaffInvitationDto> InviteStaffAsync(Guid userId, CreateStaffInvitationDto dto);
    Task<StaffInvitationDto> RespondToInvitationAsync(Guid userId, RespondToInvitationDto dto);
    Task<IEnumerable<StaffInvitationDto>> GetMyInvitationsAsync(Guid userId);
    Task<IEnumerable<StaffInvitationDto>> GetEventInvitationsAsync(Guid userId, Guid eventId);
    
    // Task Management
    Task<EventTaskDto> CreateTaskAsync(Guid userId, CreateEventTaskDto dto);
    Task<EventTaskDto> UpdateTaskAsync(Guid userId, Guid eventId, Guid taskId, UpdateEventTaskDto dto);
    Task<bool> DeleteTaskAsync(Guid userId, Guid eventId, Guid taskId);
    Task<IEnumerable<EventTaskDto>> GetEventTasksAsync(Guid userId, Guid eventId);
    
    // Task Option Management
    Task<TaskOptionDto> CreateTaskOptionAsync(Guid userId, Guid eventId, Guid taskId, CreateTaskOptionDto dto);
    Task<TaskOptionDto> UpdateTaskOptionAsync(Guid userId, Guid eventId, Guid taskId, Guid optionId, string optionName);
    Task<bool> DeleteTaskOptionAsync(Guid userId, Guid eventId, Guid taskId, Guid optionId);
    Task<IEnumerable<TaskOptionDto>> GetTaskOptionsAsync(Guid userId, Guid eventId, Guid taskId);
    
    // Task Assignment
    Task<bool> AssignTaskToStaffAsync(Guid userId, Guid eventId, AssignTaskToStaffDto dto);
    Task<bool> UnassignTaskFromStaffAsync(Guid userId, Guid eventId, Guid taskId, Guid optionId, Guid staffId);
    
    // Staff Workspace
    Task<StaffCalendarDto> GetStaffCalendarAsync(Guid userId, int month, int year);
    Task<CurrentShiftDto?> GetCurrentShiftAsync(Guid userId);
    Task<VerifyTicketResponse> VerifyTicketAsync(Guid userId, Guid staffId, VerifyTicketRequest request);
}

