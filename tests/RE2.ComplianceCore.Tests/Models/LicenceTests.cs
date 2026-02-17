using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Extensions;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for Licence domain model.
/// T056: Test-driven development for Licence per data-model.md entity 1.
/// </summary>
public class LicenceTests
{
    [Fact]
    public void Licence_Constructor_InitializesWithValidData()
    {
        // Arrange & Act
        var issueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6));
        var expiryDate = issueDate.AddMonths(12);

        var licence = new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = "WDA-2024-001",
            LicenceTypeId = Guid.NewGuid(),
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = issueDate,
            ExpiryDate = expiryDate,
            Status = "Valid",
            Scope = "All medicinal products",
            PermittedActivities = LicenceTypes.PermittedActivity.Possess | LicenceTypes.PermittedActivity.Distribute
        };

        // Assert
        Assert.NotEqual(Guid.Empty, licence.LicenceId);
        Assert.Equal("WDA-2024-001", licence.LicenceNumber);
        Assert.Equal("IGJ", licence.IssuingAuthority);
        Assert.Equal("Valid", licence.Status);
    }

    [Fact]
    public void Licence_IsExpired_ReturnsTrueForPastExpiryDate()
    {
        // Arrange
        var licence = new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = "OA-2023-001",
            LicenceTypeId = Guid.NewGuid(),
            HolderType = "Customer",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-18)),
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            Status = "Expired"
        };

        // Act & Assert
        Assert.True(licence.ExpiryDate!.Value.IsExpired());
    }

    [Fact]
    public void Licence_IsExpired_ReturnsFalseForFutureExpiryDate()
    {
        // Arrange
        var licence = new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = "OA-2024-001",
            LicenceTypeId = Guid.NewGuid(),
            HolderType = "Customer",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
            Status = "Valid"
        };

        // Act & Assert
        Assert.False(licence.ExpiryDate!.Value.IsExpired());
    }

    [Fact]
    public void Licence_Validate_FailsWhenExpiryDateBeforeIssueDate()
    {
        // Arrange
        var licence = new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = "INVALID-001",
            LicenceTypeId = Guid.NewGuid(),
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            Status = "Valid"
        };

        // Act
        var result = licence.Validate();

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("ExpiryDate") && v.Message.Contains("IssueDate"));
    }

    [Fact]
    public void Licence_Validate_SucceedsWithNoExpiryDate()
    {
        // Arrange
        var licence = new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = "PERM-001",
            LicenceTypeId = Guid.NewGuid(),
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "Ministry",
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            ExpiryDate = null,
            Status = "Valid"
        };

        // Act
        var result = licence.Validate();

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Licence_Validate_SucceedsWithValidData()
    {
        // Arrange
        var licence = new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = "WDA-2024-100",
            LicenceTypeId = Guid.NewGuid(),
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
            Status = "Valid",
            PermittedActivities = LicenceTypes.PermittedActivity.Distribute
        };

        // Act
        var result = licence.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Violations);
    }

    [Theory]
    [InlineData("Valid")]
    [InlineData("Expired")]
    [InlineData("Suspended")]
    [InlineData("Revoked")]
    public void Licence_Status_AcceptsValidValues(string status)
    {
        // Arrange & Act
        var licence = new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = "TEST-001",
            LicenceTypeId = Guid.NewGuid(),
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = status
        };

        // Assert
        Assert.Equal(status, licence.Status);
    }

    #region T078: Permitted Activities Validation Tests

    [Fact]
    public void Licence_ValidatePermittedActivities_SucceedsWhenSubsetOfLicenceType()
    {
        // Arrange - Licence type allows Possess, Store, Distribute
        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "WDA",
            IssuingAuthority = "IGJ",
            PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                 LicenceTypes.PermittedActivity.Store |
                                 LicenceTypes.PermittedActivity.Distribute,
            IsActive = true
        };

        // Licence only has Possess and Distribute (subset of type's activities)
        var licence = new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = "WDA-2024-001",
            LicenceTypeId = licenceType.LicenceTypeId,
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = "Valid",
            PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                 LicenceTypes.PermittedActivity.Distribute
        };

        // Act
        var result = licence.ValidatePermittedActivities(licenceType);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Licence_ValidatePermittedActivities_SucceedsWhenExactMatch()
    {
        // Arrange
        var activities = LicenceTypes.PermittedActivity.Possess |
                        LicenceTypes.PermittedActivity.Store |
                        LicenceTypes.PermittedActivity.Distribute;

        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "WDA",
            IssuingAuthority = "IGJ",
            PermittedActivities = activities,
            IsActive = true
        };

        var licence = new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = "WDA-2024-001",
            LicenceTypeId = licenceType.LicenceTypeId,
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = "Valid",
            PermittedActivities = activities
        };

        // Act
        var result = licence.ValidatePermittedActivities(licenceType);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Licence_ValidatePermittedActivities_FailsWhenLicenceHasUnauthorizedActivities()
    {
        // Arrange - Licence type only allows Possess and Store
        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Storage Permit",
            IssuingAuthority = "IGJ",
            PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                 LicenceTypes.PermittedActivity.Store,
            IsActive = true
        };

        // Licence tries to add Import activity (not allowed by type)
        var licence = new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = "SP-2024-001",
            LicenceTypeId = licenceType.LicenceTypeId,
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = "Valid",
            PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                 LicenceTypes.PermittedActivity.Store |
                                 LicenceTypes.PermittedActivity.Import // NOT ALLOWED
        };

        // Act
        var result = licence.ValidatePermittedActivities(licenceType);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("Import") && v.Message.Contains("Unauthorized"));
    }

    [Fact]
    public void Licence_ValidatePermittedActivities_FailsWithMultipleUnauthorizedActivities()
    {
        // Arrange - Licence type only allows Possess
        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Possession Permit",
            IssuingAuthority = "Farmatec",
            PermittedActivities = LicenceTypes.PermittedActivity.Possess,
            IsActive = true
        };

        // Licence tries to add multiple unauthorized activities
        var licence = new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = "PP-2024-001",
            LicenceTypeId = licenceType.LicenceTypeId,
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "Farmatec",
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = "Valid",
            PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                 LicenceTypes.PermittedActivity.Distribute |
                                 LicenceTypes.PermittedActivity.Export
        };

        // Act
        var result = licence.ValidatePermittedActivities(licenceType);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("Distribute"));
        Assert.Contains(result.Violations, v => v.Message.Contains("Export"));
    }

    [Fact]
    public void Licence_ValidatePermittedActivities_SucceedsWithNoActivities()
    {
        // Arrange - Both have no activities
        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Registration Only",
            IssuingAuthority = "IGJ",
            PermittedActivities = LicenceTypes.PermittedActivity.None,
            IsActive = true
        };

        var licence = new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = "REG-2024-001",
            LicenceTypeId = licenceType.LicenceTypeId,
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = "Valid",
            PermittedActivities = LicenceTypes.PermittedActivity.None
        };

        // Act
        var result = licence.ValidatePermittedActivities(licenceType);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion
}
