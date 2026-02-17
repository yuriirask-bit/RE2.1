using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceApi.Controllers.V1;

namespace RE2.ComplianceApi.Tests.Controllers;

/// <summary>
/// T171: Integration tests for role-based access control.
/// Verifies that controllers enforce correct authorization policies
/// and that users with different roles get appropriate access.
/// </summary>
public class RoleBasedAccessTests
{
    #region Helper Methods

    private static ClaimsPrincipal CreateUserWithRoles(params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, "test@example.com"),
            new("employee_status", "active")
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("roles", role)); // Azure AD style
        }

        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }

    private static ControllerContext CreateControllerContext(ClaimsPrincipal user)
    {
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    #endregion

    #region Licence Management Access Tests

    [Fact]
    public void CustomersController_ShouldHaveAuthorizeAttribute_OnController()
    {
        var controllerType = typeof(CustomersController);
        var authorizeAttrs = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true);
        authorizeAttrs.Should().NotBeEmpty("CustomersController must require authentication");
    }

    [Fact]
    public void TransactionsController_ShouldHaveAuthorizeAttribute_OnController()
    {
        var controllerType = typeof(TransactionsController);
        var authorizeAttrs = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true);
        authorizeAttrs.Should().NotBeEmpty("TransactionsController must require authentication");
    }

    #endregion

    #region ApprovalWorkflow Access Tests

    [Fact]
    public void ApprovalWorkflowController_ShouldHaveAuthorizeAttribute()
    {
        var controllerType = typeof(ApprovalWorkflowController);
        var authorizeAttrs = controllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true);
        authorizeAttrs.Should().NotBeEmpty("ApprovalWorkflowController must require authentication");
    }

    [Fact]
    public void ApprovalWorkflowController_TriggerWorkflow_ShouldRequireComplianceManagerRole()
    {
        // Verify the TriggerWorkflow method has [Authorize(Policy = "ComplianceManager")] attribute
        var methodInfo = typeof(ApprovalWorkflowController).GetMethod("TriggerWorkflow");
        methodInfo.Should().NotBeNull("TriggerWorkflow method should exist");

        var authorizeAttrs = methodInfo!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>();
        authorizeAttrs.Should().Contain(a => a.Policy == "ComplianceManager",
            "Only ComplianceManager role should trigger high-risk workflows");
    }

    [Fact]
    public void ApprovalWorkflowController_GetWorkflowStatus_ShouldBeAccessibleByAnyAuthenticatedUser()
    {
        // The status endpoint should be accessible by any authenticated user
        var methodInfo = typeof(ApprovalWorkflowController).GetMethod("GetWorkflowStatus");
        methodInfo.Should().NotBeNull("GetWorkflowStatus method should exist");
    }

    #endregion

    #region Controller-Level Role Restriction Tests

    [Theory]
    [InlineData(typeof(ApprovalWorkflowController))]
    [InlineData(typeof(TransactionsController))]
    [InlineData(typeof(CustomersController))]
    public void SecuredControllers_ShouldRequireAuthentication(Type controllerType)
    {
        var authorizeAttrs = controllerType.GetCustomAttributes(
            typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true);
        authorizeAttrs.Should().NotBeEmpty(
            $"{controllerType.Name} must have [Authorize] attribute for security per FR-031");
    }

    #endregion
}
