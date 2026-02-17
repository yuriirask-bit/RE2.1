using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.ComplianceCore.Services.GdpCompliance;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Tests.Services;

/// <summary>
/// T285: Unit tests for GdpComplianceService.
/// Tests sites, providers, credentials, reviews, verifications, inspections, findings, CAPAs, documents, and WDA coverage.
/// </summary>
public class GdpComplianceServiceTests
{
    private readonly Mock<IGdpSiteRepository> _gdpSiteRepoMock;
    private readonly Mock<IGdpCredentialRepository> _gdpCredentialRepoMock;
    private readonly Mock<IGdpInspectionRepository> _gdpInspectionRepoMock;
    private readonly Mock<ICapaRepository> _capaRepoMock;
    private readonly Mock<IGdpDocumentRepository> _gdpDocumentRepoMock;
    private readonly Mock<IDocumentStorage> _documentStorageMock;
    private readonly Mock<ILicenceRepository> _licenceRepoMock;
    private readonly Mock<ILicenceTypeRepository> _licenceTypeRepoMock;
    private readonly Mock<ILogger<GdpComplianceService>> _loggerMock;
    private readonly GdpComplianceService _service;

    public GdpComplianceServiceTests()
    {
        _gdpSiteRepoMock = new Mock<IGdpSiteRepository>();
        _gdpCredentialRepoMock = new Mock<IGdpCredentialRepository>();
        _gdpInspectionRepoMock = new Mock<IGdpInspectionRepository>();
        _capaRepoMock = new Mock<ICapaRepository>();
        _gdpDocumentRepoMock = new Mock<IGdpDocumentRepository>();
        _documentStorageMock = new Mock<IDocumentStorage>();
        _licenceRepoMock = new Mock<ILicenceRepository>();
        _licenceTypeRepoMock = new Mock<ILicenceTypeRepository>();
        _loggerMock = new Mock<ILogger<GdpComplianceService>>();

        _service = new GdpComplianceService(
            _gdpSiteRepoMock.Object,
            _gdpCredentialRepoMock.Object,
            _gdpInspectionRepoMock.Object,
            _capaRepoMock.Object,
            _gdpDocumentRepoMock.Object,
            _documentStorageMock.Object,
            _licenceRepoMock.Object,
            _licenceTypeRepoMock.Object,
            _loggerMock.Object);
    }

    #region Sites Tests

