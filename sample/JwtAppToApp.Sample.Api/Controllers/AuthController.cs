using JwtAppToApp.Client.Abstractions;
using JwtAppToApp.Client.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JwtAppToApp.Sample.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ITokenProvider _tokenProvider;

    public AuthController(ITokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    [HttpPost("get-token")]
    public async Task<IActionResult> GetToken(CancellationToken cancellationToken)
    {
        try
        {
            var token = await _tokenProvider.GetTokenAsync(cancellationToken);

            return Ok(new
            {
                accessToken = token
            });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new
            {
                error = "خطا در دریافت توکن از API اصلی",
                detail = ex.Message
            });
        }
    }
}
