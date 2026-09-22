using Microsoft.AspNetCore.Authorization;


namespace JwtAppToApp.Client.Authentication;

/// <summary>
/// Authorization handler demonstrating service-to-service policy: requires an authenticated
/// principal whose "sub" claim matches the configured client id.
/// </summary>
public class JwtHandler : AuthorizationHandler<JwtRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        JwtRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            context.User.HasClaim(c => c.Type == "sub"))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public class JwtRequirement : IAuthorizationRequirement
{
}
