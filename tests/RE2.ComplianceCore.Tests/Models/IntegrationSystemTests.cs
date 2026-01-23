using FluentAssertions;
using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for IntegrationSystem domain model.
/// T047a verification: Tests IntegrationSystem per data-model.md entity 27.
/// </summary>
public class IntegrationSystemTests
{
    [Fact]
    public void IntegrationSystem_ShouldHaveCorrectDefaultValues()
    {
        // Arrange & Act
        var system = new IntegrationSystem();

        // Assert
        system.IntegrationSystemId.Should().Be(Guid.Empty);
        system.SystemName.Should().BeEmpty();
        system.IsActive.Should().BeTrue(); // Default is true per data-model.md
        system.ApiKeyHash.Should().BeNull();
        system.OAuthClientId.Should().BeNull();
        system.AuthorizedEndpoints.Should().BeNull();
        system.IpWhitelist.Should().BeNull();
        system.ContactPerson.Should().BeNull();
    }

    [Fact]
    public void Validate_WithValidActiveSystem_ShouldPass()
    {
        // Arrange
        var system = new IntegrationSystem
        {
            IntegrationSystemId = Guid.NewGuid(),
            SystemName = "SAP ERP",
            SystemType = IntegrationSystemType.ERP,
            OAuthClientId = "sap-client-id",
            IsActive = true
        };

        // Act
        var result = system.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptySystemName_ShouldFail()
    {
        // Arrange
        var system = new IntegrationSystem
        {
            IntegrationSystemId = Guid.NewGuid(),
            SystemName = "",
            SystemType = IntegrationSystemType.ERP,
            OAuthClientId = "client-id",
            IsActive = true
        };

        // Act
        var result = system.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("SystemName is required"));
    }

