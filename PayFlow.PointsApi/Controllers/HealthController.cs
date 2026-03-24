using Microsoft.AspNetCore.Mvc;

namespace PayFlow.PointsApi.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            service = "PayFlow.PointsApi",
            timestamp = DateTime.UtcNow
        });
    }
}
