namespace Eventiq.Application.Dtos;

public class OrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    public string Avatar { get; set; } = "";

}
public class CreateOrganizationResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    public string Avatar { get; set; } = "";
    public string? Jwt {get; set; } = "";

}

public class DeleteOrganizationResponse
{
    public string? Jwt { get; set; }
    public bool Success { get; set; }
}
public class CreateOrganizationDto
{
    public string Name { get; set; }
    public Stream AvatarStream { get; set; }
    public string Avatar { get; set; } = "";
}
public class UpdateOrganizationDto
{
    public string? Name { get; set; }
    public Stream? AvatarStream { get; set; }
    public string? Avatar { get; set; } = "";
}