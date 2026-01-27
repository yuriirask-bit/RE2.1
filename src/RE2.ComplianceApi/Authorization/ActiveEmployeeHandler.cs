using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace RE2.ComplianceApi.Authorization;

/// <summary>
/// T173: Authorization handler that validates the user has an active employee status.
/// Checks for the "employee_status" claim with value "active" (case-insensitive).
/// This claim is expected to be provided by Azure AD via custom claims mapping
/// or directory extensions synced from HR systems.
/// </summary>
public class ActiveEmployeeHandler : AuthorizationHandler<ActiveEmployeeRequirement>
{
    /// <summary>
    /// The claim type used to determine employee status.
    /// </summary>
    public const string EmployeeStatusClaimType = "employee_status";

    /// <summary>
    /// The expected claim value for active employees.
    /// </summary>
    public const string ActiveStatus = "active";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveEmployeeRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var employeeStatusClaim = context.User.FindFirst(EmployeeStatusClaimType);
        if (employeeStatusClaim != null &&
            string.Equals(employeeStatusClaim.Value, ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
