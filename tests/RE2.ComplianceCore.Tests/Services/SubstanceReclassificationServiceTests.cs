using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.ComplianceCore.Services.LicenceValidation;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Tests.Services;

/// <summary>
/// Unit tests for SubstanceReclassificationService.
/// T080n: Per FR-066 reclassification workflow testing.
/// </summary>
public class SubstanceReclassificationServiceTests
{
    private readonly Mock<ISubstanceReclassificationRepository> _reclassificationRepoMock;
    private readonly Mock<IControlledSubstanceRepository> _substanceRepoMock;
    private readonly Mock<ILicenceRepository> _licenceRepoMock;
    private readonly Mock<ILicenceTypeRepository> _licenceTypeRepoMock;
    private readonly Mock<ILogger<SubstanceReclassificationService>> _loggerMock;
    private readonly SubstanceReclassificationService _service;

    public SubstanceReclassificationServiceTests()
    {
        _reclassificationRepoMock = new Mock<ISubstanceReclassificationRepository>();
        _substanceRepoMock = new Mock<IControlledSubstanceRepository>();
        _licenceRepoMock = new Mock<ILicenceRepository>();
        _licenceTypeRepoMock = new Mock<ILicenceTypeRepository>();
        _loggerMock = new Mock<ILogger<SubstanceReclassificationService>>();

        _service = new SubstanceReclassificationService(
            _reclassificationRepoMock.Object,
            _substanceRepoMock.Object,
            _licenceRepoMock.Object,
            _licenceTypeRepoMock.Object,
            _loggerMock.Object);
    }

    #region CreateReclassificationAsync Tests

    [Fact]
    public async Task CreateReclassificationAsync_WithValidData_ReturnsId()
    {
        // Arrange
        var substanceCode = "Morphine";
        var substance = CreateSubstance(substanceCode, SubstanceCategories.OpiumActList.ListII, SubstanceCategories.PrecursorCategory.None);
        var reclassification = CreateReclassification(substanceCode,
            SubstanceCategories.OpiumActList.ListII, SubstanceCategories.OpiumActList.ListI,
            SubstanceCategories.PrecursorCategory.None, SubstanceCategories.PrecursorCategory.None);

        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);
        _reclassificationRepoMock.Setup(r => r.CreateAsync(It.IsAny<SubstanceReclassification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        var (id, result) = await _service.CreateReclassificationAsync(reclassification);

        // Assert
        result.IsValid.Should().BeTrue();
        id.Should().NotBeNull();
        _reclassificationRepoMock.Verify(r => r.CreateAsync(It.IsAny<SubstanceReclassification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateReclassificationAsync_WithNonExistentSubstance_ReturnsValidationError()
    {
        // Arrange
        var substanceCode = "NonExistent";
        var reclassification = CreateReclassification(substanceCode);

        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);

        // Act
        var (id, result) = await _service.CreateReclassificationAsync(reclassification);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.Message.Contains("not found"));
    }

    [Fact]
    public async Task CreateReclassificationAsync_WithMismatchedPreviousClassification_ReturnsValidationError()
    {
        // Arrange
        var substanceCode = "Morphine";
        var substance = CreateSubstance(substanceCode, SubstanceCategories.OpiumActList.ListI, SubstanceCategories.PrecursorCategory.None);
        var reclassification = CreateReclassification(substanceCode,
            SubstanceCategories.OpiumActList.ListII, SubstanceCategories.OpiumActList.ListI, // Previous doesn't match
            SubstanceCategories.PrecursorCategory.None, SubstanceCategories.PrecursorCategory.None);

        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var (id, result) = await _service.CreateReclassificationAsync(reclassification);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.Message.Contains("current classification"));
    }

    [Fact]
    public async Task CreateReclassificationAsync_WithNoActualChange_ReturnsValidationError()
    {
        // Arrange
        var substanceCode = "Morphine";
        var substance = CreateSubstance(substanceCode, SubstanceCategories.OpiumActList.ListII, SubstanceCategories.PrecursorCategory.None);
        var reclassification = CreateReclassification(substanceCode,
            SubstanceCategories.OpiumActList.ListII, SubstanceCategories.OpiumActList.ListII, // Same values
            SubstanceCategories.PrecursorCategory.None, SubstanceCategories.PrecursorCategory.None);

        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var (id, result) = await _service.CreateReclassificationAsync(reclassification);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.Message.Contains("must change"));
    }

