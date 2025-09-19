using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Serialization;

namespace Eventiq.Api.Request;

public class CreateOrganizationRequest
{
    [StringLength(maximumLength: 30, MinimumLength = 2)]
    public string Name { get; set; }
    public IFormFile Avatar { get; set; }
}

public class UpdateOrganizationRequest
{
    [StringLength(maximumLength: 30, MinimumLength = 2)]
    public string? Name { get; set; }
    public IFormFile? Avatar { get; set; }

    public bool Valid()
    {
        return !string.IsNullOrEmpty(Name)||Avatar != null;
    }
}