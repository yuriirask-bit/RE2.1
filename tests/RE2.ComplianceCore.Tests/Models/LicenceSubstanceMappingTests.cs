using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for LicenceSubstanceMapping domain model.
/// T055: Test-driven development for LicenceSubstanceMapping per data-model.md entity 4.
/// T079: Validation for substance-to-licence-type mappings.
/// </summary>
public class LicenceSubstanceMappingTests
{
    [Fact]
    public void LicenceSubstanceMapping_Constructor_InitializesWithValidData()
    {
        // Arrange & Act
        var mapping = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            SubstanceCode = "Morphine",
            MaxQuantityPerTransaction = 1000.0m,
            MaxQuantityPerPeriod = 5000.0m,
            PeriodType = "Monthly",
            Restrictions = "Prescription required",
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))
        };

        // Assert
        Assert.NotEqual(Guid.Empty, mapping.MappingId);
        Assert.NotEqual(Guid.Empty, mapping.LicenceId);
        Assert.False(string.IsNullOrWhiteSpace(mapping.SubstanceCode));
        Assert.Equal(1000.0m, mapping.MaxQuantityPerTransaction);
        Assert.Equal(5000.0m, mapping.MaxQuantityPerPeriod);
        Assert.Equal("Monthly", mapping.PeriodType);
    }

    [Fact]
    public void LicenceSubstanceMapping_Validate_FailsWithEmptyLicenceId()
    {
        // Arrange
        var mapping = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = Guid.Empty,
            SubstanceCode = "Fentanyl",
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        // Act
        var result = mapping.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("LicenceId"));
    }

    [Fact]
    public void LicenceSubstanceMapping_Validate_SucceedsWithValidData()
    {
        // Arrange
        var mapping = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            SubstanceCode = "Morphine",
            MaxQuantityPerTransaction = 500.0m,
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        // Act
        var result = mapping.Validate();

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void LicenceSubstanceMapping_Validate_FailsWhenEffectiveDateNotSet()
    {
        // Arrange
        var mapping = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            SubstanceCode = "SUB-001",
            EffectiveDate = default // Not set
        };

        // Act
        var result = mapping.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("EffectiveDate"));
    }

    [Fact]
    public void LicenceSubstanceMapping_Validate_FailsWhenExpiryDateBeforeEffectiveDate()
    {
        // Arrange
        var mapping = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            SubstanceCode = "Morphine",
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
        };

        // Act
        var result = mapping.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("ExpiryDate") && v.Message.Contains("EffectiveDate"));
    }

    #region T079: ValidateAgainstLicence Tests

    [Fact]
    public void LicenceSubstanceMapping_ValidateAgainstLicence_SucceedsWhenExpiryWithinLicenceExpiry()
    {
        // Arrange
        var licenceExpiry = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2));
        var licence = CreateTestLicence(licenceExpiry);

        var mapping = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = licence.LicenceId,
            SubstanceCode = "Morphine",
            EffectiveDate = licence.IssueDate.AddDays(1),
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)) // Within licence expiry
        };

        // Act
        var result = mapping.ValidateAgainstLicence(licence);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void LicenceSubstanceMapping_ValidateAgainstLicence_SucceedsWhenMappingHasNoExpiry()
    {
        // Arrange
        var licence = CreateTestLicence(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)));

        var mapping = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = licence.LicenceId,
            SubstanceCode = "Fentanyl",
            EffectiveDate = licence.IssueDate.AddDays(1),
            ExpiryDate = null // No separate expiry
        };

        // Act
        var result = mapping.ValidateAgainstLicence(licence);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void LicenceSubstanceMapping_ValidateAgainstLicence_FailsWhenExpiryExceedsLicenceExpiry()
    {
        // Arrange
        var licenceExpiry = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));
        var licence = CreateTestLicence(licenceExpiry);

        var mapping = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = licence.LicenceId,
            SubstanceCode = "Morphine",
            EffectiveDate = licence.IssueDate.AddDays(1),
            ExpiryDate = licenceExpiry.AddDays(30) // Exceeds licence expiry
        };

        // Act
        var result = mapping.ValidateAgainstLicence(licence);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("cannot exceed"));
    }

    [Fact]
    public void LicenceSubstanceMapping_ValidateAgainstLicence_FailsWhenEffectiveDateBeforeLicenceIssueDate()
    {
        // Arrange
        var licence = CreateTestLicence(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)));

        var mapping = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = licence.LicenceId,
            SubstanceCode = "SUB-001",
            EffectiveDate = licence.IssueDate.AddDays(-10), // Before licence was issued
            ExpiryDate = null
        };

        // Act
        var result = mapping.ValidateAgainstLicence(licence);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("cannot be before"));
    }

    [Fact]
    public void LicenceSubstanceMapping_ValidateAgainstLicence_SucceedsWhenLicenceHasNoExpiry()
    {
        // Arrange - Licence has no expiry (permanent licence)
        var licence = CreateTestLicence(expiryDate: null);

        var mapping = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = licence.LicenceId,
            SubstanceCode = "Morphine",
            EffectiveDate = licence.IssueDate.AddDays(1),
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(10)) // Any expiry is fine
        };

        // Act
        var result = mapping.ValidateAgainstLicence(licence);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void LicenceSubstanceMapping_ValidateAgainstLicence_SucceedsWhenEffectiveDateEqualsLicenceIssueDate()
    {
        // Arrange
        var licence = CreateTestLicence(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)));

        var mapping = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = licence.LicenceId,
            SubstanceCode = "Fentanyl",
            EffectiveDate = licence.IssueDate, // Same as issue date (allowed)
            ExpiryDate = null
        };

        // Act
        var result = mapping.ValidateAgainstLicence(licence);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Helper Methods

    private static Licence CreateTestLicence(DateOnly? expiryDate)
    {
        return new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = "TEST-001",
            LicenceTypeId = Guid.NewGuid(),
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
            ExpiryDate = expiryDate,
            Status = "Valid",
            PermittedActivities = LicenceTypes.PermittedActivity.Possess
        };
    }

    #endregion
}