    [Fact]
    public async Task GetAllWarehousesAsync_ReturnsList()
    {
        // Arrange
        var warehouses = new List<GdpSite> { CreateValidGdpSite() };
        _gdpSiteRepoMock.Setup(r => r.GetAllWarehousesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouses);

        // Act
        var result = await _service.GetAllWarehousesAsync();

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetWarehouseAsync_ReturnsSite_WhenFound()
    {
        // Arrange
        var site = CreateValidGdpSite();
        _gdpSiteRepoMock.Setup(r => r.GetWarehouseAsync("WH-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(site);

        // Act
        var result = await _service.GetWarehouseAsync("WH-001", "nlpd");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllGdpSitesAsync_ReturnsConfiguredSites()
    {
        // Arrange
        var sites = new List<GdpSite> { CreateValidGdpSite() };
        _gdpSiteRepoMock.Setup(r => r.GetAllGdpConfiguredSitesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sites);

        // Act
        var result = await _service.GetAllGdpSitesAsync();

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetGdpSiteAsync_ReturnsSite_WhenConfigured()
    {
        // Arrange
        var site = CreateValidGdpSite();
        _gdpSiteRepoMock.Setup(r => r.GetGdpExtensionAsync("WH-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(site);

        // Act
        var result = await _service.GetGdpSiteAsync("WH-001", "nlpd");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ConfigureGdpAsync_Succeeds_WhenValid()
    {
        // Arrange
        var site = CreateValidGdpSite();
        var expectedId = Guid.NewGuid();

        _gdpSiteRepoMock.Setup(r => r.GetWarehouseAsync("WH-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(site);
        _gdpSiteRepoMock.Setup(r => r.GetGdpExtensionAsync("WH-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpSite?)null);
        _gdpSiteRepoMock.Setup(r => r.SaveGdpExtensionAsync(site, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        var (id, result) = await _service.ConfigureGdpAsync(site);

        // Assert
        result.IsValid.Should().BeTrue();
        id.Should().Be(expectedId);
    }

    [Fact]
    public async Task ConfigureGdpAsync_Fails_WhenValidationFails()
    {
        // Arrange
        var site = new GdpSite(); // Empty — no WarehouseId or DataAreaId

        // Act
        var (id, result) = await _service.ConfigureGdpAsync(site);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
    }

    [Fact]
    public async Task ConfigureGdpAsync_Fails_WhenAlreadyConfigured()
    {
        // Arrange
        var site = CreateValidGdpSite();
        _gdpSiteRepoMock.Setup(r => r.GetWarehouseAsync("WH-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(site);
        _gdpSiteRepoMock.Setup(r => r.GetGdpExtensionAsync("WH-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(site);

        // Act
        var (id, result) = await _service.ConfigureGdpAsync(site);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.Message.Contains("already configured"));
    }

    [Fact]
    public async Task ConfigureGdpAsync_Fails_WhenWarehouseNotFound()
    {
        // Arrange
        var site = CreateValidGdpSite();
        _gdpSiteRepoMock.Setup(r => r.GetWarehouseAsync("WH-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpSite?)null);

        // Act
        var (id, result) = await _service.ConfigureGdpAsync(site);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
    }

    [Fact]
    public async Task UpdateGdpConfigAsync_Succeeds_WhenFound()
    {
        // Arrange
        var site = CreateValidGdpSite();
        var existing = CreateValidGdpSite();
        existing.GdpExtensionId = Guid.NewGuid();

        _gdpSiteRepoMock.Setup(r => r.GetGdpExtensionAsync("WH-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _service.UpdateGdpConfigAsync(site);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateGdpConfigAsync_Fails_WhenNotFound()
    {
        // Arrange
        var site = CreateValidGdpSite();
        _gdpSiteRepoMock.Setup(r => r.GetGdpExtensionAsync("WH-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpSite?)null);

        // Act
        var result = await _service.UpdateGdpConfigAsync(site);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveGdpConfigAsync_Succeeds_WhenFound()
    {
        // Arrange
        var existing = CreateValidGdpSite();
        _gdpSiteRepoMock.Setup(r => r.GetGdpExtensionAsync("WH-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _service.RemoveGdpConfigAsync("WH-001", "nlpd");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveGdpConfigAsync_Fails_WhenNotFound()
    {
        // Arrange
        _gdpSiteRepoMock.Setup(r => r.GetGdpExtensionAsync("WH-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpSite?)null);

        // Act
        var result = await _service.RemoveGdpConfigAsync("WH-001", "nlpd");

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region Providers Tests

    [Fact]
    public async Task GetAllProvidersAsync_ReturnsList()
    {
        // Arrange
        var providers = new List<GdpServiceProvider> { CreateValidProvider() };
        _gdpCredentialRepoMock.Setup(r => r.GetAllProvidersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        // Act
        var result = await _service.GetAllProvidersAsync();

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetProviderAsync_ReturnsProvider_WhenFound()
    {
        // Arrange
        var provider = CreateValidProvider();
        _gdpCredentialRepoMock.Setup(r => r.GetProviderAsync(provider.ProviderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        // Act
        var result = await _service.GetProviderAsync(provider.ProviderId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateProviderAsync_Succeeds_WhenValid()
    {
        // Arrange
        var provider = CreateValidProvider();
        var expectedId = Guid.NewGuid();
        _gdpCredentialRepoMock.Setup(r => r.CreateProviderAsync(provider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        var (id, result) = await _service.CreateProviderAsync(provider);

        // Assert
        result.IsValid.Should().BeTrue();
        id.Should().Be(expectedId);
    }

    [Fact]
    public async Task CreateProviderAsync_Fails_WhenValidationFails()
    {
        // Arrange
        var provider = new GdpServiceProvider(); // Empty — ProviderName missing

        // Act
        var (id, result) = await _service.CreateProviderAsync(provider);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
    }

    [Fact]
    public async Task UpdateProviderAsync_Succeeds_WhenFound()
    {
        // Arrange
        var provider = CreateValidProvider();
        _gdpCredentialRepoMock.Setup(r => r.GetProviderAsync(provider.ProviderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        // Act
        var result = await _service.UpdateProviderAsync(provider);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateProviderAsync_Fails_WhenNotFound()
    {
        // Arrange
        var provider = CreateValidProvider();
        _gdpCredentialRepoMock.Setup(r => r.GetProviderAsync(provider.ProviderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpServiceProvider?)null);

        // Act
        var result = await _service.UpdateProviderAsync(provider);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProviderAsync_Succeeds_WhenFound()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        _gdpCredentialRepoMock.Setup(r => r.GetProviderAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidProvider());

        // Act
        var result = await _service.DeleteProviderAsync(providerId);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProviderAsync_Fails_WhenNotFound()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        _gdpCredentialRepoMock.Setup(r => r.GetProviderAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpServiceProvider?)null);

        // Act
        var result = await _service.DeleteProviderAsync(providerId);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region Credentials Tests

    [Fact]
    public async Task GetCredentialsByEntityAsync_ReturnsList()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var credentials = new List<GdpCredential> { CreateValidCredential() };
        _gdpCredentialRepoMock.Setup(r => r.GetCredentialsByEntityAsync(
                GdpCredentialEntityType.Supplier, entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credentials);

        // Act
        var result = await _service.GetCredentialsByEntityAsync(GdpCredentialEntityType.Supplier, entityId);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCredentialAsync_ReturnsCredential_WhenFound()
    {
        // Arrange
        var credential = CreateValidCredential();
        _gdpCredentialRepoMock.Setup(r => r.GetCredentialAsync(credential.CredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Act
        var result = await _service.GetCredentialAsync(credential.CredentialId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCredentialAsync_Succeeds_WhenValid()
    {
        // Arrange
        var credential = CreateValidCredential();
        var expectedId = Guid.NewGuid();
        _gdpCredentialRepoMock.Setup(r => r.CreateCredentialAsync(credential, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        var (id, result) = await _service.CreateCredentialAsync(credential);

        // Assert
        result.IsValid.Should().BeTrue();
        id.Should().Be(expectedId);
    }

    [Fact]
    public async Task CreateCredentialAsync_Fails_WhenValidationFails()
    {
        // Arrange
        var credential = new GdpCredential(); // Empty — EntityId and WdaNumber/GdpCertificateNumber missing

        // Act
        var (id, result) = await _service.CreateCredentialAsync(credential);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCredentialAsync_Succeeds_WhenFound()
    {
        // Arrange
        var credential = CreateValidCredential();
        _gdpCredentialRepoMock.Setup(r => r.GetCredentialAsync(credential.CredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Act
        var result = await _service.UpdateCredentialAsync(credential);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCredentialAsync_Fails_WhenNotFound()
    {
        // Arrange
        var credential = CreateValidCredential();
        _gdpCredentialRepoMock.Setup(r => r.GetCredentialAsync(credential.CredentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpCredential?)null);

        // Act
        var result = await _service.UpdateCredentialAsync(credential);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCredentialAsync_Succeeds_WhenFound()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        _gdpCredentialRepoMock.Setup(r => r.GetCredentialAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidCredential());

        // Act
        var result = await _service.DeleteCredentialAsync(credentialId);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCredentialAsync_Fails_WhenNotFound()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        _gdpCredentialRepoMock.Setup(r => r.GetCredentialAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpCredential?)null);

        // Act
        var result = await _service.DeleteCredentialAsync(credentialId);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region Qualification Reviews Tests

    [Fact]
    public async Task GetReviewsByEntityAsync_ReturnsList()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var reviews = new List<QualificationReview> { CreateValidReview(entityId) };
        _gdpCredentialRepoMock.Setup(r => r.GetReviewsByEntityAsync(
                ReviewEntityType.ServiceProvider, entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        // Act
        var result = await _service.GetReviewsByEntityAsync(ReviewEntityType.ServiceProvider, entityId);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task RecordReviewAsync_Succeeds_WhenValid()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var review = CreateValidReview(providerId);
        var provider = CreateValidProvider();
        provider.ProviderId = providerId;
        var expectedId = Guid.NewGuid();

        _gdpCredentialRepoMock.Setup(r => r.GetProviderAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);
        _gdpCredentialRepoMock.Setup(r => r.CreateReviewAsync(review, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        var (id, result) = await _service.RecordReviewAsync(review);

        // Assert
        result.IsValid.Should().BeTrue();
        id.Should().Be(expectedId);
    }

    [Fact]
    public async Task RecordReviewAsync_Fails_WhenValidationFails()
    {
        // Arrange
        var review = new QualificationReview(); // Empty — EntityId and ReviewerName missing

        // Act
        var (id, result) = await _service.RecordReviewAsync(review);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
    }

    [Fact]
    public async Task RecordReviewAsync_UpdatesProviderStatus_WhenServiceProvider()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var review = CreateValidReview(providerId);
        review.ReviewOutcome = ReviewOutcome.Approved;
        var provider = CreateValidProvider();
        provider.ProviderId = providerId;
        provider.QualificationStatus = GdpQualificationStatus.UnderReview;

        _gdpCredentialRepoMock.Setup(r => r.GetProviderAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);
        _gdpCredentialRepoMock.Setup(r => r.CreateReviewAsync(review, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _service.RecordReviewAsync(review);

        // Assert
        _gdpCredentialRepoMock.Verify(r => r.UpdateProviderAsync(
            It.Is<GdpServiceProvider>(p => p.QualificationStatus == GdpQualificationStatus.Approved),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Verifications Tests

    [Fact]
    public async Task GetVerificationsByCredentialAsync_ReturnsList()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var verifications = new List<GdpCredentialVerification> { CreateValidGdpVerification(credentialId) };
        _gdpCredentialRepoMock.Setup(r => r.GetVerificationsByCredentialAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(verifications);

        // Act
        var result = await _service.GetVerificationsByCredentialAsync(credentialId);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task RecordVerificationAsync_Succeeds_WhenCredentialExists()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var verification = CreateValidGdpVerification(credentialId);
        var credential = CreateValidCredential();
        credential.CredentialId = credentialId;
        var expectedId = Guid.NewGuid();

        _gdpCredentialRepoMock.Setup(r => r.GetCredentialAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        _gdpCredentialRepoMock.Setup(r => r.CreateVerificationAsync(verification, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        var (id, result) = await _service.RecordVerificationAsync(verification);

        // Assert
        result.IsValid.Should().BeTrue();
        id.Should().Be(expectedId);
    }

    [Fact]
    public async Task RecordVerificationAsync_Fails_WhenCredentialNotFound()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var verification = CreateValidGdpVerification(credentialId);

        _gdpCredentialRepoMock.Setup(r => r.GetCredentialAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpCredential?)null);

        // Act
        var (id, result) = await _service.RecordVerificationAsync(verification);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
    }

    [Fact]
    public async Task RecordVerificationAsync_UpdatesLastVerificationDate()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var verification = CreateValidGdpVerification(credentialId);
        var credential = CreateValidCredential();
        credential.CredentialId = credentialId;

        _gdpCredentialRepoMock.Setup(r => r.GetCredentialAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        _gdpCredentialRepoMock.Setup(r => r.CreateVerificationAsync(verification, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _service.RecordVerificationAsync(verification);

        // Assert
        _gdpCredentialRepoMock.Verify(r => r.UpdateCredentialAsync(
            It.Is<GdpCredential>(c => c.LastVerificationDate == verification.VerificationDate),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Partner Checks Tests

    [Fact]
    public async Task IsPartnerQualifiedAsync_ReturnsTrue_WhenApprovedAndValid()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var credential = CreateValidCredential();
        credential.QualificationStatus = GdpQualificationStatus.Approved;
        credential.ValidityStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6));
        credential.ValidityEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6));

        _gdpCredentialRepoMock.Setup(r => r.GetCredentialsByEntityAsync(
                GdpCredentialEntityType.Supplier, entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { credential });

        // Act
        var result = await _service.IsPartnerQualifiedAsync(GdpCredentialEntityType.Supplier, entityId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsPartnerQualifiedAsync_ReturnsFalse_WhenNoApprovedCredentials()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        _gdpCredentialRepoMock.Setup(r => r.GetCredentialsByEntityAsync(
                GdpCredentialEntityType.Supplier, entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<GdpCredential>());

        // Act
        var result = await _service.IsPartnerQualifiedAsync(GdpCredentialEntityType.Supplier, entityId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetProvidersRequiringReviewAsync_ReturnsList()
    {
        // Arrange
        var providers = new List<GdpServiceProvider> { CreateValidProvider() };
        _gdpCredentialRepoMock.Setup(r => r.GetProvidersRequiringReviewAsync(
                It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        // Act
        var result = await _service.GetProvidersRequiringReviewAsync();

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCredentialsExpiringAsync_ReturnsList()
    {
        // Arrange
        var credentials = new List<GdpCredential> { CreateValidCredential() };
        _gdpCredentialRepoMock.Setup(r => r.GetCredentialsExpiringBeforeAsync(
                It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credentials);

        // Act
        var result = await _service.GetCredentialsExpiringAsync(90);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region Inspections Tests

    [Fact]
    public async Task GetAllInspectionsAsync_ReturnsList()
    {
        // Arrange
        var inspections = new List<GdpInspection> { CreateValidInspection() };
        _gdpInspectionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(inspections);

        // Act
        var result = await _service.GetAllInspectionsAsync();

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetInspectionAsync_ReturnsInspection_WhenFound()
    {
        // Arrange
        var inspection = CreateValidInspection();
        _gdpInspectionRepoMock.Setup(r => r.GetByIdAsync(inspection.InspectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inspection);

        // Act
        var result = await _service.GetInspectionAsync(inspection.InspectionId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetInspectionsBySiteAsync_ReturnsFilteredList()
    {
        // Arrange
        var siteId = Guid.NewGuid();
        var inspections = new List<GdpInspection> { CreateValidInspection() };
        _gdpInspectionRepoMock.Setup(r => r.GetBySiteAsync(siteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inspections);

        // Act
        var result = await _service.GetInspectionsBySiteAsync(siteId);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateInspectionAsync_Succeeds_WhenValid()
    {
        // Arrange
        var inspection = CreateValidInspection();
        var expectedId = Guid.NewGuid();
        _gdpInspectionRepoMock.Setup(r => r.CreateAsync(inspection, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        var (id, result) = await _service.CreateInspectionAsync(inspection);

        // Assert
        result.IsValid.Should().BeTrue();
        id.Should().Be(expectedId);
    }

    [Fact]
    public async Task CreateInspectionAsync_Fails_WhenValidationFails()
    {
        // Arrange
        var inspection = new GdpInspection(); // Empty

        // Act
        var (id, result) = await _service.CreateInspectionAsync(inspection);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
    }

    [Fact]
    public async Task UpdateInspectionAsync_Succeeds_WhenFound()
    {
        // Arrange
        var inspection = CreateValidInspection();
        _gdpInspectionRepoMock.Setup(r => r.GetByIdAsync(inspection.InspectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inspection);

        // Act
        var result = await _service.UpdateInspectionAsync(inspection);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateInspectionAsync_Fails_WhenNotFound()
    {
        // Arrange
        var inspection = CreateValidInspection();
        _gdpInspectionRepoMock.Setup(r => r.GetByIdAsync(inspection.InspectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpInspection?)null);

        // Act
        var result = await _service.UpdateInspectionAsync(inspection);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region Findings Tests

    [Fact]
    public async Task GetFindingsAsync_ReturnsList()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var findings = new List<GdpInspectionFinding> { CreateValidFinding(inspectionId) };
        _gdpInspectionRepoMock.Setup(r => r.GetFindingsAsync(inspectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(findings);

        // Act
        var result = await _service.GetFindingsAsync(inspectionId);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetFindingAsync_ReturnsFinding_WhenFound()
    {
        // Arrange
        var finding = CreateValidFinding(Guid.NewGuid());
        _gdpInspectionRepoMock.Setup(r => r.GetFindingByIdAsync(finding.FindingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(finding);

        // Act
        var result = await _service.GetFindingAsync(finding.FindingId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateFindingAsync_Succeeds_WhenInspectionExists()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var finding = CreateValidFinding(inspectionId);
        var expectedId = Guid.NewGuid();

        _gdpInspectionRepoMock.Setup(r => r.GetByIdAsync(inspectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidInspection());
        _gdpInspectionRepoMock.Setup(r => r.CreateFindingAsync(finding, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        var (id, result) = await _service.CreateFindingAsync(finding);

        // Assert
        result.IsValid.Should().BeTrue();
        id.Should().Be(expectedId);
    }

    [Fact]
    public async Task CreateFindingAsync_Fails_WhenInspectionNotFound()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var finding = CreateValidFinding(inspectionId);

        _gdpInspectionRepoMock.Setup(r => r.GetByIdAsync(inspectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpInspection?)null);

        // Act
        var (id, result) = await _service.CreateFindingAsync(finding);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFindingAsync_Succeeds_WhenFound()
    {
        // Arrange
        var findingId = Guid.NewGuid();
        _gdpInspectionRepoMock.Setup(r => r.GetFindingByIdAsync(findingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidFinding(Guid.NewGuid()));

        // Act
        var result = await _service.DeleteFindingAsync(findingId);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFindingAsync_Fails_WhenNotFound()
    {
        // Arrange
        var findingId = Guid.NewGuid();
        _gdpInspectionRepoMock.Setup(r => r.GetFindingByIdAsync(findingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpInspectionFinding?)null);

        // Act
        var result = await _service.DeleteFindingAsync(findingId);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region CAPAs Tests

    [Fact]
    public async Task GetAllCapasAsync_ReturnsList()
    {
        // Arrange
        var capas = new List<Capa> { CreateValidCapa(Guid.NewGuid()) };
        _capaRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(capas);

        // Act
        var result = await _service.GetAllCapasAsync();

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCapaAsync_ReturnsCapa_WhenFound()
    {
        // Arrange
        var capa = CreateValidCapa(Guid.NewGuid());
        _capaRepoMock.Setup(r => r.GetByIdAsync(capa.CapaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(capa);

        // Act
        var result = await _service.GetCapaAsync(capa.CapaId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCapasByFindingAsync_ReturnsFilteredList()
    {
        // Arrange
        var findingId = Guid.NewGuid();
        var capas = new List<Capa> { CreateValidCapa(findingId) };
        _capaRepoMock.Setup(r => r.GetByFindingAsync(findingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(capas);

        // Act
        var result = await _service.GetCapasByFindingAsync(findingId);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetOverdueCapasAsync_ReturnsList()
    {
        // Arrange
        var capas = new List<Capa> { CreateValidCapa(Guid.NewGuid()) };
        _capaRepoMock.Setup(r => r.GetOverdueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(capas);

        // Act
        var result = await _service.GetOverdueCapasAsync();

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateCapaAsync_Succeeds_WhenFindingExists()
    {
        // Arrange
        var findingId = Guid.NewGuid();
        var capa = CreateValidCapa(findingId);
        var expectedId = Guid.NewGuid();

        _gdpInspectionRepoMock.Setup(r => r.GetFindingByIdAsync(findingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidFinding(Guid.NewGuid()));
        _capaRepoMock.Setup(r => r.CreateAsync(capa, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        var (id, result) = await _service.CreateCapaAsync(capa);

        // Assert
        result.IsValid.Should().BeTrue();
        id.Should().Be(expectedId);
    }

    [Fact]
    public async Task CreateCapaAsync_Fails_WhenFindingNotFound()
    {
        // Arrange
        var findingId = Guid.NewGuid();
        var capa = CreateValidCapa(findingId);

        _gdpInspectionRepoMock.Setup(r => r.GetFindingByIdAsync(findingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpInspectionFinding?)null);

        // Act
        var (id, result) = await _service.CreateCapaAsync(capa);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCapaAsync_Succeeds_WhenFound()
    {
        // Arrange
        var capa = CreateValidCapa(Guid.NewGuid());
        _capaRepoMock.Setup(r => r.GetByIdAsync(capa.CapaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(capa);

        // Act
        var result = await _service.UpdateCapaAsync(capa);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCapaAsync_Fails_WhenNotFound()
    {
        // Arrange
        var capa = CreateValidCapa(Guid.NewGuid());
        _capaRepoMock.Setup(r => r.GetByIdAsync(capa.CapaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Capa?)null);

        // Act
        var result = await _service.UpdateCapaAsync(capa);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CompleteCapaAsync_Succeeds_WhenFound()
    {
        // Arrange
        var capa = CreateValidCapa(Guid.NewGuid());
        _capaRepoMock.Setup(r => r.GetByIdAsync(capa.CapaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(capa);

        // Act
        var result = await _service.CompleteCapaAsync(capa.CapaId, DateOnly.FromDateTime(DateTime.UtcNow), "Verified effective");

        // Assert
        result.IsValid.Should().BeTrue();
        _capaRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Capa>(c => c.Status == CapaStatus.Completed),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteCapaAsync_Fails_WhenNotFound()
    {
        // Arrange
        var capaId = Guid.NewGuid();
        _capaRepoMock.Setup(r => r.GetByIdAsync(capaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Capa?)null);

        // Act
        var result = await _service.CompleteCapaAsync(capaId, DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region Documents Tests

    [Fact]
    public async Task GetDocumentsByEntityAsync_ReturnsList()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var documents = new List<GdpDocument> { CreateValidGdpDocument(entityId) };
        _gdpDocumentRepoMock.Setup(r => r.GetDocumentsByEntityAsync(
                GdpDocumentEntityType.Site, entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _service.GetDocumentsByEntityAsync(GdpDocumentEntityType.Site, entityId);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetDocumentAsync_ReturnsDocument_WhenFound()
    {
        // Arrange
        var document = CreateValidGdpDocument(Guid.NewGuid());
        _gdpDocumentRepoMock.Setup(r => r.GetDocumentAsync(document.DocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        var result = await _service.GetDocumentAsync(document.DocumentId);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UploadDocumentAsync_Succeeds_WhenValid()
    {
        // Arrange
        var document = CreateValidGdpDocument(Guid.NewGuid());
        var content = new MemoryStream(new byte[] { 1, 2, 3 });
        var expectedId = Guid.NewGuid();

        _documentStorageMock.Setup(r => r.UploadDocumentAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Uri("https://storage.blob.core.windows.net/gdp-docs/test.pdf"));
        _gdpDocumentRepoMock.Setup(r => r.CreateDocumentAsync(document, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        var (id, result) = await _service.UploadDocumentAsync(document, content);

        // Assert
        result.IsValid.Should().BeTrue();
        id.Should().Be(expectedId);
    }

    [Fact]
    public async Task UploadDocumentAsync_Fails_WhenValidationFails()
    {
        // Arrange
        var document = new GdpDocument(); // Empty
        var content = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var (id, result) = await _service.UploadDocumentAsync(document, content);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
    }

    [Fact]
    public async Task GetDocumentDownloadUrlAsync_Succeeds_WhenFound()
    {
        // Arrange
        var document = CreateValidGdpDocument(Guid.NewGuid());
        var expectedUri = new Uri("https://storage.blob.core.windows.net/gdp-docs/test.pdf?sas=token");

        _gdpDocumentRepoMock.Setup(r => r.GetDocumentAsync(document.DocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);
        _documentStorageMock.Setup(r => r.GetDocumentSasUriAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUri);

        // Act
        var (url, result) = await _service.GetDocumentDownloadUrlAsync(document.DocumentId);

        // Assert
        result.IsValid.Should().BeTrue();
        url.Should().Be(expectedUri);
    }

    [Fact]
    public async Task GetDocumentDownloadUrlAsync_Fails_WhenNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _gdpDocumentRepoMock.Setup(r => r.GetDocumentAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpDocument?)null);

        // Act
        var (url, result) = await _service.GetDocumentDownloadUrlAsync(documentId);

        // Assert
        result.IsValid.Should().BeFalse();
        url.Should().BeNull();
    }

    [Fact]
    public async Task DeleteDocumentAsync_Succeeds_WhenFound()
    {
        // Arrange
        var document = CreateValidGdpDocument(Guid.NewGuid());
        _gdpDocumentRepoMock.Setup(r => r.GetDocumentAsync(document.DocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        var result = await _service.DeleteDocumentAsync(document.DocumentId);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDocumentAsync_Fails_WhenNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _gdpDocumentRepoMock.Setup(r => r.GetDocumentAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpDocument?)null);

        // Act
        var result = await _service.DeleteDocumentAsync(documentId);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region WDA Coverage Tests

    [Fact]
    public async Task GetWdaCoverageAsync_ReturnsList()
    {
        // Arrange
        var coverages = new List<GdpSiteWdaCoverage> { CreateValidWdaCoverage() };
        _gdpSiteRepoMock.Setup(r => r.GetWdaCoverageAsync("WH-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(coverages);

        // Act
        var result = await _service.GetWdaCoverageAsync("WH-001", "nlpd");

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddWdaCoverageAsync_Succeeds_WhenValid()
    {
        // Arrange
        var coverage = CreateValidWdaCoverage();
        var licence = new Licence
        {
            LicenceId = coverage.LicenceId,
            LicenceNumber = "WDA-001",
            LicenceTypeId = Guid.NewGuid(),
            HolderType = "Company",
            IssuingAuthority = "MHRA",
            Status = "Valid"
        };
        var wdaType = new LicenceType
        {
            LicenceTypeId = licence.LicenceTypeId,
            Name = "Wholesale Distribution Authorisation (WDA)",
            IssuingAuthority = "MHRA",
            PermittedActivities = LicenceTypes.PermittedActivity.Distribute,
            IsActive = true
        };
        var gdpSite = CreateValidGdpSite();
        var expectedId = Guid.NewGuid();

        _licenceRepoMock.Setup(r => r.GetByIdAsync(coverage.LicenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);
        _licenceTypeRepoMock.Setup(r => r.GetByIdAsync(licence.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wdaType);
        _gdpSiteRepoMock.Setup(r => r.GetGdpExtensionAsync("WH-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(gdpSite);
        _gdpSiteRepoMock.Setup(r => r.AddWdaCoverageAsync(coverage, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        var (id, result) = await _service.AddWdaCoverageAsync(coverage);

        // Assert
        result.IsValid.Should().BeTrue();
        id.Should().Be(expectedId);
    }

    [Fact]
    public async Task AddWdaCoverageAsync_Fails_WhenValidationFails()
    {
        // Arrange
        var coverage = new GdpSiteWdaCoverage(); // Empty

        // Act
        var (id, result) = await _service.AddWdaCoverageAsync(coverage);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
    }

    [Fact]
    public async Task AddWdaCoverageAsync_Fails_WhenLicenceNotFound()
    {
        // Arrange
        var coverage = CreateValidWdaCoverage();
        _licenceRepoMock.Setup(r => r.GetByIdAsync(coverage.LicenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licence?)null);

        // Act
        var (id, result) = await _service.AddWdaCoverageAsync(coverage);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.LICENCE_NOT_FOUND);
    }

    [Fact]
    public async Task AddWdaCoverageAsync_Fails_WhenLicenceNotWdaType()
    {
        // Arrange
        var coverage = CreateValidWdaCoverage();
        var licence = new Licence
        {
            LicenceId = coverage.LicenceId,
            LicenceNumber = "OAL-001",
            LicenceTypeId = Guid.NewGuid(),
            HolderType = "Company",
            IssuingAuthority = "IGJ",
            Status = "Valid"
        };
        var nonWdaType = new LicenceType
        {
            LicenceTypeId = licence.LicenceTypeId,
            Name = "Opium Act Licence",
            IssuingAuthority = "IGJ",
            PermittedActivities = LicenceTypes.PermittedActivity.Store,
            IsActive = true
        };

        _licenceRepoMock.Setup(r => r.GetByIdAsync(coverage.LicenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);
        _licenceTypeRepoMock.Setup(r => r.GetByIdAsync(licence.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nonWdaType);

        // Act
        var (id, result) = await _service.AddWdaCoverageAsync(coverage);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("not a Wholesale Distribution Authorisation"));
    }

    [Fact]
    public async Task AddWdaCoverageAsync_Fails_WhenSiteNotConfigured()
    {
        // Arrange
        var coverage = CreateValidWdaCoverage();
        var licence = new Licence
        {
            LicenceId = coverage.LicenceId,
            LicenceNumber = "WDA-001",
            LicenceTypeId = Guid.NewGuid(),
            HolderType = "Company",
            IssuingAuthority = "MHRA",
            Status = "Valid"
        };
        var wdaType = new LicenceType
        {
            LicenceTypeId = licence.LicenceTypeId,
            Name = "Wholesale Distribution Authorisation (WDA)",
            IssuingAuthority = "MHRA",
            PermittedActivities = LicenceTypes.PermittedActivity.Distribute,
            IsActive = true
        };

        _licenceRepoMock.Setup(r => r.GetByIdAsync(coverage.LicenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);
        _licenceTypeRepoMock.Setup(r => r.GetByIdAsync(licence.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wdaType);
        _gdpSiteRepoMock.Setup(r => r.GetGdpExtensionAsync("WH-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpSite?)null);

        // Act
        var (id, result) = await _service.AddWdaCoverageAsync(coverage);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("not configured for GDP"));
    }

    [Fact]
    public async Task RemoveWdaCoverageAsync_Succeeds()
    {
        // Arrange
        var coverageId = Guid.NewGuid();

        // Act
        var result = await _service.RemoveWdaCoverageAsync(coverageId);

        // Assert
        result.IsValid.Should().BeTrue();
        _gdpSiteRepoMock.Verify(r => r.DeleteWdaCoverageAsync(coverageId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static GdpSite CreateValidGdpSite()
    {
        return new GdpSite
        {
            WarehouseId = "WH-001",
            WarehouseName = "Main Warehouse",
            DataAreaId = "nlpd",
            GdpExtensionId = Guid.NewGuid(),
            GdpSiteType = GdpSiteType.Warehouse,
            PermittedActivities = GdpSiteActivity.StorageOver72h | GdpSiteActivity.TemperatureControlled,
            IsGdpActive = true
        };
    }

    private static GdpServiceProvider CreateValidProvider()
    {
        return new GdpServiceProvider
        {
            ProviderId = Guid.NewGuid(),
            ProviderName = "Test 3PL Provider",
            ServiceType = GdpServiceType.ThirdPartyLogistics,
            TemperatureControlledCapability = true,
            QualificationStatus = GdpQualificationStatus.Approved,
            ReviewFrequencyMonths = 24,
            LastReviewDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
            NextReviewDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(18)),
            IsActive = true
        };
    }

    private static GdpCredential CreateValidCredential()
    {
        return new GdpCredential
        {
            CredentialId = Guid.NewGuid(),
            EntityType = GdpCredentialEntityType.Supplier,
            EntityId = Guid.NewGuid(),
            WdaNumber = "WDA-NL-2024-001",
            ValidityStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
            ValidityEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(18)),
            QualificationStatus = GdpQualificationStatus.Approved
        };
    }

    private static QualificationReview CreateValidReview(Guid entityId)
    {
        return QualificationReview.CreateForServiceProvider(
            entityId,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            ReviewMethod.OnSiteAudit,
            ReviewOutcome.Approved,
            "QA Manager");
    }

    private static GdpCredentialVerification CreateValidGdpVerification(Guid credentialId)
    {
        return new GdpCredentialVerification
        {
            VerificationId = Guid.NewGuid(),
            CredentialId = credentialId,
            VerificationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            VerificationMethod = GdpVerificationMethod.EudraGMDP,
            VerifiedBy = "QA Specialist",
            Outcome = GdpVerificationOutcome.Valid
        };
    }

    private static GdpInspection CreateValidInspection()
    {
        return new GdpInspection
        {
            InspectionId = Guid.NewGuid(),
            InspectionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            InspectorName = "IGJ Inspector",
            InspectionType = GdpInspectionType.RegulatoryAuthority,
            SiteId = Guid.NewGuid(),
            FindingsSummary = "Minor findings noted"
        };
    }

    private static GdpInspectionFinding CreateValidFinding(Guid inspectionId)
    {
        return new GdpInspectionFinding
        {
            FindingId = Guid.NewGuid(),
            InspectionId = inspectionId,
            FindingDescription = "Temperature monitoring gap noted",
            Classification = FindingClassification.Major,
            FindingNumber = "F-001"
        };
    }

    private static Capa CreateValidCapa(Guid findingId)
    {
        return new Capa
        {
            CapaId = Guid.NewGuid(),
            CapaNumber = "CAPA-2026-001",
            FindingId = findingId,
            Description = "Implement continuous temperature monitoring",
            OwnerName = "Warehouse Manager",
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            Status = CapaStatus.Open
        };
    }

    private static GdpDocument CreateValidGdpDocument(Guid ownerEntityId)
    {
        return new GdpDocument
        {
            DocumentId = Guid.NewGuid(),
            OwnerEntityType = GdpDocumentEntityType.Site,
            OwnerEntityId = ownerEntityId,
            DocumentType = DocumentType.Certificate,
            FileName = "gdp-certificate.pdf",
            BlobStorageUrl = "https://storage.blob.core.windows.net/gdp-docs/cert.pdf",
            UploadedBy = "Compliance Manager",
            UploadedDate = DateTime.UtcNow,
            ContentType = "application/pdf"
        };
    }

    private static GdpSiteWdaCoverage CreateValidWdaCoverage()
    {
        return new GdpSiteWdaCoverage
        {
            CoverageId = Guid.NewGuid(),
            WarehouseId = "WH-001",
            DataAreaId = "nlpd",
            LicenceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(18))
        };
    }

    #endregion
}
