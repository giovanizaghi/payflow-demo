using Microsoft.AspNetCore.Mvc;

namespace PayFlow.PointsApi.Controllers;

[ApiController]
[Route("api/points")]
public class PointsController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { message = "Points API is running" });
    }
}
