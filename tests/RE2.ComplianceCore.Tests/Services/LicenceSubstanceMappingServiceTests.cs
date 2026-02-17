using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.ComplianceCore.Services.LicenceValidation;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Tests.Services;

/// <summary>
/// T079h: Unit tests for LicenceSubstanceMappingService.
/// Per FR-004 licence-to-substance mapping management.
/// </summary>
public class LicenceSubstanceMappingServiceTests
{
    private readonly Mock<ILicenceSubstanceMappingRepository> _mappingRepoMock;
    private readonly Mock<ILicenceRepository> _licenceRepoMock;
    private readonly Mock<IControlledSubstanceRepository> _substanceRepoMock;
    private readonly Mock<ILogger<LicenceSubstanceMappingService>> _loggerMock;
    private readonly LicenceSubstanceMappingService _service;

    public LicenceSubstanceMappingServiceTests()
    {
        _mappingRepoMock = new Mock<ILicenceSubstanceMappingRepository>();
        _licenceRepoMock = new Mock<ILicenceRepository>();
        _substanceRepoMock = new Mock<IControlledSubstanceRepository>();
        _loggerMock = new Mock<ILogger<LicenceSubstanceMappingService>>();

        _service = new LicenceSubstanceMappingService(
            _mappingRepoMock.Object,
            _licenceRepoMock.Object,
            _substanceRepoMock.Object,
            _loggerMock.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingMapping_ReturnsMappingWithNavigationProperties()
    {
        // Arrange
        var mapping = CreateMapping();
        var licence = CreateLicence(mapping.LicenceId);
        var substance = CreateSubstance(mapping.SubstanceCode);

        _mappingRepoMock.Setup(r => r.GetByIdAsync(mapping.MappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapping);
        _licenceRepoMock.Setup(r => r.GetByIdAsync(mapping.LicenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);
        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(mapping.SubstanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _service.GetByIdAsync(mapping.MappingId);

        // Assert
        result.Should().NotBeNull();
        result!.MappingId.Should().Be(mapping.MappingId);
        result.Licence.Should().NotBeNull();
        result.Substance.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentMapping_ReturnsNull()
    {
        // Arrange
        var mappingId = Guid.NewGuid();
        _mappingRepoMock.Setup(r => r.GetByIdAsync(mappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LicenceSubstanceMapping?)null);

        // Act
        var result = await _service.GetByIdAsync(mappingId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByLicenceIdAsync Tests

    [Fact]
    public async Task GetByLicenceIdAsync_ReturnsAllMappingsForLicence()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var mappings = new List<LicenceSubstanceMapping>
        {
            CreateMapping(licenceId: licenceId, substanceCode: "Morphine"),
            CreateMapping(licenceId: licenceId, substanceCode: "Fentanyl")
        };

        _mappingRepoMock.Setup(r => r.GetByLicenceIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mappings);
        _licenceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateLicence(licenceId) });
        _substanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ControlledSubstance>());

        // Act
        var result = await _service.GetByLicenceIdAsync(licenceId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(m => m.LicenceId == licenceId);
    }

    #endregion

    #region GetActiveMappingsByLicenceIdAsync Tests

    [Fact]
    public async Task GetActiveMappingsByLicenceIdAsync_ReturnsOnlyActiveMappings()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var activeMappings = new List<LicenceSubstanceMapping>
        {
            CreateMapping(licenceId: licenceId, effectiveDate: DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)))
        };

        _mappingRepoMock.Setup(r => r.GetActiveMappingsByLicenceIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeMappings);
        _licenceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Licence>());
        _substanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ControlledSubstance>());

        // Act
        var result = await _service.GetActiveMappingsByLicenceIdAsync(licenceId);

        // Assert
        result.Should().HaveCount(1);
        _mappingRepoMock.Verify(r => r.GetActiveMappingsByLicenceIdAsync(licenceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidMapping_ReturnsId()
    {
        // Arrange
        var mapping = CreateMapping();
        var expectedId = Guid.NewGuid();
        var licence = CreateLicence(mapping.LicenceId);
        var substance = CreateSubstance(mapping.SubstanceCode);

        _licenceRepoMock.Setup(r => r.GetByIdAsync(mapping.LicenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);
        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(mapping.SubstanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);
        _mappingRepoMock.Setup(r => r.GetByLicenceSubstanceEffectiveDateAsync(
            mapping.LicenceId, mapping.SubstanceCode, mapping.EffectiveDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LicenceSubstanceMapping?)null);
        _mappingRepoMock.Setup(r => r.CreateAsync(It.IsAny<LicenceSubstanceMapping>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        var (id, result) = await _service.CreateAsync(mapping);

        // Assert
        result.IsValid.Should().BeTrue();
        id.Should().Be(expectedId);
        _mappingRepoMock.Verify(r => r.CreateAsync(It.IsAny<LicenceSubstanceMapping>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentLicence_ReturnsValidationError()
    {
        // Arrange
        var mapping = CreateMapping();

        _licenceRepoMock.Setup(r => r.GetByIdAsync(mapping.LicenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licence?)null);

        // Act
        var (id, result) = await _service.CreateAsync(mapping);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.Message.Contains("Licence") && v.Message.Contains("not found"));
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentSubstance_ReturnsValidationError()
    {
        // Arrange
        var mapping = CreateMapping();
        var licence = CreateLicence(mapping.LicenceId);

        _licenceRepoMock.Setup(r => r.GetByIdAsync(mapping.LicenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);
        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(mapping.SubstanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);

        // Act
        var (id, result) = await _service.CreateAsync(mapping);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.Message.Contains("Substance") && v.Message.Contains("not found"));
    }

    [Fact]
    public async Task CreateAsync_WithInactiveSubstance_ReturnsValidationError()
    {
        // Arrange
        var mapping = CreateMapping();
        var licence = CreateLicence(mapping.LicenceId);
        var inactiveSubstance = CreateSubstance(mapping.SubstanceCode, isActive: false);

        _licenceRepoMock.Setup(r => r.GetByIdAsync(mapping.LicenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);
        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(mapping.SubstanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveSubstance);

        // Act
        var (id, result) = await _service.CreateAsync(mapping);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.Message.Contains("not active"));
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateMapping_ReturnsValidationError()
    {
        // Arrange
        var mapping = CreateMapping();
        var licence = CreateLicence(mapping.LicenceId);
        var substance = CreateSubstance(mapping.SubstanceCode);
        var existingMapping = CreateMapping(mapping.LicenceId, mapping.SubstanceCode, mapping.EffectiveDate);

        _licenceRepoMock.Setup(r => r.GetByIdAsync(mapping.LicenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);
        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(mapping.SubstanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);
        _mappingRepoMock.Setup(r => r.GetByLicenceSubstanceEffectiveDateAsync(
            mapping.LicenceId, mapping.SubstanceCode, mapping.EffectiveDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMapping);

        // Act
        var (id, result) = await _service.CreateAsync(mapping);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.Message.Contains("already exists"));
    }

    [Fact]
    public async Task CreateAsync_WithExpiryExceedingLicenceExpiry_ReturnsValidationError()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var licence = CreateLicence(licenceId, expiryDate: DateOnly.FromDateTime(DateTime.Today.AddYears(1)));
        var substance = CreateSubstance();
        var mapping = CreateMapping(
            licenceId: licenceId,
            substanceCode: substance.SubstanceCode,
            effectiveDate: DateOnly.FromDateTime(DateTime.Today),
            expiryDate: DateOnly.FromDateTime(DateTime.Today.AddYears(2)) // Exceeds licence expiry
        );

        _licenceRepoMock.Setup(r => r.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);
        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(mapping.SubstanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var (id, result) = await _service.CreateAsync(mapping);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.Message.Contains("ExpiryDate") && v.Message.Contains("cannot exceed"));
    }

    [Fact]
    public async Task CreateAsync_WithEffectiveDateBeforeLicenceIssueDate_ReturnsValidationError()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var licence = CreateLicence(licenceId, issueDate: DateOnly.FromDateTime(DateTime.Today));
        var substance = CreateSubstance();
        var mapping = CreateMapping(
            licenceId: licenceId,
            substanceCode: substance.SubstanceCode,
            effectiveDate: DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)) // Before licence issue
        );

        _licenceRepoMock.Setup(r => r.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);
        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(mapping.SubstanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var (id, result) = await _service.CreateAsync(mapping);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.Message.Contains("EffectiveDate") && v.Message.Contains("cannot be before"));
    }

    [Fact]
    public async Task CreateAsync_WithExpiryBeforeEffective_ReturnsValidationError()
    {
        // Arrange
        var mapping = CreateMapping(
            effectiveDate: DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
            expiryDate: DateOnly.FromDateTime(DateTime.Today) // Expiry before effective
        );
        var licence = CreateLicence(mapping.LicenceId);
        var substance = CreateSubstance(mapping.SubstanceCode);

        _licenceRepoMock.Setup(r => r.GetByIdAsync(mapping.LicenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);
        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(mapping.SubstanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var (id, result) = await _service.CreateAsync(mapping);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.Message.Contains("ExpiryDate must be after EffectiveDate"));
    }

    [Fact]
    public async Task CreateAsync_WithNegativeMaxQuantity_ReturnsValidationError()
    {
        // Arrange
        var mapping = CreateMapping();
        mapping.MaxQuantityPerTransaction = -100;
        var licence = CreateLicence(mapping.LicenceId);
        var substance = CreateSubstance(mapping.SubstanceCode);

        _licenceRepoMock.Setup(r => r.GetByIdAsync(mapping.LicenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);
        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(mapping.SubstanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var (id, result) = await _service.CreateAsync(mapping);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.Message.Contains("cannot be negative"));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidMapping_Succeeds()
    {
        // Arrange
        var mapping = CreateMapping();
        var existingMapping = CreateMapping(mappingId: mapping.MappingId);
        var licence = CreateLicence(mapping.LicenceId);
        var substance = CreateSubstance(mapping.SubstanceCode);

        _mappingRepoMock.Setup(r => r.GetByIdAsync(mapping.MappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMapping);
        _licenceRepoMock.Setup(r => r.GetByIdAsync(mapping.LicenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);
        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(mapping.SubstanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _service.UpdateAsync(mapping);

        // Assert
        result.IsValid.Should().BeTrue();
        _mappingRepoMock.Verify(r => r.UpdateAsync(It.IsAny<LicenceSubstanceMapping>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentMapping_ReturnsNotFoundError()
    {
        // Arrange
        var mapping = CreateMapping();
        _mappingRepoMock.Setup(r => r.GetByIdAsync(mapping.MappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LicenceSubstanceMapping?)null);

        // Act
        var result = await _service.UpdateAsync(mapping);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.NOT_FOUND);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateAfterKeyChange_ReturnsValidationError()
    {
        // Arrange
        var existingMapping = CreateMapping();
        // Create updated mapping with same MappingId but different effective date
        var updatedMapping = CreateMapping(
            licenceId: existingMapping.LicenceId,
            substanceCode: existingMapping.SubstanceCode,
            mappingId: existingMapping.MappingId);
        updatedMapping.EffectiveDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)); // Changed effective date
        // Create another mapping that already exists with the new composite key
        var duplicateMapping = CreateMapping(
            licenceId: existingMapping.LicenceId,
            substanceCode: existingMapping.SubstanceCode,
            effectiveDate: updatedMapping.EffectiveDate);
        duplicateMapping.MappingId = Guid.NewGuid(); // Different mapping ID
        var licence = CreateLicence(updatedMapping.LicenceId);
        var substance = CreateSubstance(updatedMapping.SubstanceCode);

        _mappingRepoMock.Setup(r => r.GetByIdAsync(existingMapping.MappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMapping);
        _mappingRepoMock.Setup(r => r.GetByLicenceSubstanceEffectiveDateAsync(
            updatedMapping.LicenceId, updatedMapping.SubstanceCode, updatedMapping.EffectiveDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicateMapping);
        _licenceRepoMock.Setup(r => r.GetByIdAsync(updatedMapping.LicenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);
        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(updatedMapping.SubstanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _service.UpdateAsync(updatedMapping);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("already exists"));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingMapping_Succeeds()
    {
        // Arrange
        var mappingId = Guid.NewGuid();
        var mapping = CreateMapping(mappingId: mappingId);

        _mappingRepoMock.Setup(r => r.GetByIdAsync(mappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapping);

        // Act
        var result = await _service.DeleteAsync(mappingId);

        // Assert
        result.IsValid.Should().BeTrue();
        _mappingRepoMock.Verify(r => r.DeleteAsync(mappingId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentMapping_ReturnsNotFoundError()
    {
        // Arrange
        var mappingId = Guid.NewGuid();
        _mappingRepoMock.Setup(r => r.GetByIdAsync(mappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LicenceSubstanceMapping?)null);

        // Act
        var result = await _service.DeleteAsync(mappingId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.NOT_FOUND);
        _mappingRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region ValidateMappingAsync Tests

    [Fact]
    public async Task ValidateMappingAsync_WithValidMapping_ReturnsSuccess()
    {
        // Arrange
        var mapping = CreateMapping();
        var licence = CreateLicence(mapping.LicenceId);
        var substance = CreateSubstance(mapping.SubstanceCode);

        _licenceRepoMock.Setup(r => r.GetByIdAsync(mapping.LicenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);
        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(mapping.SubstanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _service.ValidateMappingAsync(mapping);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateMappingAsync_WithEmptyLicenceId_ReturnsValidationError()
    {
        // Arrange
        var mapping = CreateMapping();
        mapping.LicenceId = Guid.Empty;

        // Act
        var result = await _service.ValidateMappingAsync(mapping);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("LicenceId is required"));
    }

    [Fact]
    public async Task ValidateMappingAsync_WithEmptySubstanceCode_ReturnsValidationError()
    {
        // Arrange
        var mapping = CreateMapping();
        mapping.SubstanceCode = string.Empty;

        // Act
        var result = await _service.ValidateMappingAsync(mapping);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("SubstanceCode is required"));
    }

    #endregion

    #region IsSubstanceAuthorizedByLicenceAsync Tests

    [Fact]
    public async Task IsSubstanceAuthorizedByLicenceAsync_WithActiveMapping_ReturnsTrue()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var substanceCode = "Morphine";
        var activeMappings = new List<LicenceSubstanceMapping>
        {
            CreateMapping(licenceId, substanceCode, DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)))
        };

        _mappingRepoMock.Setup(r => r.GetActiveMappingsByLicenceIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeMappings);

        // Act
        var result = await _service.IsSubstanceAuthorizedByLicenceAsync(licenceId, substanceCode);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSubstanceAuthorizedByLicenceAsync_WithNoActiveMapping_ReturnsFalse()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var substanceCode = "Fentanyl";

        _mappingRepoMock.Setup(r => r.GetActiveMappingsByLicenceIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<LicenceSubstanceMapping>());

        // Act
        var result = await _service.IsSubstanceAuthorizedByLicenceAsync(licenceId, substanceCode);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsSubstanceAuthorizedByLicenceAsync_WithDifferentSubstanceMapping_ReturnsFalse()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var substanceCode = "Morphine";
        var otherSubstanceCode = "Fentanyl";
        var activeMappings = new List<LicenceSubstanceMapping>
        {
            CreateMapping(licenceId, otherSubstanceCode) // Different substance
        };

        _mappingRepoMock.Setup(r => r.GetActiveMappingsByLicenceIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeMappings);

        // Act
        var result = await _service.IsSubstanceAuthorizedByLicenceAsync(licenceId, substanceCode);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static LicenceSubstanceMapping CreateMapping(
        Guid? licenceId = null,
        string? substanceCode = null,
        DateOnly? effectiveDate = null,
        DateOnly? expiryDate = null,
        Guid? mappingId = null)
    {
        return new LicenceSubstanceMapping
        {
            MappingId = mappingId ?? Guid.NewGuid(),
            LicenceId = licenceId ?? Guid.NewGuid(),
            SubstanceCode = substanceCode ?? "SUB-001",
            EffectiveDate = effectiveDate ?? DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)),
            ExpiryDate = expiryDate,
            MaxQuantityPerTransaction = 100,
            MaxQuantityPerPeriod = 1000,
            PeriodType = "Monthly"
        };
    }

    private static Licence CreateLicence(
        Guid? licenceId = null,
        DateOnly? issueDate = null,
        DateOnly? expiryDate = null)
    {
        return new Licence
        {
            LicenceId = licenceId ?? Guid.NewGuid(),
            LicenceNumber = "LIC-001",
            LicenceTypeId = Guid.NewGuid(),
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = issueDate ?? DateOnly.FromDateTime(DateTime.Today.AddYears(-1)),
            ExpiryDate = expiryDate ?? DateOnly.FromDateTime(DateTime.Today.AddYears(4)),
            Status = "Valid"
        };
    }

    private static ControlledSubstance CreateSubstance(
        string? substanceCode = null,
        bool isActive = true)
    {
        return new ControlledSubstance
        {
            SubstanceCode = substanceCode ?? "SUB-001",
            SubstanceName = "Morphine",
            OpiumActList = SubstanceCategories.OpiumActList.ListI,
            IsActive = isActive
        };
    }

    #endregion
}
