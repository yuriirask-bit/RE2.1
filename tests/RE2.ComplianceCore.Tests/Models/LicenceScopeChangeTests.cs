using FluentAssertions;
using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for LicenceScopeChange domain model.
/// T101: Tests LicenceScopeChange per data-model.md entity 14.
/// </summary>
public class LicenceScopeChangeTests
{
    [Fact]
    public void LicenceScopeChange_ShouldHaveCorrectDefaultValues()
    {
        // Arrange & Act
        var change = new LicenceScopeChange();

        // Assert
        change.ChangeId.Should().Be(Guid.Empty);
        change.LicenceId.Should().Be(Guid.Empty);
        change.ChangeDescription.Should().BeEmpty();
        change.RecordedBy.Should().Be(Guid.Empty);
        change.RecorderName.Should().BeNull();
        change.SupportingDocumentId.Should().BeNull();
        change.SubstancesAdded.Should().BeNull();
        change.SubstancesRemoved.Should().BeNull();
        change.ActivitiesAdded.Should().BeNull();
        change.ActivitiesRemoved.Should().BeNull();
    }

    [Fact]
    public void Validate_WithValidScopeChange_ShouldPass()
    {
        // Arrange
        var change = new LicenceScopeChange
        {
            ChangeId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            ChangeDescription = "Added morphine to authorized substances per licence amendment",
            ChangeType = ScopeChangeType.SubstancesAdded,
            RecordedBy = Guid.NewGuid(),
            RecordedDate = DateTime.UtcNow,
            SubstancesAdded = "MORPH-001,MORPH-002"
        };

        // Act
        var result = change.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyLicenceId_ShouldFail()
    {
        // Arrange
        var change = new LicenceScopeChange
        {
            ChangeId = Guid.NewGuid(),
            LicenceId = Guid.Empty,
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ChangeDescription = "Test change",
            RecordedBy = Guid.NewGuid()
        };

        // Act
        var result = change.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("LicenceId is required"));
    }