    #endregion

    #region AnalyzeCustomerImpactAsync Tests

    [Fact]
    public async Task AnalyzeCustomerImpactAsync_WithNoAffectedLicences_ReturnsEmptyAnalysis()
    {
        // Arrange
        var reclassificationId = Guid.NewGuid();
        var substanceCode = "Morphine";
        var reclassification = CreateReclassification(substanceCode);
        reclassification.ReclassificationId = reclassificationId;

        var substance = CreateSubstance(substanceCode);

        _reclassificationRepoMock.Setup(r => r.GetByIdAsync(reclassificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reclassification);
        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);
        _licenceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Licence>());

        // Act
        var analysis = await _service.AnalyzeCustomerImpactAsync(reclassificationId);

        // Assert
        analysis.TotalAffectedCustomers.Should().Be(0);
        analysis.CustomersFlaggedForReQualification.Should().Be(0);
        analysis.CustomerImpacts.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeCustomerImpactAsync_WithAffectedCustomers_ReturnsImpactAnalysis()
    {
        // Arrange
        var reclassificationId = Guid.NewGuid();
        var substanceCode = "Morphine";
        var customerId = Guid.NewGuid();

        var reclassification = CreateReclassification(substanceCode,
            SubstanceCategories.OpiumActList.ListII, SubstanceCategories.OpiumActList.ListI,
            SubstanceCategories.PrecursorCategory.None, SubstanceCategories.PrecursorCategory.None);
        reclassification.ReclassificationId = reclassificationId;

        var substance = CreateSubstance(substanceCode);

        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Wholesale Licence",
            IssuingAuthority = "IGJ",
            PermittedActivities = LicenceTypes.PermittedActivity.Distribute // Not sufficient for List I
        };

