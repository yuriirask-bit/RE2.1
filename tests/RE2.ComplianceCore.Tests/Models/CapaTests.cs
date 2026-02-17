using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for Capa domain model.
/// T214: TDD tests for Capa per User Story 9 (FR-041, FR-042).
/// </summary>
public class CapaTests
{
    #region CapaStatus Enum Tests

    [Theory]
    [InlineData(CapaStatus.Open, "Open")]
    [InlineData(CapaStatus.Overdue, "Overdue")]
    [InlineData(CapaStatus.Completed, "Completed")]
    public void CapaStatus_AllStatusesAreDefined(CapaStatus status, string expectedName)
    {
        Assert.Equal(expectedName, status.ToString());
    }

    [Fact]
    public void CapaStatus_HasExpectedValues()
    {
        var values = Enum.GetValues<CapaStatus>();
        Assert.Equal(3, values.Length);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Capa_DefaultValues_AreCorrect()
    {
        var capa = new Capa();

        Assert.Equal(Guid.Empty, capa.CapaId);
        Assert.Equal(string.Empty, capa.CapaNumber);
        Assert.Equal(Guid.Empty, capa.FindingId);
        Assert.Equal(string.Empty, capa.Description);
        Assert.Equal(string.Empty, capa.OwnerName);
        Assert.Equal(default, capa.DueDate);
        Assert.Null(capa.CompletionDate);
        Assert.Equal(CapaStatus.Open, capa.Status);
        Assert.Null(capa.VerificationNotes);
    }

    [Fact]
    public void Capa_SetProperties_ReturnsCorrectValues()
    {
        var id = Guid.NewGuid();
        var findingId = Guid.NewGuid();
        var dueDate = new DateOnly(2026, 6, 30);

        var capa = new Capa
        {
            CapaId = id,
            CapaNumber = "CAPA-2026-001",
            FindingId = findingId,
            Description = "Implement temperature mapping for cold storage area B",
            OwnerName = "Jan de Vries",
            DueDate = dueDate,
            Status = CapaStatus.Open,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };

        Assert.Equal(id, capa.CapaId);
        Assert.Equal("CAPA-2026-001", capa.CapaNumber);
        Assert.Equal(findingId, capa.FindingId);
        Assert.Equal("Implement temperature mapping for cold storage area B", capa.Description);
        Assert.Equal("Jan de Vries", capa.OwnerName);
        Assert.Equal(dueDate, capa.DueDate);
        Assert.Equal(CapaStatus.Open, capa.Status);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Capa_Validate_ValidCapa_ReturnsSuccess()
    {
        var capa = CreateValidCapa();

        var result = capa.Validate();

        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public void Capa_Validate_EmptyCapaNumber_ReturnsFailure()
    {
        var capa = CreateValidCapa();
        capa.CapaNumber = string.Empty;

        var result = capa.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("CapaNumber"));
    }

    [Fact]
    public void Capa_Validate_EmptyDescription_ReturnsFailure()
    {
        var capa = CreateValidCapa();
        capa.Description = string.Empty;

        var result = capa.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("Description"));
    }

    [Fact]
    public void Capa_Validate_EmptyOwnerName_ReturnsFailure()
    {
        var capa = CreateValidCapa();
        capa.OwnerName = string.Empty;

        var result = capa.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("OwnerName"));
    }

    [Fact]
    public void Capa_Validate_EmptyFindingId_ReturnsFailure()
    {
        var capa = CreateValidCapa();
        capa.FindingId = Guid.Empty;

        var result = capa.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("FindingId"));
    }

    [Fact]
    public void Capa_Validate_CompletedWithoutCompletionDate_ReturnsFailure()
    {
        var capa = CreateValidCapa();
        capa.Status = CapaStatus.Completed;
        capa.CompletionDate = null;

        var result = capa.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("CompletionDate"));
    }

    [Fact]
    public void Capa_Validate_CompletedWithCompletionDate_ReturnsSuccess()
    {
        var capa = CreateValidCapa();
        capa.Status = CapaStatus.Completed;
        capa.CompletionDate = new DateOnly(2026, 5, 15);

        var result = capa.Validate();

        Assert.True(result.IsValid);
    }

    #endregion

    #region IsOverdue Tests

    [Fact]
    public void Capa_IsOverdue_PastDueDateWithNoCompletion_ReturnsTrue()
    {
        var capa = CreateValidCapa();
        capa.DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5);
        capa.CompletionDate = null;

        Assert.True(capa.IsOverdue());
    }

    [Fact]
    public void Capa_IsOverdue_FutureDueDate_ReturnsFalse()
    {
        var capa = CreateValidCapa();
        capa.DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        capa.CompletionDate = null;

        Assert.False(capa.IsOverdue());
    }

    [Fact]
    public void Capa_IsOverdue_PastDueDateWithCompletion_ReturnsFalse()
    {
        var capa = CreateValidCapa();
        capa.DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5);
        capa.CompletionDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3);

        Assert.False(capa.IsOverdue());
    }

    #endregion

    #region Complete Method Tests

    [Fact]
    public void Capa_Complete_SetsStatusAndCompletionDate()
    {
        var capa = CreateValidCapa();
        var completionDate = new DateOnly(2026, 5, 15);

        capa.Complete(completionDate, "Verified temperature mapping completed");

        Assert.Equal(CapaStatus.Completed, capa.Status);
        Assert.Equal(completionDate, capa.CompletionDate);
        Assert.Equal("Verified temperature mapping completed", capa.VerificationNotes);
    }

    [Fact]
    public void Capa_Complete_WithoutNotes_SetsStatusAndDate()
    {
        var capa = CreateValidCapa();
        var completionDate = new DateOnly(2026, 5, 15);

        capa.Complete(completionDate);

        Assert.Equal(CapaStatus.Completed, capa.Status);
        Assert.Equal(completionDate, capa.CompletionDate);
        Assert.Null(capa.VerificationNotes);
    }

    [Fact]
    public void Capa_Complete_UpdatesModifiedDate()
    {
        var capa = CreateValidCapa();
        var originalModified = capa.ModifiedDate;

        capa.Complete(new DateOnly(2026, 5, 15));

        Assert.True(capa.ModifiedDate >= originalModified);
    }

    #endregion

    #region Helpers

    private static Capa CreateValidCapa() => new()
    {
        CapaId = Guid.NewGuid(),
        CapaNumber = "CAPA-2026-001",
        FindingId = Guid.NewGuid(),
        Description = "Implement temperature mapping for cold storage area B",
        OwnerName = "Jan de Vries",
        DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(60),
        Status = CapaStatus.Open,
        CreatedDate = DateTime.UtcNow,
        ModifiedDate = DateTime.UtcNow
    };

    #endregion
}