    [Fact]
    public void Validate_WithEmptyChangeDescription_ShouldFail()
    {
        // Arrange
        var change = new LicenceScopeChange
        {
            ChangeId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ChangeDescription = "",
            RecordedBy = Guid.NewGuid()
        };

        // Act
        var result = change.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("ChangeDescription is required"));
    }

    [Fact]
    public void Validate_WithEmptyRecordedBy_ShouldFail()
    {
        // Arrange
        var change = new LicenceScopeChange
        {
            ChangeId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ChangeDescription = "Test change",
            RecordedBy = Guid.Empty
        };

        // Act
        var result = change.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("RecordedBy is required"));
    }

    [Fact]
    public void Validate_WithEffectiveDateTooFarInFuture_ShouldFail()
    {
        // Arrange
        var change = new LicenceScopeChange
        {
            ChangeId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
            ChangeDescription = "Test change",
            RecordedBy = Guid.NewGuid()
        };

        // Act
        var result = change.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("more than 1 year in the future"));
    }

    [Theory]
    [InlineData("2026-01-15", "2026-01-15", true)]
    [InlineData("2026-01-15", "2026-01-16", true)]
    [InlineData("2026-01-15", "2026-01-14", false)]
    public void IsEffectiveAsOf_ShouldReturnCorrectValue(string effectiveDateStr, string checkDateStr, bool expected)
    {
        // Arrange
        var effectiveDate = DateOnly.Parse(effectiveDateStr);
        var checkDate = DateOnly.Parse(checkDateStr);
        var change = new LicenceScopeChange { EffectiveDate = effectiveDate };

        // Act
        var result = change.IsEffectiveAsOf(checkDate);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetSubstancesAddedArray_ShouldSplitCorrectly()
    {
        // Arrange
        var change = new LicenceScopeChange
        {
            SubstancesAdded = "MORPH-001, FENT-002 , COD-003"
        };

        // Act
        var substances = change.GetSubstancesAddedArray();

        // Assert
        substances.Should().HaveCount(3);
        substances.Should().Contain("MORPH-001");
        substances.Should().Contain("FENT-002");
        substances.Should().Contain("COD-003");
    }

    [Fact]
    public void GetSubstancesAddedArray_WithNull_ShouldReturnEmptyArray()
    {
        // Arrange
        var change = new LicenceScopeChange { SubstancesAdded = null };

        // Act
        var substances = change.GetSubstancesAddedArray();

        // Assert
        substances.Should().BeEmpty();
    }

    [Fact]
    public void GetSubstancesRemovedArray_ShouldSplitCorrectly()
    {
        // Arrange
        var change = new LicenceScopeChange
        {
            SubstancesRemoved = "OLD-001,OLD-002"
        };

        // Act
        var substances = change.GetSubstancesRemovedArray();

        // Assert
        substances.Should().HaveCount(2);
        substances.Should().Contain("OLD-001");
        substances.Should().Contain("OLD-002");
    }

    [Fact]
    public void GetSubstancesRemovedArray_WithNull_ShouldReturnEmptyArray()
    {
        // Arrange
        var change = new LicenceScopeChange { SubstancesRemoved = null };

        // Act
        var substances = change.GetSubstancesRemovedArray();

        // Assert
        substances.Should().BeEmpty();
    }

    [Fact]
    public void AddsSubstances_WithSubstancesAdded_ShouldReturnTrue()
    {
        // Arrange
        var change = new LicenceScopeChange { SubstancesAdded = "MORPH-001" };

        // Act & Assert
        change.AddsSubstances().Should().BeTrue();
    }

    [Fact]
    public void AddsSubstances_WithNoSubstancesAdded_ShouldReturnFalse()
    {
        // Arrange
        var change = new LicenceScopeChange { SubstancesAdded = null };

        // Act & Assert
        change.AddsSubstances().Should().BeFalse();
    }

    [Fact]
    public void RemovesSubstances_WithSubstancesRemoved_ShouldReturnTrue()
    {
        // Arrange
        var change = new LicenceScopeChange { SubstancesRemoved = "OLD-001" };

        // Act & Assert
        change.RemovesSubstances().Should().BeTrue();
    }

    [Fact]
    public void RemovesSubstances_WithNoSubstancesRemoved_ShouldReturnFalse()
    {
        // Arrange
        var change = new LicenceScopeChange { SubstancesRemoved = null };

        // Act & Assert
        change.RemovesSubstances().Should().BeFalse();
    }

    [Theory]
    [InlineData(ScopeChangeType.SubstancesAdded)]
    [InlineData(ScopeChangeType.SubstancesRemoved)]
    [InlineData(ScopeChangeType.ActivitiesAdded)]
    [InlineData(ScopeChangeType.ActivitiesRemoved)]
    [InlineData(ScopeChangeType.GeographicChange)]
    [InlineData(ScopeChangeType.ConditionsModified)]
    [InlineData(ScopeChangeType.Other)]
    public void ScopeChangeType_ShouldSupportAllValues(ScopeChangeType changeType)
    {
        // Arrange
        var change = new LicenceScopeChange
        {
            ChangeId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ChangeDescription = "Test change",
            ChangeType = changeType,
            RecordedBy = Guid.NewGuid(),
            RecordedDate = DateTime.UtcNow
        };

        // Act & Assert
        change.ChangeType.Should().Be(changeType);
        change.Validate().IsValid.Should().BeTrue();
    }

    [Fact]
    public void LicenceScopeChange_ShouldStoreSupportingDocument()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var change = new LicenceScopeChange
        {
            ChangeId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ChangeDescription = "Licence amendment per IGJ letter dated 2026-01-15",
            ChangeType = ScopeChangeType.SubstancesAdded,
            RecordedBy = Guid.NewGuid(),
            RecorderName = "Maria Jansen",
            RecordedDate = DateTime.UtcNow,
            SupportingDocumentId = documentId,
            SubstancesAdded = "MORPH-001"
        };

        // Assert
        change.SupportingDocumentId.Should().Be(documentId);
        change.RecorderName.Should().Be("Maria Jansen");
    }
}
