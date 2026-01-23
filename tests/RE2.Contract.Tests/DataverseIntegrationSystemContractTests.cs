using FluentAssertions;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.D365FinanceOperations.Models;

namespace RE2.Contract.Tests;

/// <summary>
/// Contract tests for Dataverse IntegrationSystem entity.
/// T047a-b verification: Verifies DTO-to-domain and domain-to-DTO mapping contracts for IntegrationSystem.
/// </summary>
public class DataverseIntegrationSystemContractTests
{
    #region DTO Field Naming Convention Tests

    [Fact]
    public void IntegrationSystemDto_ShouldHave_DataverseFieldNamingConvention()
    {
        // Arrange & Act
        var dtoType = typeof(IntegrationSystemDto);
        var properties = dtoType.GetProperties();

        // Assert - all properties should follow phr_ prefix convention for Dataverse
        properties.Should().Contain(p => p.Name == "phr_integrationsystemid");
        properties.Should().Contain(p => p.Name == "phr_systemname");
        properties.Should().Contain(p => p.Name == "phr_systemtype");
        properties.Should().Contain(p => p.Name == "phr_apikeyhash");
        properties.Should().Contain(p => p.Name == "phr_oauthclientid");
        properties.Should().Contain(p => p.Name == "phr_authorizedendpoints");
        properties.Should().Contain(p => p.Name == "phr_ipwhitelist");
        properties.Should().Contain(p => p.Name == "phr_isactive");
        properties.Should().Contain(p => p.Name == "phr_contactperson");
        properties.Should().Contain(p => p.Name == "phr_createddate");
        properties.Should().Contain(p => p.Name == "phr_modifieddate");
    }

    [Fact]
    public void IntegrationSystemDto_PrimaryKey_ShouldBeGuid()
    {
        // Arrange
        var dtoType = typeof(IntegrationSystemDto);
        var primaryKeyProperty = dtoType.GetProperty("phr_integrationsystemid");

        // Assert
        primaryKeyProperty.Should().NotBeNull();
        primaryKeyProperty!.PropertyType.Should().Be(typeof(Guid));
    }

    #endregion

    #region DTO to Domain Model Mapping Tests

