using Microsoft.AspNetCore.Mvc;

namespace TicketSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            service = "TicketSystem.Api",
            status = "healthy"
        });
    }
}
