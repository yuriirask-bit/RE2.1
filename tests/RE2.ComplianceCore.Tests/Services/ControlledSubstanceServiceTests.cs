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

    #region GetBySubstanceCodeAsync Tests

    [Fact]
    public async Task GetBySubstanceCodeAsync_WithExistingSubstance_ReturnsSubstance()
    {
        // Arrange
        var substanceCode = "MOR-001";
        var substance = CreateSubstance(substanceCode: substanceCode);

        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _service.GetBySubstanceCodeAsync(substanceCode);

        // Assert
        result.Should().NotBeNull();
        result!.SubstanceCode.Should().Be(substanceCode);
    }

    [Fact]
    public async Task GetBySubstanceCodeAsync_WithNonExistentSubstance_ReturnsNull()
    {
        // Arrange
        var substanceCode = "NONEXISTENT";

        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);

        // Act
        var result = await _service.GetBySubstanceCodeAsync(substanceCode);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBySubstanceCodeAsync_WithEmptyCode_ReturnsNull()
    {
        // Act
        var result = await _service.GetBySubstanceCodeAsync("");

        // Assert
        result.Should().BeNull();
        _substanceRepoMock.Verify(r => r.GetBySubstanceCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetBySubstanceCodeAsync_WithWhitespaceCode_ReturnsNull()
    {
        // Act
        var result = await _service.GetBySubstanceCodeAsync("   ");

        // Assert
        result.Should().BeNull();
        _substanceRepoMock.Verify(r => r.GetBySubstanceCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetByOpiumActListAsync Tests

    [Fact]
    public async Task GetByOpiumActListAsync_WithListI_ReturnsFilteredSubstances()
    {
        // Arrange
        var substances = new[]
        {
            CreateSubstance(substanceCode: "SUB-001", opiumActList: SubstanceCategories.OpiumActList.ListI),
            CreateSubstance(substanceCode: "SUB-002", opiumActList: SubstanceCategories.OpiumActList.ListII),
            CreateSubstance(substanceCode: "SUB-003", opiumActList: SubstanceCategories.OpiumActList.ListI)
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
            CreateSubstance(substanceCode: "SUB-001", precursorCategory: SubstanceCategories.PrecursorCategory.Category1),
            CreateSubstance(substanceCode: "SUB-002", precursorCategory: SubstanceCategories.PrecursorCategory.Category2),
            CreateSubstance(substanceCode: "SUB-003", precursorCategory: SubstanceCategories.PrecursorCategory.None)
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
            CreateSubstance(substanceCode: "MOR-001", substanceName: "Morphine"),
            CreateSubstance(substanceCode: "COD-001", substanceName: "Codeine"),
            CreateSubstance(substanceCode: "MOR-002", substanceName: "Morphine Sulfate")
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
            CreateSubstance(substanceCode: "MOR-001"),
            CreateSubstance(substanceCode: "COD-001"),
            CreateSubstance(substanceCode: "MOR-002")
        };

        _substanceRepoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(substances);

        // Act
        var result = await _service.SearchAsync("MOR");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.SubstanceCode.ToLowerInvariant().Contains("mor"));
    }

    [Fact]
    public async Task SearchAsync_WithEmptySearchTerm_ReturnsAllActive()
    {
        // Arrange
        var substances = new[]
        {
            CreateSubstance(substanceCode: "SUB-001"),
            CreateSubstance(substanceCode: "SUB-002"),
            CreateSubstance(substanceCode: "SUB-003")
        };

        _substanceRepoMock.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(substances);

        // Act
        var result = await _service.SearchAsync("");

        // Assert
        result.Should().HaveCount(3);
    }

    #endregion

    #region ConfigureComplianceAsync Tests

    [Fact]
    public async Task ConfigureComplianceAsync_WithValidSubstance_ReturnsSuccess()
    {
        // Arrange
        var substance = CreateSubstance();

        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substance.SubstanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);

        // Act
        var result = await _service.ConfigureComplianceAsync(substance);

        // Assert
        result.IsValid.Should().BeTrue();
        _substanceRepoMock.Verify(r => r.SaveComplianceExtensionAsync(It.IsAny<ControlledSubstance>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfigureComplianceAsync_WithDuplicateSubstanceCode_ReturnsValidationError()
    {
        // Arrange
        var substance = CreateSubstance(substanceCode: "EXISTING-001");
        var existingSubstance = CreateSubstance(substanceCode: "EXISTING-001");
        existingSubstance.ComplianceExtensionId = Guid.NewGuid(); // mark as already configured

        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substance.SubstanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSubstance);

        // Act
        var result = await _service.ConfigureComplianceAsync(substance);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("already exists"));
    }

    [Fact]
    public async Task ConfigureComplianceAsync_WithNoClassification_ReturnsValidationError()
    {
        // Arrange
        var substance = new ControlledSubstance
        {
            SubstanceCode = "TEST-001",
            SubstanceName = "Test Substance",
            OpiumActList = SubstanceCategories.OpiumActList.None,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
            IsActive = true
        };

        // Act
        var result = await _service.ConfigureComplianceAsync(substance);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("OpiumActList") || v.Message.Contains("PrecursorCategory"));
    }

    [Fact]
    public async Task ConfigureComplianceAsync_WithEmptyName_ReturnsValidationError()
    {
        // Arrange
        var substance = new ControlledSubstance
        {
            SubstanceCode = "TEST-001",
            SubstanceName = "",
            OpiumActList = SubstanceCategories.OpiumActList.ListI,
            IsActive = true
        };

        // Act
        var result = await _service.ConfigureComplianceAsync(substance);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("name", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region UpdateComplianceAsync Tests

    [Fact]
    public async Task UpdateComplianceAsync_WithExistingSubstance_Succeeds()
    {
        // Arrange
        var substanceCode = "TEST-001";
        var existingSubstance = CreateSubstance(substanceCode: substanceCode);
        var updatedSubstance = CreateSubstance(substanceCode: substanceCode, substanceName: "Updated Name");

        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSubstance);

        // Act
        var result = await _service.UpdateComplianceAsync(updatedSubstance);

        // Assert
        result.IsValid.Should().BeTrue();
        _substanceRepoMock.Verify(r => r.UpdateComplianceExtensionAsync(It.IsAny<ControlledSubstance>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateComplianceAsync_WithNonExistentSubstance_ReturnsNotFoundError()
    {
        // Arrange
        var substanceCode = "NONEXISTENT";
        var substance = CreateSubstance(substanceCode: substanceCode);

        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);

        // Act
        var result = await _service.UpdateComplianceAsync(substance);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.SUBSTANCE_NOT_FOUND);
    }

    #endregion

    #region DeactivateAsync Tests

    [Fact]
    public async Task DeactivateAsync_WithActiveSubstance_Succeeds()
    {
        // Arrange
        var substanceCode = "TEST-001";
        var substance = CreateSubstance(substanceCode: substanceCode);
        substance.IsActive = true;

        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _service.DeactivateAsync(substanceCode);

        // Assert
        result.IsValid.Should().BeTrue();
        _substanceRepoMock.Verify(r => r.UpdateComplianceExtensionAsync(
            It.Is<ControlledSubstance>(s => !s.IsActive),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_WithInactiveSubstance_ReturnsValidationError()
    {
        // Arrange
        var substanceCode = "TEST-001";
        var substance = CreateSubstance(substanceCode: substanceCode);
        substance.IsActive = false;

        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _service.DeactivateAsync(substanceCode);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("already inactive"));
    }

    [Fact]
    public async Task DeactivateAsync_WithNonExistentSubstance_ReturnsNotFoundError()
    {
        // Arrange
        var substanceCode = "NONEXISTENT";

        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);

        // Act
        var result = await _service.DeactivateAsync(substanceCode);

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
        var substanceCode = "TEST-001";
        var substance = CreateSubstance(substanceCode: substanceCode);
        substance.IsActive = false;

        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _service.ReactivateAsync(substanceCode);

        // Assert
        result.IsValid.Should().BeTrue();
        _substanceRepoMock.Verify(r => r.UpdateComplianceExtensionAsync(
            It.Is<ControlledSubstance>(s => s.IsActive),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReactivateAsync_WithActiveSubstance_ReturnsValidationError()
    {
        // Arrange
        var substanceCode = "TEST-001";
        var substance = CreateSubstance(substanceCode: substanceCode);
        substance.IsActive = true;

        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _service.ReactivateAsync(substanceCode);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("already active"));
    }

    [Fact]
    public async Task ReactivateAsync_WithNonExistentSubstance_ReturnsNotFoundError()
    {
        // Arrange
        var substanceCode = "NONEXISTENT";

        _substanceRepoMock.Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);

        // Act
        var result = await _service.ReactivateAsync(substanceCode);

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
    public async Task ValidateSubstanceAsync_WithEmptySubstanceCode_ReturnsValidationError()
    {
        // Arrange
        var substance = new ControlledSubstance
        {
            SubstanceCode = "",
            SubstanceName = "Test Substance",
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

    private static int _substanceCounter;

    private static ControlledSubstance CreateSubstance(
        string? substanceCode = null,
        string substanceName = "Test Substance",
        SubstanceCategories.OpiumActList opiumActList = SubstanceCategories.OpiumActList.ListII,
        SubstanceCategories.PrecursorCategory precursorCategory = SubstanceCategories.PrecursorCategory.None)
    {
        return new ControlledSubstance
        {
            SubstanceCode = substanceCode ?? $"TEST-{Interlocked.Increment(ref _substanceCounter):D3}",
            SubstanceName = substanceName,
            OpiumActList = opiumActList,
            PrecursorCategory = precursorCategory,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }

    #endregion
}
