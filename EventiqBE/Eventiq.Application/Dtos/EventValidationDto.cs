using Eventiq.Domain.Entities;

namespace Eventiq.Application.Dtos;

public class EventValidationDto
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class EventSubmissionResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = default!;
    public EventStatus NewStatus { get; set; }
}

