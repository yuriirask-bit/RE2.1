using FluentAssertions;
using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Tests for GdpChangeRecord domain model.
/// T271: Validates change record business rules per FR-051.
/// </summary>
public class GdpChangeRecordTests
{
    #region Validate

    [Fact]
    public void Validate_WithValidRecord_ShouldSucceed()
    {
        var record = CreateValidChangeRecord();
        var result = record.Validate();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithMissingChangeNumber_ShouldFail()
    {
        var record = CreateValidChangeRecord();
        record.ChangeNumber = string.Empty;
        var result = record.Validate();
        result.IsValid.Should().BeFalse();
        result.Violations.Should().ContainSingle(v => v.Message.Contains("ChangeNumber"));
    }

    [Fact]
    public void Validate_WithMissingDescription_ShouldFail()
    {
        var record = CreateValidChangeRecord();
        record.Description = string.Empty;
        var result = record.Validate();
        result.IsValid.Should().BeFalse();
        result.Violations.Should().ContainSingle(v => v.Message.Contains("Description"));
    }

    [Fact]
    public void Validate_WithImplementationDateButNotApproved_ShouldFail()
    {
        var record = CreateValidChangeRecord();
        record.ApprovalStatus = ChangeApprovalStatus.Pending;
        record.ImplementationDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = record.Validate();
        result.IsValid.Should().BeFalse();
        result.Violations.Should().ContainSingle(v => v.Message.Contains("ImplementationDate"));
    }

    [Fact]
    public void Validate_WithImplementationDateAndApproved_ShouldSucceed()
    {
        var record = CreateValidChangeRecord();
        record.ApprovalStatus = ChangeApprovalStatus.Approved;
        record.ImplementationDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = record.Validate();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithMultipleErrors_ShouldReturnAll()
    {
        var record = new GdpChangeRecord
        {
            ImplementationDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        var result = record.Validate();
        result.IsValid.Should().BeFalse();
        result.Violations.Should().HaveCountGreaterOrEqualTo(3);
    }

    #endregion

    #region IsPending

    [Fact]
    public void IsPending_WhenPending_ShouldReturnTrue()
    {
        var record = CreateValidChangeRecord();
        record.ApprovalStatus = ChangeApprovalStatus.Pending;
        record.IsPending().Should().BeTrue();
    }

    [Fact]
    public void IsPending_WhenApproved_ShouldReturnFalse()
    {
        var record = CreateValidChangeRecord();
        record.ApprovalStatus = ChangeApprovalStatus.Approved;
        record.IsPending().Should().BeFalse();
    }

    #endregion

    #region IsApproved

    [Fact]
    public void IsApproved_WhenApproved_ShouldReturnTrue()
    {
        var record = CreateValidChangeRecord();
        record.ApprovalStatus = ChangeApprovalStatus.Approved;
        record.IsApproved().Should().BeTrue();
    }

    [Fact]
    public void IsApproved_WhenRejected_ShouldReturnFalse()
    {
        var record = CreateValidChangeRecord();
        record.ApprovalStatus = ChangeApprovalStatus.Rejected;
        record.IsApproved().Should().BeFalse();
    }

    #endregion

    #region CanImplement

    [Fact]
    public void CanImplement_WhenApprovedAndNotImplemented_ShouldReturnTrue()
    {
        var record = CreateValidChangeRecord();
        record.ApprovalStatus = ChangeApprovalStatus.Approved;
        record.ImplementationDate = null;
        record.CanImplement().Should().BeTrue();
    }

    [Fact]
    public void CanImplement_WhenApprovedAndAlreadyImplemented_ShouldReturnFalse()
    {
        var record = CreateValidChangeRecord();
        record.ApprovalStatus = ChangeApprovalStatus.Approved;
        record.ImplementationDate = DateOnly.FromDateTime(DateTime.UtcNow);
        record.CanImplement().Should().BeFalse();
    }

    [Fact]
    public void CanImplement_WhenNotApproved_ShouldReturnFalse()
    {
        var record = CreateValidChangeRecord();
        record.ApprovalStatus = ChangeApprovalStatus.Pending;
        record.CanImplement().Should().BeFalse();
    }

    #endregion

    #region Enums

    [Fact]
    public void GdpChangeType_ShouldHaveExpectedValues()
    {
        Enum.GetValues<GdpChangeType>().Should().HaveCount(5);
    }

    [Fact]
    public void ChangeApprovalStatus_ShouldHaveExpectedValues()
    {
        Enum.GetValues<ChangeApprovalStatus>().Should().HaveCount(3);
    }

    #endregion

    #region Defaults

    [Fact]
    public void ApprovalStatus_ShouldDefaultToPending()
    {
        var record = new GdpChangeRecord();
        record.ApprovalStatus.Should().Be(ChangeApprovalStatus.Pending);
    }

    #endregion

    private static GdpChangeRecord CreateValidChangeRecord() => new()
    {
        ChangeRecordId = Guid.NewGuid(),
        ChangeNumber = "CHG-2026-001",
        ChangeType = GdpChangeType.NewWarehouse,
        Description = "Adding new temperature-controlled warehouse in Dublin",
        RiskAssessment = "Low risk - existing GDP procedures apply",
        ApprovalStatus = ChangeApprovalStatus.Pending,
        CreatedDate = DateTime.UtcNow,
        ModifiedDate = DateTime.UtcNow
    };
}
