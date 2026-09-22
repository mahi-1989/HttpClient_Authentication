using JwtAppToApp.Client.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtAppToApp.Sample.Api.Controllers;

[ApiController]
[Route("api/demo")]
[Authorize]
public class CallerDemoController : ControllerBase
{
    private readonly IApiClient _apiClient;
    public CallerDemoController(IApiClient apiClient) => _apiClient = apiClient;

    /// <summary>Calls a downstream service using the library's IApiClient (auto JWT).</summary>
    [HttpGet("call-downstream")]
    public async Task<IActionResult> CallDownstream(CancellationToken ct)
    {
        var result = await _apiClient.GetAsync<object>("api/data", ct);
        return Ok(result);
    }
}
