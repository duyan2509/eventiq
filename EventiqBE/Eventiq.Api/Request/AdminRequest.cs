namespace Eventiq.Api.Request;

public class ApproveEventRequest
{
    public string? Comment { get; set; }
}

public class RejectEventRequest
{
    public required string Comment { get; set; }
}