        var licence = new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = "WL-001",
            LicenceTypeId = licenceType.LicenceTypeId,
            HolderType = "Customer",
            HolderId = customerId,
            IssuingAuthority = "IGJ",
            Status = "Valid",
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
        };

        _reclassificationRepoMock.Setup(r => r.GetByIdAsync(reclassificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reclassification);
        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);
        _licenceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { licence });
        _licenceTypeRepoMock.Setup(r => r.GetByIdAsync(licenceType.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licenceType);

        // Act
        var analysis = await _service.AnalyzeCustomerImpactAsync(reclassificationId);

        // Assert
        analysis.TotalAffectedCustomers.Should().Be(1);
        analysis.CustomersFlaggedForReQualification.Should().Be(1);
        analysis.CustomerImpacts.Should().HaveCount(1);
        analysis.CustomerImpacts[0].CustomerId.Should().Be(customerId);
        analysis.CustomerImpacts[0].RequiresReQualification.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzeCustomerImpactAsync_WithSufficientLicence_NoFlagging()
    {
        // Arrange
        var reclassificationId = Guid.NewGuid();
        var substanceCode = "Morphine";
        var customerId = Guid.NewGuid();

        var reclassification = CreateReclassification(substanceCode,
            SubstanceCategories.OpiumActList.ListII, SubstanceCategories.OpiumActList.ListI,
            SubstanceCategories.PrecursorCategory.None, SubstanceCategories.PrecursorCategory.None);
        reclassification.ReclassificationId = reclassificationId;

        var substance = CreateSubstance(substanceCode);

        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Opium Act Exemption",
            IssuingAuthority = "IGJ",
            PermittedActivities = LicenceTypes.PermittedActivity.Possess | LicenceTypes.PermittedActivity.Store | LicenceTypes.PermittedActivity.Distribute
        };

        var licence = new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = "OA-001",
            LicenceTypeId = licenceType.LicenceTypeId,
            HolderType = "Customer",
            HolderId = customerId,
            IssuingAuthority = "IGJ",
            Status = "Valid",
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
        };

        _reclassificationRepoMock.Setup(r => r.GetByIdAsync(reclassificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reclassification);
        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);
        _licenceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { licence });
        _licenceTypeRepoMock.Setup(r => r.GetByIdAsync(licenceType.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licenceType);

        // Act
        var analysis = await _service.AnalyzeCustomerImpactAsync(reclassificationId);

        // Assert
        analysis.TotalAffectedCustomers.Should().Be(1);
        analysis.CustomersFlaggedForReQualification.Should().Be(0);
        analysis.CustomersWithSufficientLicences.Should().Be(1);
        analysis.CustomerImpacts[0].HasSufficientLicence.Should().BeTrue();
        analysis.CustomerImpacts[0].RequiresReQualification.Should().BeFalse();
    }

    #endregion

    #region ProcessReclassificationAsync Tests

    [Fact]
    public async Task ProcessReclassificationAsync_WithPendingStatus_ProcessesSuccessfully()
    {
        // Arrange
        var reclassificationId = Guid.NewGuid();
        var substanceCode = "Morphine";

        var reclassification = CreateReclassification(substanceCode);
        reclassification.ReclassificationId = reclassificationId;
        reclassification.Status = ReclassificationStatus.Pending;

        var substance = CreateSubstance(substanceCode);

        _reclassificationRepoMock.Setup(r => r.GetByIdAsync(reclassificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reclassification);
        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);
        _licenceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Licence>());

        // Act
        var result = await _service.ProcessReclassificationAsync(reclassificationId);

        // Assert
        result.IsValid.Should().BeTrue();
        _reclassificationRepoMock.Verify(r => r.UpdateAsync(
            It.Is<SubstanceReclassification>(s => s.Status == ReclassificationStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _substanceRepoMock.Verify(r => r.UpdateComplianceExtensionAsync(It.IsAny<ControlledSubstance>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessReclassificationAsync_WithNonPendingStatus_ReturnsError()
    {
        // Arrange
        var reclassificationId = Guid.NewGuid();
        var substanceCode = "Morphine";

        var reclassification = CreateReclassification(substanceCode);
        reclassification.ReclassificationId = reclassificationId;
        reclassification.Status = ReclassificationStatus.Completed;

        _reclassificationRepoMock.Setup(r => r.GetByIdAsync(reclassificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reclassification);

        // Act
        var result = await _service.ProcessReclassificationAsync(reclassificationId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("already in status"));
    }

    [Fact]
    public async Task ProcessReclassificationAsync_WithNotFoundReclassification_ReturnsError()
    {
        // Arrange
        var reclassificationId = Guid.NewGuid();

        _reclassificationRepoMock.Setup(r => r.GetByIdAsync(reclassificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubstanceReclassification?)null);

        // Act
        var result = await _service.ProcessReclassificationAsync(reclassificationId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("not found"));
    }

    #endregion

    #region CheckCustomerBlockedAsync Tests

    [Fact]
    public async Task CheckCustomerBlockedAsync_WithFlaggedCustomer_ReturnsBlocked()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var impact = new ReclassificationCustomerImpact
        {
            ImpactId = Guid.NewGuid(),
            CustomerId = customerId,
            RequiresReQualification = true,
            ReQualificationDate = null
        };

        _reclassificationRepoMock.Setup(r => r.GetCustomersRequiringReQualificationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { impact });

        // Act
        var (isBlocked, impacts) = await _service.CheckCustomerBlockedAsync(customerId);

        // Assert
        isBlocked.Should().BeTrue();
        impacts.Should().HaveCount(1);
        impacts.First().CustomerId.Should().Be(customerId);
    }

    [Fact]
    public async Task CheckCustomerBlockedAsync_WithNoFlaggedCustomers_ReturnsNotBlocked()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        _reclassificationRepoMock.Setup(r => r.GetCustomersRequiringReQualificationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<ReclassificationCustomerImpact>());

        // Act
        var (isBlocked, impacts) = await _service.CheckCustomerBlockedAsync(customerId);

        // Assert
        isBlocked.Should().BeFalse();
        impacts.Should().BeEmpty();
    }

    #endregion

    #region MarkCustomerReQualifiedAsync Tests

    [Fact]
    public async Task MarkCustomerReQualifiedAsync_WithValidImpact_Succeeds()
    {
        // Arrange
        var reclassificationId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var impact = new ReclassificationCustomerImpact
        {
            ImpactId = Guid.NewGuid(),
            ReclassificationId = reclassificationId,
            CustomerId = customerId,
            RequiresReQualification = true
        };

        _reclassificationRepoMock.Setup(r => r.GetCustomerImpactAsync(reclassificationId, customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(impact);

        // Act
        var result = await _service.MarkCustomerReQualifiedAsync(reclassificationId, customerId);

        // Assert
        result.IsValid.Should().BeTrue();
        _reclassificationRepoMock.Verify(r => r.UpdateCustomerImpactAsync(
            It.Is<ReclassificationCustomerImpact>(i => !i.RequiresReQualification && i.ReQualificationDate.HasValue),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkCustomerReQualifiedAsync_WithNotFoundImpact_ReturnsError()
    {
        // Arrange
        var reclassificationId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        _reclassificationRepoMock.Setup(r => r.GetCustomerImpactAsync(reclassificationId, customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReclassificationCustomerImpact?)null);

        // Act
        var result = await _service.MarkCustomerReQualifiedAsync(reclassificationId, customerId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("not found"));
    }

    #endregion

    #region GetEffectiveClassificationAsync Tests

    [Fact]
    public async Task GetEffectiveClassificationAsync_WithReclassification_ReturnsNewClassification()
    {
        // Arrange
        var substanceCode = "Morphine";
        var asOfDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var substance = CreateSubstance(substanceCode, SubstanceCategories.OpiumActList.ListI, SubstanceCategories.PrecursorCategory.None);

        var reclassification = CreateReclassification(substanceCode,
            SubstanceCategories.OpiumActList.ListII, SubstanceCategories.OpiumActList.ListI,
            SubstanceCategories.PrecursorCategory.None, SubstanceCategories.PrecursorCategory.None);
        reclassification.ReclassificationId = Guid.NewGuid();
        reclassification.Status = ReclassificationStatus.Completed;

        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);
        _reclassificationRepoMock.Setup(r => r.GetEffectiveReclassificationAsync(substanceCode, asOfDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reclassification);

        // Act
        var classification = await _service.GetEffectiveClassificationAsync(substanceCode, asOfDate);

        // Assert
        classification.OpiumActList.Should().Be(SubstanceCategories.OpiumActList.ListI);
        classification.SourceReclassificationId.Should().Be(reclassification.ReclassificationId);
    }

    [Fact]
    public async Task GetEffectiveClassificationAsync_WithNoReclassification_ReturnsCurrentClassification()
    {
        // Arrange
        var substanceCode = "Morphine";
        var asOfDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var substance = CreateSubstance(substanceCode, SubstanceCategories.OpiumActList.ListII, SubstanceCategories.PrecursorCategory.None);

        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);
        _reclassificationRepoMock.Setup(r => r.GetEffectiveReclassificationAsync(substanceCode, asOfDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubstanceReclassification?)null);
        _reclassificationRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<SubstanceReclassification>());

        // Act
        var classification = await _service.GetEffectiveClassificationAsync(substanceCode, asOfDate);

        // Assert
        classification.OpiumActList.Should().Be(SubstanceCategories.OpiumActList.ListII);
        classification.SourceReclassificationId.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private static ControlledSubstance CreateSubstance(
        string substanceCode,
        SubstanceCategories.OpiumActList opiumActList = SubstanceCategories.OpiumActList.ListII,
        SubstanceCategories.PrecursorCategory precursorCategory = SubstanceCategories.PrecursorCategory.None)
    {
        return new ControlledSubstance
        {
            SubstanceCode = substanceCode,
            SubstanceName = "Test Substance",
            OpiumActList = opiumActList,
            PrecursorCategory = precursorCategory,
            IsActive = true
        };
    }

    private static SubstanceReclassification CreateReclassification(
        string substanceCode,
        SubstanceCategories.OpiumActList previousOpiumActList = SubstanceCategories.OpiumActList.ListII,
        SubstanceCategories.OpiumActList newOpiumActList = SubstanceCategories.OpiumActList.ListI,
        SubstanceCategories.PrecursorCategory previousPrecursorCategory = SubstanceCategories.PrecursorCategory.None,
        SubstanceCategories.PrecursorCategory newPrecursorCategory = SubstanceCategories.PrecursorCategory.None)
    {
        return new SubstanceReclassification
        {
            SubstanceCode = substanceCode,
            PreviousOpiumActList = previousOpiumActList,
            NewOpiumActList = newOpiumActList,
            PreviousPrecursorCategory = previousPrecursorCategory,
            NewPrecursorCategory = newPrecursorCategory,
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            RegulatoryReference = "Staatscourant 2026/123",
            RegulatoryAuthority = "Ministry of Health",
            Status = ReclassificationStatus.Pending
        };
    }

    #endregion
}
