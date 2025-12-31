using Microsoft.AspNetCore.Http;

namespace Eventiq.Api.Request;

public class UpdatePayoutRequest
{
    public string? ProofImageUrl { get; set; }
    public string? Notes { get; set; }
    public IFormFile? ProofImage { get; set; }
}

