using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.ComplianceCore.Services.SubstanceManagement;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Tests.Services;

/// <summary>
/// Unit tests for ControlledSubstanceService.
/// T073g: Per FR-003 controlled substance master list management.
/// </summary>
public class ControlledSubstanceServiceTests
{
    private readonly Mock<IControlledSubstanceRepository> _substanceRepoMock;
    private readonly Mock<ILogger<ControlledSubstanceService>> _loggerMock;
    private readonly ControlledSubstanceService _service;

    public ControlledSubstanceServiceTests()
    {
        _substanceRepoMock = new Mock<IControlledSubstanceRepository>();
        _loggerMock = new Mock<ILogger<ControlledSubstanceService>>();

        _service = new ControlledSubstanceService(
            _substanceRepoMock.Object,
            _loggerMock.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingSubstance_ReturnsSubstance()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var substance = CreateSubstance(substanceId);

        _substanceRepoMock.Setup(r => r.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _service.GetByIdAsync(substanceId);

        // Assert
        result.Should().NotBeNull();
        result!.SubstanceId.Should().Be(substanceId);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentSubstance_ReturnsNull()
    {
        // Arrange
        var substanceId = Guid.NewGuid();

        _substanceRepoMock.Setup(r => r.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);

        // Act
        var result = await _service.GetByIdAsync(substanceId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByInternalCodeAsync Tests

    [Fact]
    public async Task GetByInternalCodeAsync_WithExistingCode_ReturnsSubstance()
    {
        // Arrange
        var internalCode = "MOR-001";
        var substance = CreateSubstance(internalCode: internalCode);

        _substanceRepoMock.Setup(r => r.GetByInternalCodeAsync(internalCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _service.GetByInternalCodeAsync(internalCode);

        // Assert
        result.Should().NotBeNull();
        result!.InternalCode.Should().Be(internalCode);
    }

    [Fact]
    public async Task GetByInternalCodeAsync_WithEmptyCode_ReturnsNull()
    {
        // Act
        var result = await _service.GetByInternalCodeAsync("");

        // Assert
        result.Should().BeNull();
        _substanceRepoMock.Verify(r => r.GetByInternalCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByInternalCodeAsync_WithWhitespaceCode_ReturnsNull()
    {
        // Act
        var result = await _service.GetByInternalCodeAsync("   ");

        // Assert
        result.Should().BeNull();
        _substanceRepoMock.Verify(r => r.GetByInternalCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetByOpiumActListAsync Tests

    [Fact]
    public async Task GetByOpiumActListAsync_WithListI_ReturnsFilteredSubstances()
    {
        // Arrange
        var substances = new[]
        {
            CreateSubstance(opiumActList: SubstanceCategories.OpiumActList.ListI),
            CreateSubstance(opiumActList: SubstanceCategories.OpiumActList.ListII),
            CreateSubstance(opiumActList: SubstanceCategories.OpiumActList.ListI)
        };

        _substanceRepoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(substances);

        // Act
        var result = await _service.GetByOpiumActListAsync(SubstanceCategories.OpiumActList.ListI);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.OpiumActList == SubstanceCategories.OpiumActList.ListI);
    }

    #endregion

    #region GetByPrecursorCategoryAsync Tests

    [Fact]
    public async Task GetByPrecursorCategoryAsync_WithCategory1_ReturnsFilteredSubstances()
    {
        // Arrange
        var substances = new[]
        {
            CreateSubstance(precursorCategory: SubstanceCategories.PrecursorCategory.Category1),
            CreateSubstance(precursorCategory: SubstanceCategories.PrecursorCategory.Category2),
            CreateSubstance(precursorCategory: SubstanceCategories.PrecursorCategory.None)
        };

        _substanceRepoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(substances);

        // Act
        var result = await _service.GetByPrecursorCategoryAsync(SubstanceCategories.PrecursorCategory.Category1);

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(s => s.PrecursorCategory == SubstanceCategories.PrecursorCategory.Category1);
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithMatchingName_ReturnsMatchingSubstances()
    {
        // Arrange
        var substances = new[]
        {
            CreateSubstance(substanceName: "Morphine"),
            CreateSubstance(substanceName: "Codeine"),
            CreateSubstance(substanceName: "Morphine Sulfate")
        };

        _substanceRepoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(substances);

        // Act
        var result = await _service.SearchAsync("morphine");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.SubstanceName.ToLowerInvariant().Contains("morphine"));
    }

    [Fact]
    public async Task SearchAsync_WithMatchingCode_ReturnsMatchingSubstances()
    {
        // Arrange
        var substances = new[]
        {
            CreateSubstance(internalCode: "MOR-001"),
            CreateSubstance(internalCode: "COD-001"),
            CreateSubstance(internalCode: "MOR-002")
        };

        _substanceRepoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(substances);

        // Act
        var result = await _service.SearchAsync("MOR");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.InternalCode.ToLowerInvariant().Contains("mor"));
    }

    [Fact]
    public async Task SearchAsync_WithEmptySearchTerm_ReturnsAllActive()
    {
        // Arrange
        var substances = new[]
        {
            CreateSubstance(),
            CreateSubstance(),
            CreateSubstance()
        };

        _substanceRepoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(substances);

        // Act
        var result = await _service.SearchAsync("");

        // Assert
        result.Should().HaveCount(3);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidSubstance_ReturnsId()
    {
        // Arrange
        var substance = CreateSubstance();
        var expectedId = Guid.NewGuid();

        _substanceRepoMock.Setup(r => r.GetByInternalCodeAsync(substance.InternalCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);
        _substanceRepoMock.Setup(r => r.CreateAsync(It.IsAny<ControlledSubstance>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        var (id, result) = await _service.CreateAsync(substance);

        // Assert
        result.IsValid.Should().BeTrue();
        id.Should().Be(expectedId);
        _substanceRepoMock.Verify(r => r.CreateAsync(It.IsAny<ControlledSubstance>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateInternalCode_ReturnsValidationError()
    {
        // Arrange
        var substance = CreateSubstance(internalCode: "EXISTING-001");
        var existingSubstance = CreateSubstance(internalCode: "EXISTING-001");

        _substanceRepoMock.Setup(r => r.GetByInternalCodeAsync(substance.InternalCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSubstance);

        // Act
        var (id, result) = await _service.CreateAsync(substance);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.Message.Contains("already exists"));
    }

    [Fact]
    public async Task CreateAsync_WithNoClassification_ReturnsValidationError()
    {
        // Arrange
        var substance = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Test Substance",
            InternalCode = "TEST-001",
            OpiumActList = SubstanceCategories.OpiumActList.None,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
            IsActive = true
        };

        // Act
        var (id, result) = await _service.CreateAsync(substance);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.Message.Contains("OpiumActList") || v.Message.Contains("PrecursorCategory"));
    }

    [Fact]
    public async Task CreateAsync_WithEmptyName_ReturnsValidationError()
    {
        // Arrange
        var substance = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "",
            InternalCode = "TEST-001",
            OpiumActList = SubstanceCategories.OpiumActList.ListI,
            IsActive = true
        };

        // Act
        var (id, result) = await _service.CreateAsync(substance);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.Message.Contains("name", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingSubstance_Succeeds()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var existingSubstance = CreateSubstance(substanceId);
        var updatedSubstance = CreateSubstance(substanceId, substanceName: "Updated Name");

        _substanceRepoMock.Setup(r => r.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSubstance);

        // Act
        var result = await _service.UpdateAsync(updatedSubstance);

        // Assert
        result.IsValid.Should().BeTrue();
        _substanceRepoMock.Verify(r => r.UpdateAsync(It.IsAny<ControlledSubstance>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentSubstance_ReturnsNotFoundError()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var substance = CreateSubstance(substanceId);

        _substanceRepoMock.Setup(r => r.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);

        // Act
        var result = await _service.UpdateAsync(substance);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.SUBSTANCE_NOT_FOUND);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateInternalCode_ReturnsValidationError()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var existingSubstance = CreateSubstance(substanceId, internalCode: "OLD-001");
        var updatedSubstance = CreateSubstance(substanceId, internalCode: "EXISTING-002");
        var duplicateSubstance = CreateSubstance(internalCode: "EXISTING-002");

        _substanceRepoMock.Setup(r => r.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSubstance);
        _substanceRepoMock.Setup(r => r.GetByInternalCodeAsync("EXISTING-002", It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicateSubstance);

        // Act
        var result = await _service.UpdateAsync(updatedSubstance);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("already exists"));
    }

    [Fact]
    public async Task UpdateAsync_WithSameInternalCode_Succeeds()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var existingSubstance = CreateSubstance(substanceId, internalCode: "SAME-001");
        var updatedSubstance = CreateSubstance(substanceId, internalCode: "SAME-001", substanceName: "Updated Name");

        _substanceRepoMock.Setup(r => r.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSubstance);

        // Act
        var result = await _service.UpdateAsync(updatedSubstance);

        // Assert
        result.IsValid.Should().BeTrue();
        _substanceRepoMock.Verify(r => r.GetByInternalCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingSubstance_Succeeds()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var substance = CreateSubstance(substanceId);

        _substanceRepoMock.Setup(r => r.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _service.DeleteAsync(substanceId);

        // Assert
        result.IsValid.Should().BeTrue();
        _substanceRepoMock.Verify(r => r.DeleteAsync(substanceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentSubstance_ReturnsNotFoundError()
    {
        // Arrange
        var substanceId = Guid.NewGuid();

        _substanceRepoMock.Setup(r => r.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);

        // Act
        var result = await _service.DeleteAsync(substanceId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.SUBSTANCE_NOT_FOUND);
        _substanceRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeactivateAsync Tests

    [Fact]
    public async Task DeactivateAsync_WithActiveSubstance_Succeeds()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var substance = CreateSubstance(substanceId);
        substance.IsActive = true;

        _substanceRepoMock.Setup(r => r.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _service.DeactivateAsync(substanceId);

        // Assert
        result.IsValid.Should().BeTrue();
        _substanceRepoMock.Verify(r => r.UpdateAsync(
            It.Is<ControlledSubstance>(s => !s.IsActive),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_WithInactiveSubstance_ReturnsValidationError()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var substance = CreateSubstance(substanceId);
        substance.IsActive = false;

        _substanceRepoMock.Setup(r => r.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _service.DeactivateAsync(substanceId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("already inactive"));
    }

    [Fact]
    public async Task DeactivateAsync_WithNonExistentSubstance_ReturnsNotFoundError()
    {
        // Arrange
        var substanceId = Guid.NewGuid();

        _substanceRepoMock.Setup(r => r.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);

        // Act
        var result = await _service.DeactivateAsync(substanceId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.SUBSTANCE_NOT_FOUND);
    }

    #endregion

    #region ReactivateAsync Tests

    [Fact]
    public async Task ReactivateAsync_WithInactiveSubstance_Succeeds()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var substance = CreateSubstance(substanceId);
        substance.IsActive = false;

        _substanceRepoMock.Setup(r => r.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _service.ReactivateAsync(substanceId);

        // Assert
        result.IsValid.Should().BeTrue();
        _substanceRepoMock.Verify(r => r.UpdateAsync(
            It.Is<ControlledSubstance>(s => s.IsActive),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReactivateAsync_WithActiveSubstance_ReturnsValidationError()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var substance = CreateSubstance(substanceId);
        substance.IsActive = true;

        _substanceRepoMock.Setup(r => r.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _service.ReactivateAsync(substanceId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("already active"));
    }

    [Fact]
    public async Task ReactivateAsync_WithNonExistentSubstance_ReturnsNotFoundError()
    {
        // Arrange
        var substanceId = Guid.NewGuid();

        _substanceRepoMock.Setup(r => r.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);

        // Act
        var result = await _service.ReactivateAsync(substanceId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.SUBSTANCE_NOT_FOUND);
    }

    #endregion

    #region ValidateSubstanceAsync Tests

    [Fact]
    public async Task ValidateSubstanceAsync_WithValidSubstance_ReturnsSuccess()
    {
        // Arrange
        var substance = CreateSubstance();

        // Act
        var result = await _service.ValidateSubstanceAsync(substance);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateSubstanceAsync_WithEmptyInternalCode_ReturnsValidationError()
    {
        // Arrange
        var substance = new ControlledSubstance
        {
            SubstanceId = Guid.NewGuid(),
            SubstanceName = "Test Substance",
            InternalCode = "",
            OpiumActList = SubstanceCategories.OpiumActList.ListI,
            IsActive = true
        };

        // Act
        var result = await _service.ValidateSubstanceAsync(substance);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("code", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Helper Methods

    private static ControlledSubstance CreateSubstance(
        Guid? substanceId = null,
        string substanceName = "Test Substance",
        string internalCode = "TEST-001",
        SubstanceCategories.OpiumActList opiumActList = SubstanceCategories.OpiumActList.ListII,
        SubstanceCategories.PrecursorCategory precursorCategory = SubstanceCategories.PrecursorCategory.None)
    {
        return new ControlledSubstance
        {
            SubstanceId = substanceId ?? Guid.NewGuid(),
            SubstanceName = substanceName,
            InternalCode = internalCode,
            OpiumActList = opiumActList,
            PrecursorCategory = precursorCategory,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }

    #endregion
}