    [Fact]
    public void ToDomainModel_ShouldMapAllFields_WhenAllFieldsPopulated()
    {
        // Arrange
        var systemId = Guid.NewGuid();
        var createdDate = DateTime.UtcNow.AddMonths(-6);
        var modifiedDate = DateTime.UtcNow;

        var dto = new IntegrationSystemDto
        {
            phr_integrationsystemid = systemId,
            phr_systemname = "SAP ERP Production",
            phr_systemtype = (int)IntegrationSystemType.ERP,
            phr_apikeyhash = "hashed-api-key-123",
            phr_oauthclientid = "sap-erp-client-id",
            phr_authorizedendpoints = "/api/v1/transactions/validate,/api/v1/customers",
            phr_ipwhitelist = "192.168.1.100,192.168.1.101",
            phr_isactive = true,
            phr_contactperson = "jan.devries@company.nl",
            phr_createddate = createdDate,
            phr_modifieddate = modifiedDate
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.IntegrationSystemId.Should().Be(systemId);
        domainModel.SystemName.Should().Be("SAP ERP Production");
        domainModel.SystemType.Should().Be(IntegrationSystemType.ERP);
        domainModel.ApiKeyHash.Should().Be("hashed-api-key-123");
        domainModel.OAuthClientId.Should().Be("sap-erp-client-id");
        domainModel.AuthorizedEndpoints.Should().Be("/api/v1/transactions/validate,/api/v1/customers");
        domainModel.IpWhitelist.Should().Be("192.168.1.100,192.168.1.101");
        domainModel.IsActive.Should().BeTrue();
        domainModel.ContactPerson.Should().Be("jan.devries@company.nl");
        domainModel.CreatedDate.Should().Be(createdDate);
        domainModel.ModifiedDate.Should().Be(modifiedDate);
    }

    [Fact]
    public void ToDomainModel_ShouldHandleNullableFields_WhenFieldsAreNull()
    {
        // Arrange
        var dto = new IntegrationSystemDto
        {
            phr_integrationsystemid = Guid.NewGuid(),
            phr_systemname = null,
            phr_systemtype = (int)IntegrationSystemType.CustomSystem,
            phr_apikeyhash = null,
            phr_oauthclientid = null,
            phr_authorizedendpoints = null,
            phr_ipwhitelist = null,
            phr_isactive = false,
            phr_contactperson = null,
            phr_createddate = DateTime.UtcNow,
            phr_modifieddate = DateTime.UtcNow
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.SystemName.Should().BeEmpty();
        domainModel.ApiKeyHash.Should().BeNull();
        domainModel.OAuthClientId.Should().BeNull();
        domainModel.AuthorizedEndpoints.Should().BeNull();
        domainModel.IpWhitelist.Should().BeNull();
        domainModel.ContactPerson.Should().BeNull();
    }

    [Theory]
    [InlineData(1, IntegrationSystemType.ERP)]
    [InlineData(2, IntegrationSystemType.OrderManagement)]
    [InlineData(3, IntegrationSystemType.WMS)]
    [InlineData(4, IntegrationSystemType.CustomSystem)]
    public void ToDomainModel_ShouldMapSystemType_Correctly(int dtoValue, IntegrationSystemType expected)
    {
        // Arrange
        var dto = new IntegrationSystemDto
        {
            phr_integrationsystemid = Guid.NewGuid(),
            phr_systemname = "Test System",
            phr_systemtype = dtoValue,
            phr_isactive = false,
            phr_createddate = DateTime.UtcNow,
            phr_modifieddate = DateTime.UtcNow
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.SystemType.Should().Be(expected);
    }

    #endregion

    #region Domain Model to DTO Mapping Tests

    [Fact]
    public void FromDomainModel_ShouldMapAllFields_WhenAllFieldsPopulated()
    {
        // Arrange
        var systemId = Guid.NewGuid();
        var createdDate = DateTime.UtcNow.AddMonths(-6);
        var modifiedDate = DateTime.UtcNow;

        var domainModel = new IntegrationSystem
        {
            IntegrationSystemId = systemId,
            SystemName = "Warehouse Management System",
            SystemType = IntegrationSystemType.WMS,
            ApiKeyHash = "hashed-wms-api-key",
            OAuthClientId = "wms-oauth-client",
            AuthorizedEndpoints = "/api/v1/warehouse/operations/validate",
            IpWhitelist = "10.0.0.50",
            IsActive = true,
            ContactPerson = "wms-admin@warehouse.nl",
            CreatedDate = createdDate,
            ModifiedDate = modifiedDate
        };

        // Act
        var dto = IntegrationSystemDto.FromDomainModel(domainModel);

        // Assert
        dto.phr_integrationsystemid.Should().Be(systemId);
        dto.phr_systemname.Should().Be("Warehouse Management System");
        dto.phr_systemtype.Should().Be((int)IntegrationSystemType.WMS);
        dto.phr_apikeyhash.Should().Be("hashed-wms-api-key");
        dto.phr_oauthclientid.Should().Be("wms-oauth-client");
        dto.phr_authorizedendpoints.Should().Be("/api/v1/warehouse/operations/validate");
        dto.phr_ipwhitelist.Should().Be("10.0.0.50");
        dto.phr_isactive.Should().BeTrue();
        dto.phr_contactperson.Should().Be("wms-admin@warehouse.nl");
        dto.phr_createddate.Should().Be(createdDate);
        dto.phr_modifieddate.Should().Be(modifiedDate);
    }

    [Fact]
    public void FromDomainModel_ShouldHandleNullOptionalFields()
    {
        // Arrange
        var domainModel = new IntegrationSystem
        {
            IntegrationSystemId = Guid.NewGuid(),
            SystemName = "Minimal System",
            SystemType = IntegrationSystemType.CustomSystem,
            ApiKeyHash = null,
            OAuthClientId = null,
            AuthorizedEndpoints = null,
            IpWhitelist = null,
            IsActive = false,
            ContactPerson = null,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var dto = IntegrationSystemDto.FromDomainModel(domainModel);

        // Assert
        dto.phr_apikeyhash.Should().BeNull();
        dto.phr_oauthclientid.Should().BeNull();
        dto.phr_authorizedendpoints.Should().BeNull();
        dto.phr_ipwhitelist.Should().BeNull();
        dto.phr_contactperson.Should().BeNull();
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public void RoundTrip_DomainToDto_AndBack_ShouldPreserveAllData()
    {
        // Arrange
        var original = new IntegrationSystem
        {
            IntegrationSystemId = Guid.NewGuid(),
            SystemName = "D365 F&O Production",
            SystemType = IntegrationSystemType.ERP,
            ApiKeyHash = "secure-hash-abc123",
            OAuthClientId = "d365-oauth-client-prod",
            AuthorizedEndpoints = "/api/v1/*",
            IpWhitelist = "172.16.0.1,172.16.0.2,172.16.0.3",
            IsActive = true,
            ContactPerson = "erp-team@company.com",
            CreatedDate = DateTime.UtcNow.AddYears(-1),
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var dto = IntegrationSystemDto.FromDomainModel(original);
        var roundTripped = dto.ToDomainModel();

        // Assert
        roundTripped.IntegrationSystemId.Should().Be(original.IntegrationSystemId);
        roundTripped.SystemName.Should().Be(original.SystemName);
        roundTripped.SystemType.Should().Be(original.SystemType);
        roundTripped.ApiKeyHash.Should().Be(original.ApiKeyHash);
        roundTripped.OAuthClientId.Should().Be(original.OAuthClientId);
        roundTripped.AuthorizedEndpoints.Should().Be(original.AuthorizedEndpoints);
        roundTripped.IpWhitelist.Should().Be(original.IpWhitelist);
        roundTripped.IsActive.Should().Be(original.IsActive);
        roundTripped.ContactPerson.Should().Be(original.ContactPerson);
        roundTripped.CreatedDate.Should().Be(original.CreatedDate);
        roundTripped.ModifiedDate.Should().Be(original.ModifiedDate);
    }

    [Fact]
    public void RoundTrip_InactiveSystem_ShouldPreserveInactiveState()
    {
        // Arrange
        var original = new IntegrationSystem
        {
            IntegrationSystemId = Guid.NewGuid(),
            SystemName = "Decommissioned Order System",
            SystemType = IntegrationSystemType.OrderManagement,
            OAuthClientId = "legacy-client",
            IsActive = false,
            ContactPerson = "legacy-support@company.com",
            CreatedDate = DateTime.UtcNow.AddYears(-3),
            ModifiedDate = DateTime.UtcNow
        };

        // Act
        var dto = IntegrationSystemDto.FromDomainModel(original);
        var roundTripped = dto.ToDomainModel();

        // Assert
        roundTripped.IsActive.Should().BeFalse();
        roundTripped.SystemName.Should().Be(original.SystemName);
    }

    #endregion

    #region Business Logic Tests

    [Fact]
    public void ToDomainModel_ShouldSupportEndpointAuthorizationCheck()
    {
        // Arrange
        var dto = new IntegrationSystemDto
        {
            phr_integrationsystemid = Guid.NewGuid(),
            phr_systemname = "Test System",
            phr_systemtype = (int)IntegrationSystemType.ERP,
            phr_oauthclientid = "test-client",
            phr_authorizedendpoints = "/api/v1/transactions/validate,/api/v1/licences",
            phr_isactive = true,
            phr_createddate = DateTime.UtcNow,
            phr_modifieddate = DateTime.UtcNow
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.IsEndpointAuthorized("/api/v1/transactions/validate").Should().BeTrue();
        domainModel.IsEndpointAuthorized("/api/v1/licences").Should().BeTrue();
        domainModel.IsEndpointAuthorized("/api/v1/customers").Should().BeFalse();
    }

    [Fact]
    public void ToDomainModel_ShouldSupportIpWhitelistCheck()
    {
        // Arrange
        var dto = new IntegrationSystemDto
        {
            phr_integrationsystemid = Guid.NewGuid(),
            phr_systemname = "Test System",
            phr_systemtype = (int)IntegrationSystemType.WMS,
            phr_oauthclientid = "test-client",
            phr_ipwhitelist = "10.0.0.1,10.0.0.2",
            phr_isactive = true,
            phr_createddate = DateTime.UtcNow,
            phr_modifieddate = DateTime.UtcNow
        };

        // Act
        var domainModel = dto.ToDomainModel();

        // Assert
        domainModel.IsIpAllowed("10.0.0.1").Should().BeTrue();
        domainModel.IsIpAllowed("10.0.0.2").Should().BeTrue();
        domainModel.IsIpAllowed("10.0.0.3").Should().BeFalse();
    }

    [Fact]
    public void ToDomainModel_ValidationShouldWork_AfterMapping()
    {
        // Arrange - Active system with OAuth
        var dto = new IntegrationSystemDto
        {
            phr_integrationsystemid = Guid.NewGuid(),
            phr_systemname = "Valid System",
            phr_systemtype = (int)IntegrationSystemType.ERP,
            phr_oauthclientid = "valid-oauth-client",
            phr_isactive = true,
            phr_createddate = DateTime.UtcNow,
            phr_modifieddate = DateTime.UtcNow
        };

        // Act
        var domainModel = dto.ToDomainModel();
        var validationResult = domainModel.Validate();

        // Assert
        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ToDomainModel_ValidationShouldFail_WhenActiveWithoutAuth()
    {
        // Arrange - Active system without authentication
        var dto = new IntegrationSystemDto
        {
            phr_integrationsystemid = Guid.NewGuid(),
            phr_systemname = "System Without Auth",
            phr_systemtype = (int)IntegrationSystemType.CustomSystem,
            phr_apikeyhash = null,
            phr_oauthclientid = null,
            phr_isactive = true,
            phr_createddate = DateTime.UtcNow,
            phr_modifieddate = DateTime.UtcNow
        };

        // Act
        var domainModel = dto.ToDomainModel();
        var validationResult = domainModel.Validate();

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Violations.Should().Contain(v => v.Message.Contains("ApiKeyHash or OAuthClientId"));
    }

    #endregion
}
