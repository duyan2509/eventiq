using AutoMapper;
using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces;
using Eventiq.Application.Interfaces.Repositories;
using Eventiq.Application.Interfaces.Services;
using Eventiq.Domain.Entities;

namespace Eventiq.Application.Services;

public class StaffService : IStaffService
{
    private readonly IStaffRepository _staffRepository;
    private readonly IStaffInvitationRepository _invitationRepository;
    private readonly IEventTaskRepository _taskRepository;
    private readonly ITaskOptionRepository _taskOptionRepository;
    private readonly IStaffTaskAssignmentRepository _taskAssignmentRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IIdentityService _identityService;
    private readonly ITicketRepository _ticketRepository;
    private readonly IEventItemRepository _eventItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public StaffService(
        IStaffRepository staffRepository,
        IStaffInvitationRepository invitationRepository,
        IEventTaskRepository taskRepository,
        ITaskOptionRepository taskOptionRepository,
        IStaffTaskAssignmentRepository taskAssignmentRepository,
        IEventRepository eventRepository,
        IOrganizationRepository organizationRepository,
        IIdentityService identityService,
        ITicketRepository ticketRepository,
        IEventItemRepository eventItemRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _staffRepository = staffRepository;
        _invitationRepository = invitationRepository;
        _taskRepository = taskRepository;
        _taskOptionRepository = taskOptionRepository;
        _taskAssignmentRepository = taskAssignmentRepository;
        _eventRepository = eventRepository;
        _organizationRepository = organizationRepository;
        _identityService = identityService;
        _ticketRepository = ticketRepository;
        _eventItemRepository = eventItemRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<StaffListDto> GetEventStaffsAsync(Guid userId, Guid eventId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");

        // Check if user is organization owner
        await ValidateEventOwnerAsync(userId, evnt.OrganizationId);

        var staffs = await _staffRepository.GetByEventIdAsync(eventId);
        var staffDtos = new List<StaffDto>();

        foreach (var staff in staffs)
        {
            var user = await _identityService.GetByIdAsync(staff.UserId);
            var tasks = staff.TaskAssignments.Select(ta => new TaskAssignmentDto
            {
                TaskId = ta.Task.Id,
                TaskName = ta.Task.Name,
                OptionId = ta.Option.Id,
                OptionName = ta.Option.OptionName
            }).ToList();

            staffDtos.Add(new StaffDto
            {
                Id = staff.Id,
                EventId = staff.EventId,
                UserId = staff.UserId,
                UserName = user?.Username,
                UserEmail = user?.Email,
                StartTime = staff.StartTime,
                EndTime = staff.EndTime,
                AssignedTasks = tasks
            });
        }

        return new StaffListDto
        {
            EventId = eventId,
            EventName = evnt.Name,
            Staffs = staffDtos
        };
    }

    public async Task<StaffInvitationDto> InviteStaffAsync(Guid userId, CreateStaffInvitationDto dto)
    {
        var evnt = await _eventRepository.GetByIdAsync(dto.EventId);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");

        // Check if user is organization owner
        await ValidateEventOwnerAsync(userId, evnt.OrganizationId);

        if (dto.OrganizationId != evnt.OrganizationId)
            throw new UnauthorizedAccessException("Organization mismatch");

        // Find user by email
        UserDto invitedUser;
        try
        {
            invitedUser = await _identityService.GetByEmailAsync(dto.InvitedUserEmail);
        }
        catch (KeyNotFoundException)
        {
            throw new KeyNotFoundException($"User with email {dto.InvitedUserEmail} not found in the system");
        }

        var invitedUserId = Guid.Parse(invitedUser.Id);

        // Check if user already has pending invitation for this event
        var existingInvitation = await _invitationRepository.GetPendingByEventIdAndUserIdAsync(dto.EventId, invitedUserId);
        if (existingInvitation != null)
            throw new InvalidOperationException("User already has a pending invitation for this event");

        // Check if user is already staff for this event
        var existingStaff = await _staffRepository.GetByEventIdAndUserIdAsync(dto.EventId, invitedUserId);
        if (existingStaff != null)
            throw new InvalidOperationException("User is already staff for this event");

        // Check if invitation expiration is valid
        if (dto.InviteExpiredAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Invitation expiration must be in the future");

        var invitation = new StaffInvitation
        {
            EventId = dto.EventId,
            OrganizationId = dto.OrganizationId,
            InvitedUserId = invitedUserId,
            EventStartTime = DateTime.MinValue,
            EventEndTime = DateTime.MinValue,
            InviteExpiredAt = dto.InviteExpiredAt,
            Status = InvitationStatus.Pending
        };

        await _invitationRepository.AddAsync(invitation);

        var org = await _organizationRepository.GetByIdAsync(dto.OrganizationId);

        return new StaffInvitationDto
        {
            Id = invitation.Id,
            EventId = invitation.EventId,
            EventName = evnt.Name,
            OrganizationId = invitation.OrganizationId,
            OrganizationName = org?.Name ?? string.Empty,
            InvitedUserId = invitation.InvitedUserId,
            InvitedUserName = invitedUser.Username,
            InvitedUserEmail = invitedUser.Email,
            InviteExpiredAt = invitation.InviteExpiredAt,
            Status = invitation.Status.ToString(),
            CreatedAt = invitation.CreatedAt
        };
    }

    public async Task<StaffInvitationDto> RespondToInvitationAsync(Guid userId, RespondToInvitationDto dto)
    {
        var invitation = await _invitationRepository.GetByIdAsync(dto.InvitationId);
        if (invitation == null)
            throw new KeyNotFoundException("Invitation not found");

        if (invitation.InvitedUserId != userId)
            throw new UnauthorizedAccessException("This invitation is not for you");

        if (invitation.Status != InvitationStatus.Pending)
            throw new InvalidOperationException($"Invitation is already {invitation.Status}");

        if (invitation.InviteExpiredAt < DateTime.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            await _invitationRepository.UpdateAsync(invitation);
            throw new InvalidOperationException("Invitation has expired");
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            if (dto.Accept)
            {
                var eventItems = await _eventItemRepository.GetAllByEventIdAsync(invitation.EventId);
                var now = DateTime.UtcNow;
                
                var activeEventItem = eventItems
                    .FirstOrDefault(ei => ei.Start <= now && ei.End >= now);
                
                if (activeEventItem == null)
                {
                    activeEventItem = eventItems
                        .OrderBy(ei => ei.Start)
                        .FirstOrDefault();
                }

                if (activeEventItem == null)
                {
                    invitation.Status = InvitationStatus.Rejected;
                    invitation.RespondedAt = DateTime.UtcNow;
                    await _invitationRepository.UpdateAsync(invitation);
                    await _unitOfWork.CommitAsync();
                    throw new InvalidOperationException("Event has no event items");
                }

                var staff = new Staff
                {
                    EventId = invitation.EventId,
                    UserId = userId,
                    StartTime = activeEventItem.Start,
                    EndTime = activeEventItem.End
                };

                await _staffRepository.AddAsync(staff);
                invitation.Status = InvitationStatus.Accepted;
            }
            else
            {
                invitation.Status = InvitationStatus.Rejected;
            }

            invitation.RespondedAt = DateTime.UtcNow;
            await _invitationRepository.UpdateAsync(invitation);
            await _unitOfWork.CommitAsync();

            var evnt = await _eventRepository.GetByIdAsync(invitation.EventId);
            var org = await _organizationRepository.GetByIdAsync(invitation.OrganizationId);
            var user = await _identityService.GetByIdAsync(userId);

            return new StaffInvitationDto
            {
                Id = invitation.Id,
                EventId = invitation.EventId,
                EventName = evnt?.Name ?? string.Empty,
                OrganizationId = invitation.OrganizationId,
                OrganizationName = org?.Name ?? string.Empty,
                InvitedUserId = invitation.InvitedUserId,
                InvitedUserName = user.Username,
                InvitedUserEmail = user.Email,
                InviteExpiredAt = invitation.InviteExpiredAt,
                Status = invitation.Status.ToString(),
                RespondedAt = invitation.RespondedAt,
                CreatedAt = invitation.CreatedAt
            };
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<StaffInvitationDto>> GetMyInvitationsAsync(Guid userId)
    {
        var invitations = await _invitationRepository.GetByInvitedUserIdAsync(userId);
        var result = new List<StaffInvitationDto>();

        foreach (var invitation in invitations)
        {
            var evnt = await _eventRepository.GetByIdAsync(invitation.EventId);
            var org = await _organizationRepository.GetByIdAsync(invitation.OrganizationId);
            var user = await _identityService.GetByIdAsync(invitation.InvitedUserId);

            result.Add(new StaffInvitationDto
            {
                Id = invitation.Id,
                EventId = invitation.EventId,
                EventName = evnt?.Name ?? string.Empty,
                OrganizationId = invitation.OrganizationId,
                OrganizationName = org?.Name ?? string.Empty,
                InvitedUserId = invitation.InvitedUserId,
                InvitedUserName = user.Username,
                InvitedUserEmail = user.Email,
                InviteExpiredAt = invitation.InviteExpiredAt,
                Status = invitation.Status.ToString(),
                RespondedAt = invitation.RespondedAt,
                CreatedAt = invitation.CreatedAt
            });
        }

        return result;
    }

    public async Task<IEnumerable<StaffInvitationDto>> GetEventInvitationsAsync(Guid userId, Guid eventId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");

        await ValidateEventOwnerAsync(userId, evnt.OrganizationId);

        var invitations = await _invitationRepository.GetByEventIdAsync(eventId);
        var result = new List<StaffInvitationDto>();

        foreach (var invitation in invitations)
        {
            var org = await _organizationRepository.GetByIdAsync(invitation.OrganizationId);
            var user = await _identityService.GetByIdAsync(invitation.InvitedUserId);

            result.Add(new StaffInvitationDto
            {
                Id = invitation.Id,
                EventId = invitation.EventId,
                EventName = evnt.Name,
                OrganizationId = invitation.OrganizationId,
                OrganizationName = org?.Name ?? string.Empty,
                InvitedUserId = invitation.InvitedUserId,
                InvitedUserName = user.Username,
                InvitedUserEmail = user.Email,
                InviteExpiredAt = invitation.InviteExpiredAt,
                Status = invitation.Status.ToString(),
                RespondedAt = invitation.RespondedAt,
                CreatedAt = invitation.CreatedAt
            });
        }

        return result;
    }

    public async Task<EventTaskDto> CreateTaskAsync(Guid userId, CreateEventTaskDto dto)
    {
        var evnt = await _eventRepository.GetByIdAsync(dto.EventId);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");

        await ValidateEventOwnerAsync(userId, evnt.OrganizationId);

        if (dto.Options == null || dto.Options.Count == 0)
            throw new InvalidOperationException("Task must have at least one option");

        var task = new EventTask
        {
            EventId = dto.EventId,
            Name = dto.Name,
            Description = dto.Description,
            IsDefault = false
        };

        await _taskRepository.AddAsync(task);

        // Create options for the task
        var optionDtos = new List<TaskOptionDto>();
        foreach (var optionName in dto.Options)
        {
            if (string.IsNullOrWhiteSpace(optionName))
                continue;

            var option = new TaskOption
            {
                TaskId = task.Id,
                OptionName = optionName.Trim()
            };
            await _taskOptionRepository.AddAsync(option);

            optionDtos.Add(new TaskOptionDto
            {
                Id = option.Id,
                TaskId = option.TaskId,
                OptionName = option.OptionName,
                AssignedStaffs = new List<AssignedStaffDto>()
            });
        }

        await _unitOfWork.SaveChangesAsync();

        return new EventTaskDto
        {
            Id = task.Id,
            EventId = task.EventId,
            Name = task.Name,
            Description = task.Description,
            IsDefault = task.IsDefault,
            Options = optionDtos,
            AssignedStaffs = new List<AssignedStaffDto>()
        };
    }

    public async Task<EventTaskDto> UpdateTaskAsync(Guid userId, Guid eventId, Guid taskId, UpdateEventTaskDto dto)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");

        await ValidateEventOwnerAsync(userId, evnt.OrganizationId);

        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null || task.EventId != eventId)
            throw new KeyNotFoundException("Task not found");

        if (dto.Name != null)
            task.Name = dto.Name;

        if (dto.Description != null)
            task.Description = dto.Description;

        await _taskRepository.UpdateAsync(task);

        var options = await _taskOptionRepository.GetByTaskIdAsync(taskId);
        var optionDtos = options.Select(o => new TaskOptionDto
        {
            Id = o.Id,
            TaskId = o.TaskId,
            OptionName = o.OptionName,
            AssignedStaffs = o.StaffAssignments.Select(sa => new AssignedStaffDto
            {
                StaffId = sa.Staff.Id,
                UserId = sa.Staff.UserId,
                UserName = null,
                UserEmail = null
            }).ToList()
        }).ToList();

        return new EventTaskDto
        {
            Id = task.Id,
            EventId = task.EventId,
            Name = task.Name,
            Description = task.Description,
            IsDefault = task.IsDefault,
            Options = optionDtos,
            AssignedStaffs = new List<AssignedStaffDto>()
        };
    }

    public async Task<bool> DeleteTaskAsync(Guid userId, Guid eventId, Guid taskId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");

        await ValidateEventOwnerAsync(userId, evnt.OrganizationId);

        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null || task.EventId != eventId)
            throw new KeyNotFoundException("Task not found");

        if (task.IsDefault)
            throw new InvalidOperationException("Cannot delete default task");

        // Check if task has assignments
        var hasAssignments = await _taskRepository.HasAssignmentsAsync(taskId);
        if (hasAssignments)
            throw new InvalidOperationException("Task is currently assigned to staff and cannot be deleted");

        await _taskRepository.HardDeleteAsync(taskId);
        return true;
    }

    public async Task<IEnumerable<EventTaskDto>> GetEventTasksAsync(Guid userId, Guid eventId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");

        await ValidateEventOwnerAsync(userId, evnt.OrganizationId);

        var tasks = await _taskRepository.GetByEventIdAsync(eventId);
        var result = new List<EventTaskDto>();

        foreach (var task in tasks)
        {
            var optionDtos = task.Options.Select(o => new TaskOptionDto
            {
                Id = o.Id,
                TaskId = o.TaskId,
                OptionName = o.OptionName,
                AssignedStaffs = o.StaffAssignments.Select(sa => new AssignedStaffDto
                {
                    StaffId = sa.Staff.Id,
                    UserId = sa.Staff.UserId,
                    UserName = null,
                    UserEmail = null
                }).ToList()
            }).ToList();

            result.Add(new EventTaskDto
            {
                Id = task.Id,
                EventId = task.EventId,
                Name = task.Name,
                Description = task.Description,
                IsDefault = task.IsDefault,
                Options = optionDtos,
                AssignedStaffs = new List<AssignedStaffDto>()
            });
        }

        return result;
    }

    public async Task<bool> AssignTaskToStaffAsync(Guid userId, Guid eventId, AssignTaskToStaffDto dto)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");

        await ValidateEventOwnerAsync(userId, evnt.OrganizationId);

        var staff = await _staffRepository.GetByIdAsync(dto.StaffId);
        if (staff == null || staff.EventId != eventId)
            throw new KeyNotFoundException("Staff not found for this event");

        var task = await _taskRepository.GetByIdAsync(dto.TaskId);
        if (task == null || task.EventId != eventId)
            throw new KeyNotFoundException("Task not found for this event");
        
        var option = await _taskOptionRepository.GetByIdAsync(dto.OptionId);
        if (option == null || option.TaskId != dto.TaskId)
            throw new KeyNotFoundException("Task option not found for this task");

        // Check if already assigned
        var existing = await _taskAssignmentRepository.GetByStaffTaskAndOptionAsync(dto.StaffId, dto.TaskId, dto.OptionId);
        if (existing != null)
            throw new InvalidOperationException("Task option is already assigned to this staff");

        var assignment = new StaffTaskAssignment
        {
            StaffId = dto.StaffId,
            TaskId = dto.TaskId,
            OptionId = dto.OptionId
        };

        await _taskAssignmentRepository.AddAsync(assignment);
        return true;
    }

