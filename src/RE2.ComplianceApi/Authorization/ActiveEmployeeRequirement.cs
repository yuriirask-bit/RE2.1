using Microsoft.AspNetCore.Authorization;

namespace RE2.ComplianceApi.Authorization;

/// <summary>
/// T172: Custom authorization requirement that verifies the user is an active employee.
/// Per research.md section 6 and FR-031: ensures only active employees can perform
/// sensitive compliance operations like managing licences or approving overrides.
/// </summary>
public class ActiveEmployeeRequirement : IAuthorizationRequirement
{
}
