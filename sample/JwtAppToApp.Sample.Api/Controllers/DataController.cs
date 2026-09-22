using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtAppToApp.Sample.Api.Controllers;

[ApiController]
[Route("api/data")]
[Authorize]
public class DataController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { message = "Hello from secure app-to-app API!", time = DateTime.UtcNow });
}
