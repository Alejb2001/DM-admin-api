using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace DmAdminApi.Common.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
