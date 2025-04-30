using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly string _policyName;

    public HangfireAuthorizationFilter(string policyName)
    {
        _policyName = policyName;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Allow only authenticated users
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            return false;

        // Resolve IAuthorizationService to check policy
        var authService = httpContext.RequestServices.GetService(typeof(IAuthorizationService)) as IAuthorizationService;

        var result = authService?.AuthorizeAsync(httpContext.User, null, _policyName).Result;
        return result?.Succeeded ?? false;
    }
}
