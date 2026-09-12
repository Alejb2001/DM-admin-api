using System.Security.Claims;
using DmAdminApi.Features.CharacterSheets.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DmAdminApi.Features.CharacterSheets;

[ApiController]
[Route("api/campaigns/{campaignId:guid}/entities/{entityId:guid}/resources")]
[Authorize]
public class CharacterSheetController(CharacterSheetService sheetService) : ControllerBase
{
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetResources(Guid campaignId, Guid entityId)
    {
        try
        {
            var resources = await sheetService.GetResourcesAsync(entityId, campaignId, UserId);
            return Ok(resources);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException e) { return NotFound(new { error = e.Message }); }
    }

    [HttpPost]
    public async Task<IActionResult> CreateResource(
        Guid campaignId, Guid entityId, [FromBody] CreateCharacterResourceDto dto)
    {
        try
        {
            var resource = await sheetService.CreateResourceAsync(entityId, campaignId, dto, UserId);
            return StatusCode(201, resource);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException e) { return NotFound(new { error = e.Message }); }
    }

    [HttpPatch("{resourceId:guid}")]
    public async Task<IActionResult> UpdateValue(
        Guid campaignId, Guid entityId, Guid resourceId,
        [FromBody] UpdateResourceValueDto dto,
        [FromQuery] Guid? sessionId)
    {
        try
        {
            var resource = await sheetService.UpdateResourceValueAsync(
                entityId, resourceId, campaignId, dto, UserId, sessionId);
            return Ok(resource);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException e) { return NotFound(new { error = e.Message }); }
    }

    [HttpDelete("{resourceId:guid}")]
    public async Task<IActionResult> DeleteResource(
        Guid campaignId, Guid entityId, Guid resourceId)
    {
        try
        {
            await sheetService.DeleteResourceAsync(entityId, resourceId, campaignId, UserId);
            return NoContent();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException e) { return NotFound(new { error = e.Message }); }
    }
}
