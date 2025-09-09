using System.ComponentModel.DataAnnotations;

namespace Eventiq.Application.Dtos;

public class UserDto
{
    public string Id { get; set; }
    public string? Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    //public string Role { get; set; } = "User"; 
}

public class CreateUserDto
{

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;
    [Required]
    [StringLength(100)]
    public string? FirstName { get; set; }
    [Required]
    [StringLength(100)]
    public string? LastName { get; set; }

}

public class UpdateUserDto
{
    [StringLength(100)]
    public string? FirstName { get; set; }

    [StringLength(100)]
    public string? LastName { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }
}

public class LoginDto
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
}