    [Fact]
    public void Validate_WithActiveSystemNoAuth_ShouldFail()
    {
        // Arrange - active system with no authentication
        var system = new IntegrationSystem
        {
            IntegrationSystemId = Guid.NewGuid(),
            SystemName = "Test System",
            SystemType = IntegrationSystemType.CustomSystem,
            IsActive = true,
            ApiKeyHash = null,
            OAuthClientId = null
        };

        // Act
        var result = system.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("ApiKeyHash or OAuthClientId"));
    }

    [Fact]
    public void Validate_WithInactiveSystemNoAuth_ShouldPass()
    {
        // Arrange - inactive system with no authentication (allowed)
        var system = new IntegrationSystem
        {
            IntegrationSystemId = Guid.NewGuid(),
            SystemName = "Disabled System",
            SystemType = IntegrationSystemType.CustomSystem,
            IsActive = false,
            ApiKeyHash = null,
            OAuthClientId = null
        };

        // Act
        var result = system.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithApiKeyAuth_ShouldPass()
    {
        // Arrange
        var system = new IntegrationSystem
        {
            IntegrationSystemId = Guid.NewGuid(),
            SystemName = "WMS System",
            SystemType = IntegrationSystemType.WMS,
            ApiKeyHash = "hashed-api-key",
            IsActive = true
        };

        // Act
        var result = system.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("/api/v1/transactions/validate", true)]
    [InlineData("/api/v1/customers/lookup", false)]
    public void IsEndpointAuthorized_WithRestrictedEndpoints_ShouldValidateCorrectly(string endpoint, bool expected)
    {
        // Arrange
        var system = new IntegrationSystem
        {
            SystemName = "Test",
            AuthorizedEndpoints = "/api/v1/transactions/validate,/api/v1/licences"
        };

        // Act
        var result = system.IsEndpointAuthorized(endpoint);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsEndpointAuthorized_WithNoRestrictions_ShouldAllowAll()
    {
        // Arrange
        var system = new IntegrationSystem
        {
            SystemName = "Test",
            AuthorizedEndpoints = null
        };

        // Act & Assert
        system.IsEndpointAuthorized("/any/endpoint").Should().BeTrue();
        system.IsEndpointAuthorized("/api/v1/transactions/validate").Should().BeTrue();
    }

    [Fact]
    public void IsEndpointAuthorized_WithWildcard_ShouldMatchPrefix()
    {
        // Arrange
        var system = new IntegrationSystem
        {
            SystemName = "Test",
            AuthorizedEndpoints = "/api/v1/*"
        };

        // Act & Assert
        system.IsEndpointAuthorized("/api/v1/transactions/validate").Should().BeTrue();
        system.IsEndpointAuthorized("/api/v1/customers/123").Should().BeTrue();
        system.IsEndpointAuthorized("/api/v2/test").Should().BeFalse();
    }

    [Theory]
    [InlineData("192.168.1.1", true)]
    [InlineData("10.0.0.1", false)]
    public void IsIpAllowed_WithRestrictedIps_ShouldValidateCorrectly(string ip, bool expected)
    {
        // Arrange
        var system = new IntegrationSystem
        {
            SystemName = "Test",
            IpWhitelist = "192.168.1.1,192.168.1.2"
        };

        // Act
        var result = system.IsIpAllowed(ip);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsIpAllowed_WithNoRestrictions_ShouldAllowAll()
    {
        // Arrange
        var system = new IntegrationSystem
        {
            SystemName = "Test",
            IpWhitelist = null
        };

        // Act & Assert
        system.IsIpAllowed("192.168.1.1").Should().BeTrue();
        system.IsIpAllowed("10.0.0.1").Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        // Arrange
        var system = new IntegrationSystem
        {
            SystemName = "Test",
            IsActive = true
        };

        // Act
        system.Deactivate();

        // Assert
        system.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        // Arrange
        var system = new IntegrationSystem
        {
            SystemName = "Test",
            IsActive = false
        };

        // Act
        system.Activate();

        // Assert
        system.IsActive.Should().BeTrue();
    }

    [Fact]
    public void GetAuthorizedEndpointsArray_ShouldSplitCorrectly()
    {
        // Arrange
        var system = new IntegrationSystem
        {
            SystemName = "Test",
            AuthorizedEndpoints = "/api/v1/transactions, /api/v1/licences , /api/v1/customers"
        };

        // Act
        var endpoints = system.GetAuthorizedEndpointsArray();

        // Assert
        endpoints.Should().HaveCount(3);
        endpoints.Should().Contain("/api/v1/transactions");
        endpoints.Should().Contain("/api/v1/licences");
        endpoints.Should().Contain("/api/v1/customers");
    }

    [Fact]
    public void GetAuthorizedEndpointsArray_WithNull_ShouldReturnEmptyArray()
    {
        // Arrange
        var system = new IntegrationSystem
        {
            SystemName = "Test",
            AuthorizedEndpoints = null
        };

        // Act
        var endpoints = system.GetAuthorizedEndpointsArray();

        // Assert
        endpoints.Should().BeEmpty();
    }

    [Fact]
    public void GetIpWhitelistArray_ShouldSplitCorrectly()
    {
        // Arrange
        var system = new IntegrationSystem
        {
            SystemName = "Test",
            IpWhitelist = "192.168.1.1, 10.0.0.1 , 172.16.0.1"
        };

        // Act
        var ips = system.GetIpWhitelistArray();

        // Assert
        ips.Should().HaveCount(3);
        ips.Should().Contain("192.168.1.1");
        ips.Should().Contain("10.0.0.1");
        ips.Should().Contain("172.16.0.1");
    }

    [Theory]
    [InlineData(IntegrationSystemType.ERP)]
    [InlineData(IntegrationSystemType.OrderManagement)]
    [InlineData(IntegrationSystemType.WMS)]
    [InlineData(IntegrationSystemType.CustomSystem)]
    public void SystemType_ShouldSupportAllValues(IntegrationSystemType type)
    {
        // Arrange
        var system = new IntegrationSystem
        {
            SystemName = "Test",
            SystemType = type,
            OAuthClientId = "client-id"
        };

        // Act & Assert
        system.SystemType.Should().Be(type);
        system.Validate().IsValid.Should().BeTrue();
    }
}
