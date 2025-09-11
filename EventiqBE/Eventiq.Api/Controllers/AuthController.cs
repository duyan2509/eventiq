using System.Reflection.PortableExecutable;
using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.JSInterop.Infrastructure;

namespace Eventiq.Api.Controllers;
[Authorize]
public class AuthController(IUserService userService) : BaseController
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> Register([FromBody] CreateUserDto createUserDto)
    {
        try
        {
            var user = await userService.CreateAsync(createUserDto);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            var response = await userService.LoginAsync(loginDto);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await userService.GetByIdAsync(userId);
            return Ok(user);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }

    }
    [AllowAnonymous]
    [HttpPost("request-reset")]
    public async Task<IActionResult> RequestReset([FromBody] ForgotPasswordRequest dto)
    {
        if(!ModelState.IsValid)
            return BadRequest();
        try
        {
            await userService.RequestResetPasswordAsync(dto.Email);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [AllowAnonymous]
    [HttpPost("confirm-reset")]
    public async Task<IActionResult> ConfirmReset([FromBody] ResetPasswordRequest dto)
    {   
        if(!ModelState.IsValid)
            return BadRequest();
        try
        {
            await userService.ConfirmResetPasswordAsync(dto.Email,dto.ResetCode,dto.NewPassword);
            return Ok("Change Password Successfully");
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            await userService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
            return Ok();
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