    public async Task<bool> UnassignTaskFromStaffAsync(Guid userId, Guid eventId, Guid taskId, Guid optionId, Guid staffId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");

        await ValidateEventOwnerAsync(userId, evnt.OrganizationId);

        var assignment = await _taskAssignmentRepository.GetByStaffTaskAndOptionAsync(staffId, taskId, optionId);
        if (assignment == null)
            throw new KeyNotFoundException("Task assignment not found");

        await _taskAssignmentRepository.HardDeleteAsync(assignment.Id);
        return true;
    }

    public async Task<TaskOptionDto> CreateTaskOptionAsync(Guid userId, Guid eventId, Guid taskId, CreateTaskOptionDto dto)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");

        await ValidateEventOwnerAsync(userId, evnt.OrganizationId);

        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null || task.EventId != eventId)
            throw new KeyNotFoundException("Task not found");

        var option = new TaskOption
        {
            TaskId = taskId,
            OptionName = dto.OptionName
        };

        await _taskOptionRepository.AddAsync(option);

        return new TaskOptionDto
        {
            Id = option.Id,
            TaskId = option.TaskId,
            OptionName = option.OptionName,
            AssignedStaffs = new List<AssignedStaffDto>()
        };
    }

    public async Task<TaskOptionDto> UpdateTaskOptionAsync(Guid userId, Guid eventId, Guid taskId, Guid optionId, string optionName)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");

        await ValidateEventOwnerAsync(userId, evnt.OrganizationId);

        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null || task.EventId != eventId)
            throw new KeyNotFoundException("Task not found");

        var option = await _taskOptionRepository.GetByIdAsync(optionId);
        if (option == null || option.TaskId != taskId)
            throw new KeyNotFoundException("Task option not found");

        option.OptionName = optionName;
        await _taskOptionRepository.UpdateAsync(option);

        var optionWithAssignments = await _taskOptionRepository.GetByIdWithAssignmentsAsync(optionId);
        var assignedStaffs = optionWithAssignments?.StaffAssignments.Select(sa => new AssignedStaffDto
        {
            StaffId = sa.Staff.Id,
            UserId = sa.Staff.UserId,
            UserName = null,
            UserEmail = null
        }).ToList() ?? new List<AssignedStaffDto>();

        return new TaskOptionDto
        {
            Id = option.Id,
            TaskId = option.TaskId,
            OptionName = option.OptionName,
            AssignedStaffs = assignedStaffs
        };
    }

    public async Task<bool> DeleteTaskOptionAsync(Guid userId, Guid eventId, Guid taskId, Guid optionId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");

        await ValidateEventOwnerAsync(userId, evnt.OrganizationId);

        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null || task.EventId != eventId)
            throw new KeyNotFoundException("Task not found");

        var option = await _taskOptionRepository.GetByIdAsync(optionId);
        if (option == null || option.TaskId != taskId)
            throw new KeyNotFoundException("Task option not found");

        // Check if option has assignments
        var hasAssignments = await _taskOptionRepository.HasAssignmentsAsync(optionId);
        if (hasAssignments)
            throw new InvalidOperationException("Task option is currently assigned to staff and cannot be deleted");

        await _taskOptionRepository.HardDeleteAsync(optionId);
        return true;
    }

    public async Task<IEnumerable<TaskOptionDto>> GetTaskOptionsAsync(Guid userId, Guid eventId, Guid taskId)
    {
        var evnt = await _eventRepository.GetByIdAsync(eventId);
        if (evnt == null)
            throw new KeyNotFoundException("Event not found");

        await ValidateEventOwnerAsync(userId, evnt.OrganizationId);

        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null || task.EventId != eventId)
            throw new KeyNotFoundException("Task not found");

        var options = await _taskOptionRepository.GetByTaskIdAsync(taskId);
        return options.Select(o => new TaskOptionDto
        {
            Id = o.Id,
            TaskId = o.TaskId,
            OptionName = o.OptionName,
            AssignedStaffs = o.StaffAssignments.Select(sa => new AssignedStaffDto
            {
                StaffId = sa.Staff.Id,
                UserId = sa.Staff.UserId,
                UserName = null,
                UserEmail = null
            }).ToList()
        });
    }

    public async Task<StaffCalendarDto> GetStaffCalendarAsync(Guid userId, int month, int year)
    {
        var staffs = await _staffRepository.GetByUserIdAndMonthAsync(userId, month, year);
        var events = new List<StaffCalendarEventDto>();

        foreach (var staff in staffs)
        {
            var evnt = staff.Event;
            if (evnt == null) continue;

            var org = evnt.Organization;
            var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1).AddMilliseconds(-1);

            var currentDate = staff.StartTime.Date;
            var staffEndDate = staff.EndTime.Date;
            
            while (currentDate <= staffEndDate && currentDate <= endDate.Date)
            {
                if (currentDate >= startDate.Date)
                {
                    events.Add(new StaffCalendarEventDto
                    {
                        StaffId = staff.Id,
                        EventId = staff.EventId,
                        EventName = evnt.Name,
                        OrganizationName = org?.Name ?? string.Empty,
                        StartTime = staff.StartTime,
                        EndTime = staff.EndTime,
                        Date = currentDate
                    });
                }
                currentDate = currentDate.AddDays(1);
            }
        }

        return new StaffCalendarDto
        {
            Month = month,
            Year = year,
            Events = events
        };
    }

    public async Task<CurrentShiftDto?> GetCurrentShiftAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var staff = await _staffRepository.GetCurrentShiftAsync(userId, now);
        
        if (staff == null)
            return null;

        var evnt = staff.Event;
        if (evnt == null)
            return null;

        var activeEventItem = evnt.EventItem
            .Where(ei => ei.Start <= now && ei.End >= now)
            .OrderByDescending(ei => ei.Start)
            .FirstOrDefault();

        if (activeEventItem == null)
            return null;

        var org = evnt.Organization;
        var assignedTasks = staff.TaskAssignments.Select(ta => new AssignedTaskDto
        {
            TaskId = ta.Task.Id,
            TaskName = ta.Task.Name,
            OptionId = ta.Option.Id,
            OptionName = ta.Option.OptionName
        }).ToList();

        return new CurrentShiftDto
        {
            StaffId = staff.Id,
            EventId = staff.EventId,
            EventItemId = activeEventItem.Id,
            EventName = evnt.Name,
            OrganizationName = org?.Name ?? string.Empty,
            StartTime = activeEventItem.Start,
            EndTime = activeEventItem.End,
            AssignedTasks = assignedTasks
        };
    }

    public async Task<VerifyTicketResponse> VerifyTicketAsync(Guid userId, Guid staffId, VerifyTicketRequest request)
    {
        var staff = await _staffRepository.GetByIdAsync(staffId);
        if (staff == null || staff.UserId != userId)
            throw new UnauthorizedAccessException("Staff not found or does not belong to user");

        if (!Guid.TryParse(request.TicketId, out var ticketId))
        {
            return new VerifyTicketResponse
            {
                IsValid = false,
                Message = "Invalid ticket ID format"
            };
        }

        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket == null || ticket.IsDeleted)
        {
            return new VerifyTicketResponse
            {
                IsValid = false,
                Message = "Ticket not found"
            };
        }

        var eventItem = await _eventItemRepository.GetByDetailByIdAsync(ticket.EventItemId);
        if (eventItem == null)
        {
            return new VerifyTicketResponse
            {
                IsValid = false,
                Message = "Event item not found"
            };
        }

        if (eventItem.EventId != staff.EventId)
        {
            return new VerifyTicketResponse
            {
                IsValid = false,
                Message = "Ticket does not belong to this event"
            };
        }

        var evnt = eventItem.Event;
        if (evnt == null)
        {
            return new VerifyTicketResponse
            {
                IsValid = false,
                Message = "Event not found"
            };
        }

        var now = DateTime.UtcNow;
        var eventItems = await _eventItemRepository.GetAllByEventIdAsync(staff.EventId);
        var activeEventItem = eventItems
            .FirstOrDefault(ei => ei.Start <= now && ei.End >= now);

        if (activeEventItem == null)
        {
            return new VerifyTicketResponse
            {
                IsValid = false,
                Message = "No active event item is currently happening"
            };
        }

        return new VerifyTicketResponse
        {
            IsValid = true,
            Message = "Ticket is valid",
            TicketInfo = new TicketInfoDto
            {
                TicketId = ticket.Id,
                EventItemId = ticket.EventItemId,
                EventItemName = eventItem.Name,
                EventId = evnt.Id,
                EventName = evnt.Name,
                TicketClassName = ticket.TicketClass?.Name ?? string.Empty,
                UserId = ticket.UserId,
                PurchaseDate = ticket.CreatedAt
            }
        };
    }

    private async Task ValidateEventOwnerAsync(Guid userId, Guid organizationId)
    {
        var userOrgs = await _identityService.GetUserOrgsAsync(userId);
        if (!userOrgs.Contains(organizationId))
            throw new UnauthorizedAccessException("User does not have permission to manage this event");
    }
}

