using FluentAssertions;
using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for LicenceVerification domain model.
/// T100: Tests LicenceVerification per data-model.md entity 13.
/// </summary>
public class LicenceVerificationTests
{
    [Fact]
    public void LicenceVerification_ShouldHaveCorrectDefaultValues()
    {
        // Arrange & Act
        var verification = new LicenceVerification();

        // Assert
        verification.VerificationId.Should().Be(Guid.Empty);
        verification.LicenceId.Should().Be(Guid.Empty);
        verification.VerifiedBy.Should().Be(Guid.Empty);
        verification.VerifierName.Should().BeNull();
        verification.Notes.Should().BeNull();
        verification.AuthorityReferenceNumber.Should().BeNull();
    }

    [Fact]
    public void Validate_WithValidVerification_ShouldPass()
    {
        // Arrange
        var verification = new LicenceVerification
        {
            VerificationId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            VerificationMethod = VerificationMethod.AuthorityWebsite,
            VerificationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            VerifiedBy = Guid.NewGuid(),
            Outcome = VerificationOutcome.Valid,
            AuthorityReferenceNumber = "IGJ-2026-12345"
        };

        // Act
        var result = verification.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyLicenceId_ShouldFail()
    {
        // Arrange
        var verification = new LicenceVerification
        {
            VerificationId = Guid.NewGuid(),
            LicenceId = Guid.Empty,
            VerificationMethod = VerificationMethod.PhysicalInspection,
            VerificationDate = DateOnly.FromDateTime(DateTime.UtcNow),
            VerifiedBy = Guid.NewGuid(),
            Outcome = VerificationOutcome.Valid
        };

        // Act
        var result = verification.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("LicenceId is required"));
    }

    [Fact]
    public void Validate_WithEmptyVerifiedBy_ShouldFail()
    {
        // Arrange
        var verification = new LicenceVerification
        {
            VerificationId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            VerificationMethod = VerificationMethod.PhysicalInspection,
            VerificationDate = DateOnly.FromDateTime(DateTime.UtcNow),
            VerifiedBy = Guid.Empty,
            Outcome = VerificationOutcome.Valid
        };

        // Act
        var result = verification.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("VerifiedBy is required"));
    }

    [Fact]
    public void Validate_WithFutureVerificationDate_ShouldFail()
    {
        // Arrange
        var verification = new LicenceVerification
        {
            VerificationId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            VerificationMethod = VerificationMethod.EmailConfirmation,
            VerificationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            VerifiedBy = Guid.NewGuid(),
            Outcome = VerificationOutcome.Valid
        };

        // Act
        var result = verification.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("cannot be in the future"));
    }

    [Theory]
    [InlineData(VerificationOutcome.Valid, true)]
    [InlineData(VerificationOutcome.Invalid, false)]
    [InlineData(VerificationOutcome.Pending, false)]
    [InlineData(VerificationOutcome.Inconclusive, false)]
    public void IsValid_ShouldReturnCorrectValue(VerificationOutcome outcome, bool expected)
    {
        // Arrange
        var verification = new LicenceVerification { Outcome = outcome };

        // Act
        var result = verification.IsValid();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsCurrent_WithRecentVerification_ShouldReturnTrue()
    {
        // Arrange
        var verification = new LicenceVerification
        {
            VerificationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6))
        };

        // Act
        var result = verification.IsCurrent();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCurrent_WithOldVerification_ShouldReturnFalse()
    {
        // Arrange
        var verification = new LicenceVerification
        {
            VerificationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2))
        };

        // Act
        var result = verification.IsCurrent();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetAgeDays_ShouldReturnCorrectAge()
    {
        // Arrange
        var verificationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var verification = new LicenceVerification
        {
            VerificationDate = verificationDate
        };

        // Act
        var ageDays = verification.GetAgeDays();

        // Assert
        ageDays.Should().BeGreaterOrEqualTo(30);
        ageDays.Should().BeLessThan(32); // Account for test execution time
    }

    [Theory]
    [InlineData(VerificationMethod.AuthorityWebsite)]
    [InlineData(VerificationMethod.EmailConfirmation)]
    [InlineData(VerificationMethod.PhoneConfirmation)]
    [InlineData(VerificationMethod.PhysicalInspection)]
    [InlineData(VerificationMethod.FarmatecDatabase)]
    [InlineData(VerificationMethod.EudraGMDP)]
    public void VerificationMethod_ShouldSupportAllValues(VerificationMethod method)
    {
        // Arrange
        var verification = new LicenceVerification
        {
            VerificationId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            VerificationMethod = method,
            VerificationDate = DateOnly.FromDateTime(DateTime.UtcNow),
            VerifiedBy = Guid.NewGuid(),
            Outcome = VerificationOutcome.Valid,
            AuthorityReferenceNumber = "REF-123"
        };

        // Act & Assert
        verification.VerificationMethod.Should().Be(method);
        verification.Validate().IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(VerificationOutcome.Valid)]
    [InlineData(VerificationOutcome.Invalid)]
    [InlineData(VerificationOutcome.Pending)]
    [InlineData(VerificationOutcome.Inconclusive)]
    public void VerificationOutcome_ShouldSupportAllValues(VerificationOutcome outcome)
    {
        // Arrange
        var verification = new LicenceVerification
        {
            VerificationId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            VerificationMethod = VerificationMethod.PhysicalInspection,
            VerificationDate = DateOnly.FromDateTime(DateTime.UtcNow),
            VerifiedBy = Guid.NewGuid(),
            Outcome = outcome
        };

        // Act & Assert
        verification.Outcome.Should().Be(outcome);
        verification.Validate().IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_AuthorityWebsiteWithoutReference_ShouldPassButWarn()
    {
        // Arrange - Authority website verification without reference number
        var verification = new LicenceVerification
        {
            VerificationId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            VerificationMethod = VerificationMethod.AuthorityWebsite,
            VerificationDate = DateOnly.FromDateTime(DateTime.UtcNow),
            VerifiedBy = Guid.NewGuid(),
            Outcome = VerificationOutcome.Valid,
            AuthorityReferenceNumber = null // Missing
        };

        // Act
        var result = verification.Validate();

        // Assert - Should still be valid, but with a warning
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LicenceVerification_ShouldStoreVerifierName()
    {
        // Arrange
        var verification = new LicenceVerification
        {
            VerificationId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            VerificationMethod = VerificationMethod.PhysicalInspection,
            VerificationDate = DateOnly.FromDateTime(DateTime.UtcNow),
            VerifiedBy = Guid.NewGuid(),
            VerifierName = "Jan de Vries",
            Outcome = VerificationOutcome.Valid,
            Notes = "Verified original certificate at customer site"
        };

        // Assert
        verification.VerifierName.Should().Be("Jan de Vries");
        verification.Notes.Should().Be("Verified original certificate at customer site");
    }
}
