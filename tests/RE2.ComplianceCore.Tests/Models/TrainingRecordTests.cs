using FluentAssertions;
using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Tests for TrainingRecord domain model.
/// T270: Validates training record business rules per FR-050.
/// </summary>
public class TrainingRecordTests
{
    #region Validate

    [Fact]
    public void Validate_WithValidRecord_ShouldSucceed()
    {
        var record = CreateValidTrainingRecord();
        var result = record.Validate();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyStaffMemberId_ShouldFail()
    {
        var record = CreateValidTrainingRecord();
        record.StaffMemberId = Guid.Empty;
        var result = record.Validate();
        result.IsValid.Should().BeFalse();
        result.Violations.Should().ContainSingle(v => v.Message.Contains("StaffMemberId"));
    }

    [Fact]
    public void Validate_WithMissingTrainingCurriculum_ShouldFail()
    {
        var record = CreateValidTrainingRecord();
        record.TrainingCurriculum = string.Empty;
        var result = record.Validate();
        result.IsValid.Should().BeFalse();
        result.Violations.Should().ContainSingle(v => v.Message.Contains("TrainingCurriculum"));
    }

    [Fact]
    public void Validate_WithFutureCompletionDate_ShouldFail()
    {
        var record = CreateValidTrainingRecord();
        record.CompletionDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        var result = record.Validate();
        result.IsValid.Should().BeFalse();
        result.Violations.Should().ContainSingle(v => v.Message.Contains("CompletionDate"));
    }

    [Fact]
    public void Validate_WithExpiryBeforeCompletion_ShouldFail()
    {
        var record = CreateValidTrainingRecord();
        record.CompletionDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30);
        record.ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-60);
        var result = record.Validate();
        result.IsValid.Should().BeFalse();
        result.Violations.Should().ContainSingle(v => v.Message.Contains("ExpiryDate"));
    }

    [Fact]
    public void Validate_WithMultipleErrors_ShouldReturnAll()
    {
        var record = new TrainingRecord();
        var result = record.Validate();
        result.IsValid.Should().BeFalse();
        result.Violations.Should().HaveCountGreaterOrEqualTo(2);
    }

    #endregion

    #region IsExpired

    [Fact]
    public void IsExpired_WithPastExpiryDate_ShouldReturnTrue()
    {
        var record = CreateValidTrainingRecord();
        record.ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        record.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WithFutureExpiryDate_ShouldReturnFalse()
    {
        var record = CreateValidTrainingRecord();
        record.ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(90);
        record.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WithNoExpiryDate_ShouldReturnFalse()
    {
        var record = CreateValidTrainingRecord();
        record.ExpiryDate = null;
        record.IsExpired().Should().BeFalse();
    }

    #endregion

    #region Enums

    [Fact]
    public void AssessmentResult_ShouldHaveExpectedValues()
    {
        Enum.GetValues<AssessmentResult>().Should().HaveCount(3);
    }

    [Fact]
    public void AssessmentResult_DefaultShouldBeNotAssessed()
    {
        var record = new TrainingRecord();
        record.AssessmentResult.Should().Be(AssessmentResult.NotAssessed);
    }

    #endregion

    private static TrainingRecord CreateValidTrainingRecord() => new()
    {
        TrainingRecordId = Guid.NewGuid(),
        StaffMemberId = Guid.NewGuid(),
        StaffMemberName = "John Doe",
        TrainingCurriculum = "GDP Awareness Training",
        CompletionDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7),
        ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(12),
        TrainerName = "Jane Smith",
        AssessmentResult = AssessmentResult.Pass
    };
}
