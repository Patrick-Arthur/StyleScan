using Microsoft.AspNetCore.Mvc;

namespace StyleScan.Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("API is healthy!");
        }
    }
}
