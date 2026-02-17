using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.ComplianceCore.Services.GdpCompliance;

namespace RE2.ComplianceCore.Tests.Services;

/// <summary>
/// Unit tests for GdpOperationalService.
/// T261: Tests per US11 (FR-046, FR-047, FR-048).
/// </summary>
public class GdpOperationalServiceTests
{
    private readonly Mock<IGdpComplianceService> _complianceServiceMock;
    private readonly Mock<IGdpEquipmentRepository> _equipmentRepoMock;
    private readonly Mock<ILogger<GdpOperationalService>> _loggerMock;
    private readonly GdpOperationalService _service;

    public GdpOperationalServiceTests()
    {
        _complianceServiceMock = new Mock<IGdpComplianceService>();
        _equipmentRepoMock = new Mock<IGdpEquipmentRepository>();
        _loggerMock = new Mock<ILogger<GdpOperationalService>>();

        _service = new GdpOperationalService(
            _complianceServiceMock.Object,
            _equipmentRepoMock.Object,
            _loggerMock.Object);
    }

    #region ValidateSiteAssignment (FR-046)

    [Fact]
    public async Task ValidateSiteAssignment_WithActiveSiteAndValidWda_ShouldAllow()
    {
        var site = new GdpSite { IsGdpActive = true };
        _complianceServiceMock.Setup(s => s.GetGdpSiteAsync("WH01", "phr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(site);
        _complianceServiceMock.Setup(s => s.GetWdaCoverageAsync("WH01", "phr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new GdpSiteWdaCoverage { EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-6), ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(6) } });

        var (isAllowed, reason) = await _service.ValidateSiteAssignmentAsync("WH01", "phr");

        isAllowed.Should().BeTrue();
        reason.Should().Contain("valid WDA");
    }

    [Fact]
    public async Task ValidateSiteAssignment_WithNoGdpSite_ShouldBlock()
    {
        _complianceServiceMock.Setup(s => s.GetGdpSiteAsync("WH99", "phr", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpSite?)null);

        var (isAllowed, reason) = await _service.ValidateSiteAssignmentAsync("WH99", "phr");

        isAllowed.Should().BeFalse();
        reason.Should().Contain("not GDP-configured");
    }

    [Fact]
    public async Task ValidateSiteAssignment_WithInactiveGdpSite_ShouldBlock()
    {
        var site = new GdpSite { IsGdpActive = false };
        _complianceServiceMock.Setup(s => s.GetGdpSiteAsync("WH01", "phr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(site);

        var (isAllowed, reason) = await _service.ValidateSiteAssignmentAsync("WH01", "phr");

        isAllowed.Should().BeFalse();
        reason.Should().Contain("not GDP-configured");
    }

    [Fact]
    public async Task ValidateSiteAssignment_WithNoValidWda_ShouldBlock()
    {
        var site = new GdpSite { IsGdpActive = true };
        _complianceServiceMock.Setup(s => s.GetGdpSiteAsync("WH01", "phr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(site);
        _complianceServiceMock.Setup(s => s.GetWdaCoverageAsync("WH01", "phr", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<GdpSiteWdaCoverage>());

        var (isAllowed, reason) = await _service.ValidateSiteAssignmentAsync("WH01", "phr");

        isAllowed.Should().BeFalse();
        reason.Should().Contain("no valid WDA");
    }

    #endregion

    #region ValidateProviderAssignment (FR-046)

    [Fact]
    public async Task ValidateProviderAssignment_WithQualifiedProvider_ShouldAllow()
    {
        var providerId = Guid.NewGuid();
        var provider = new GdpServiceProvider { ProviderId = providerId, ProviderName = "Test Provider" };
        _complianceServiceMock.Setup(s => s.GetProviderAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);
        _complianceServiceMock.Setup(s => s.IsPartnerQualifiedAsync(GdpCredentialEntityType.ServiceProvider, providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var (isAllowed, reason) = await _service.ValidateProviderAssignmentAsync(providerId);

        isAllowed.Should().BeTrue();
        reason.Should().Contain("GDP-qualified");
    }

    [Fact]
    public async Task ValidateProviderAssignment_WithNotFoundProvider_ShouldBlock()
    {
        var providerId = Guid.NewGuid();
        _complianceServiceMock.Setup(s => s.GetProviderAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpServiceProvider?)null);

        var (isAllowed, reason) = await _service.ValidateProviderAssignmentAsync(providerId);

        isAllowed.Should().BeFalse();
        reason.Should().Contain("not found");
    }

    [Fact]
    public async Task ValidateProviderAssignment_WithUnqualifiedProvider_ShouldBlock()
    {
        var providerId = Guid.NewGuid();
        var provider = new GdpServiceProvider { ProviderId = providerId, ProviderName = "Bad Provider" };
        _complianceServiceMock.Setup(s => s.GetProviderAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);
        _complianceServiceMock.Setup(s => s.IsPartnerQualifiedAsync(GdpCredentialEntityType.ServiceProvider, providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var (isAllowed, reason) = await _service.ValidateProviderAssignmentAsync(providerId);

        isAllowed.Should().BeFalse();
        reason.Should().Contain("not GDP-qualified");
    }

    #endregion

    #region GetApprovedProviders (FR-047)

    [Fact]
    public async Task GetApprovedProviders_ShouldReturnOnlyQualified()
    {
        var qualifiedId = Guid.NewGuid();
        var unqualifiedId = Guid.NewGuid();
        var providers = new[]
        {
            new GdpServiceProvider { ProviderId = qualifiedId, ProviderName = "Qualified" },
            new GdpServiceProvider { ProviderId = unqualifiedId, ProviderName = "Unqualified" }
        };
        _complianceServiceMock.Setup(s => s.GetAllProvidersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);
        _complianceServiceMock.Setup(s => s.IsPartnerQualifiedAsync(GdpCredentialEntityType.ServiceProvider, qualifiedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _complianceServiceMock.Setup(s => s.IsPartnerQualifiedAsync(GdpCredentialEntityType.ServiceProvider, unqualifiedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = (await _service.GetApprovedProvidersAsync()).ToList();

        result.Should().HaveCount(1);
        result[0].ProviderName.Should().Be("Qualified");
    }

    [Fact]
    public async Task GetApprovedProviders_WithTempControlFilter_ShouldFilterByCapability()
    {
        var withTempId = Guid.NewGuid();
        var withoutTempId = Guid.NewGuid();
        var providers = new[]
        {
            new GdpServiceProvider { ProviderId = withTempId, ProviderName = "TempControl", TemperatureControlledCapability = true },
            new GdpServiceProvider { ProviderId = withoutTempId, ProviderName = "NoTemp", TemperatureControlledCapability = false }
        };
        _complianceServiceMock.Setup(s => s.GetAllProvidersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);
        _complianceServiceMock.Setup(s => s.IsPartnerQualifiedAsync(GdpCredentialEntityType.ServiceProvider, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = (await _service.GetApprovedProvidersAsync(requireTempControl: true)).ToList();

        result.Should().HaveCount(1);
        result[0].ProviderName.Should().Be("TempControl");
    }

    #endregion

    #region Equipment CRUD (FR-048)

    [Fact]
    public async Task CreateEquipment_WithValidRecord_ShouldSucceed()
    {
        var equipment = CreateValidEquipment();
        var expectedId = Guid.NewGuid();
        _equipmentRepoMock.Setup(r => r.CreateAsync(It.IsAny<GdpEquipmentQualification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        var (id, result) = await _service.CreateEquipmentAsync(equipment);

        id.Should().Be(expectedId);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateEquipment_WithInvalidRecord_ShouldReturnValidationError()
    {
        var equipment = new GdpEquipmentQualification(); // Missing required fields

        var (id, result) = await _service.CreateEquipmentAsync(equipment);

        id.Should().BeNull();
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateEquipment_WithValidRecord_ShouldSucceed()
    {
        var equipment = CreateValidEquipment();
        _equipmentRepoMock.Setup(r => r.GetByIdAsync(equipment.EquipmentQualificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment);

        var result = await _service.UpdateEquipmentAsync(equipment);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateEquipment_WithNonExistent_ShouldReturnNotFound()
    {
        var equipment = CreateValidEquipment();
        _equipmentRepoMock.Setup(r => r.GetByIdAsync(equipment.EquipmentQualificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpEquipmentQualification?)null);

        var result = await _service.UpdateEquipmentAsync(equipment);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().ContainSingle(v => v.Message.Contains("not found"));
    }

    [Fact]
    public async Task DeleteEquipment_WithExistingRecord_ShouldSucceed()
    {
        var id = Guid.NewGuid();
        _equipmentRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidEquipment());

        var result = await _service.DeleteEquipmentAsync(id);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteEquipment_WithNonExistent_ShouldReturnNotFound()
    {
        var id = Guid.NewGuid();
        _equipmentRepoMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpEquipmentQualification?)null);

        var result = await _service.DeleteEquipmentAsync(id);

        result.IsValid.Should().BeFalse();
        result.Violations.Should().ContainSingle(v => v.Message.Contains("not found"));
    }

    [Fact]
    public async Task GetEquipmentDueForRequalification_ShouldDelegateToRepository()
    {
        var equipment = new[] { CreateValidEquipment() };
        _equipmentRepoMock.Setup(r => r.GetDueForRequalificationAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment);

        var result = (await _service.GetEquipmentDueForRequalificationAsync(30)).ToList();

        result.Should().HaveCount(1);
    }

    #endregion

    private static GdpEquipmentQualification CreateValidEquipment() => new()
    {
        EquipmentQualificationId = Guid.NewGuid(),
        EquipmentName = "Temperature Vehicle TRK-001",
        EquipmentType = GdpEquipmentType.TemperatureControlledVehicle,
        ProviderId = Guid.NewGuid(),
        QualificationDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
        RequalificationDueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(11),
        QualificationStatus = GdpQualificationStatusType.Qualified,
        QualifiedBy = "Test Engineer",
        CreatedDate = DateTime.UtcNow,
        ModifiedDate = DateTime.UtcNow
    };
}
