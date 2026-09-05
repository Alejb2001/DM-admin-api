using System.Text;
using System.Text.Json;
using DmAdminApi.Common.Controllers;
using DmAdminApi.Features.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DmAdminApi.Features.World;

[Authorize]
[Route("api/campaigns/{campaignId:guid}")]
public class ExportController(ExportService exportService, PermissionService permissions) : ApiControllerBase
{
    // GET /api/campaigns/{id}/export — JSON file download (DM only)
    [HttpGet("export")]
    public async Task<IActionResult> ExportJson(Guid campaignId)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();

        var data = await exportService.ExportCampaignAsync(campaignId);
        if (data is null) return NotFound();

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        var bytes = Encoding.UTF8.GetBytes(json);
        var fileName = $"campaign-{data.Name.ToLower().Replace(" ", "-")}-{DateTime.UtcNow:yyyy-MM-dd}.json";
        return File(bytes, "application/json", fileName);
    }

    // GET /api/campaigns/{id}/entities/{entityId}/history — change log (Master only)
    [HttpGet("entities/{entityId:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid campaignId, Guid entityId)
    {
        if (!await permissions.IsCampaignOwnerAsync(campaignId, CurrentUserId)) return Forbid();
        return Ok(await exportService.GetEntityHistoryAsync(entityId));
    }
}
