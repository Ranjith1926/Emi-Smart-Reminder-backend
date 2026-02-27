using Hangfire.Dashboard;

namespace EMI_REMAINDER.Middleware;

/// <summary>
/// Allow Hangfire dashboard in Development, block in Production unless authenticated.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        // Allow in Development; in Production require authenticated user
        return httpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsDevelopment()
            || httpContext.User.Identity?.IsAuthenticated == true;
    }
}
