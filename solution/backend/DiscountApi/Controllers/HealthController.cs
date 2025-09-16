using Microsoft.AspNetCore.Mvc;

namespace DiscountApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<object> Get()
    {
        return Ok(new { 
            status = "healthy", 
            service = "DiscountApi",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }

    [HttpGet("ready")]
    public ActionResult<object> Ready()
    {
        return Ok(new { 
            status = "ready", 
            service = "DiscountApi",
            timestamp = DateTime.UtcNow
        });
    }
}
