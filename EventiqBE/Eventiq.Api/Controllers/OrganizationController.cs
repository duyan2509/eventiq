using AutoMapper;
using Eventiq.Api.Request;
using Eventiq.Application.Dtos;
using Eventiq.Application.Interfaces.Services;
using Eventiq.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Eventiq.Api.Controllers;
[Authorize]
public class OrganizationController:BaseController
{
    protected readonly IMapper _mapper;
    protected readonly IOrganizationService _orgService;

    public OrganizationController(IMapper mapper, IOrganizationService orgService)
    {
        _mapper = mapper;
        _orgService = orgService;
    }

    [HttpPost]
    public async Task<ActionResult<CreateOrganizationResponse>> PostOrganization([FromForm] CreateOrganizationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        try
        {
            var userId = GetCurrentUserId();
            var dto = _mapper.Map<CreateOrganizationDto>(request);
            {
                var memStream = new MemoryStream();
                await request.Avatar.CopyToAsync(memStream);
                memStream.Position = 0;
                dto.AvatarStream = memStream;
            }
            var response = await _orgService.CreateOrganizationAsync(userId, dto);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [HttpPatch("{orgId}")]
    public async Task<ActionResult<OrganizationDto>> PatchOrganization(Guid orgId, [FromForm] UpdateOrganizationRequest request)
    {
        if(!request.Valid())
            return BadRequest(new {message = "No fields update"});
        try
        {
            var userId = GetCurrentUserId();
            var dto = _mapper.Map<UpdateOrganizationDto>(request);
            if (request.Avatar != null)
            {
                var memStream = new MemoryStream();
                await request.Avatar.CopyToAsync(memStream);
                memStream.Position = 0;
                dto.AvatarStream = memStream;
            }

            var response = await _orgService.UpdateOrganizationAsync(userId, orgId, dto);
            return Ok(response);
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

    [HttpDelete("{orgId}")]
    public async Task<ActionResult<DeleteOrganizationResponse>> DeleteOrganization(Guid orgId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _orgService.DeleteOrganizationAsync(userId, orgId);
            return Ok(response);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new {message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("my-organization")]
    public async Task<ActionResult<IEnumerable<OrganizationDto>>> GetMyOrganization()
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _orgService.GetMyOrgsAsync(userId);
            return Ok(response);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}