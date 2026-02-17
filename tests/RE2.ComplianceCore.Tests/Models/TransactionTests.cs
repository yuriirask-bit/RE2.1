using FluentAssertions;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for Transaction domain model.
/// T149a: Transaction model tests per FR-018 through FR-024.
/// </summary>
public class TransactionTests
{
    #region IsCrossBorder Tests

    [Fact]
    public void IsCrossBorder_ReturnsTrue_WhenOriginAndDestinationDiffer()
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.OriginCountry = "NL";
        transaction.DestinationCountry = "DE";

        // Act & Assert
        transaction.IsCrossBorder().Should().BeTrue();
    }

    [Fact]
    public void IsCrossBorder_ReturnsFalse_WhenOriginAndDestinationSame()
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.OriginCountry = "NL";
        transaction.DestinationCountry = "NL";

        // Act & Assert
        transaction.IsCrossBorder().Should().BeFalse();
    }

    [Fact]
    public void IsCrossBorder_ReturnsFalse_WhenDestinationNull()
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.OriginCountry = "NL";
        transaction.DestinationCountry = null;

        // Act & Assert
        transaction.IsCrossBorder().Should().BeFalse();
    }

    [Fact]
    public void IsCrossBorder_ReturnsFalse_WhenDestinationEmpty()
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.OriginCountry = "NL";
        transaction.DestinationCountry = "";

        // Act & Assert
        transaction.IsCrossBorder().Should().BeFalse();
    }

    [Theory]
    [InlineData("nl", "NL")]
    [InlineData("NL", "nl")]
    [InlineData("Nl", "nL")]
    public void IsCrossBorder_IsCaseInsensitive(string origin, string destination)
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.OriginCountry = origin;
        transaction.DestinationCountry = destination;

        // Act & Assert
        transaction.IsCrossBorder().Should().BeFalse();
    }

    #endregion

    #region RequiresImportPermit Tests

    [Fact]
    public void RequiresImportPermit_ReturnsTrue_WhenCrossBorderInbound()
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.OriginCountry = "DE";
        transaction.DestinationCountry = "NL";
        transaction.Direction = TransactionDirection.Inbound;

        // Act & Assert
        transaction.RequiresImportPermit().Should().BeTrue();
    }

    [Fact]
    public void RequiresImportPermit_ReturnsFalse_WhenDomestic()
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.OriginCountry = "NL";
        transaction.DestinationCountry = "NL";
        transaction.Direction = TransactionDirection.Inbound;

        // Act & Assert
        transaction.RequiresImportPermit().Should().BeFalse();
    }

    [Fact]
    public void RequiresImportPermit_ReturnsFalse_WhenCrossBorderOutbound()
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.OriginCountry = "NL";
        transaction.DestinationCountry = "DE";
        transaction.Direction = TransactionDirection.Outbound;

        // Act & Assert
        transaction.RequiresImportPermit().Should().BeFalse();
    }

    #endregion

    #region RequiresExportPermit Tests

    [Fact]
    public void RequiresExportPermit_ReturnsTrue_WhenCrossBorderOutbound()
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.OriginCountry = "NL";
        transaction.DestinationCountry = "DE";
        transaction.Direction = TransactionDirection.Outbound;

        // Act & Assert
        transaction.RequiresExportPermit().Should().BeTrue();
    }

    [Fact]
    public void RequiresExportPermit_ReturnsFalse_WhenDomestic()
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.OriginCountry = "NL";
        transaction.DestinationCountry = "NL";
        transaction.Direction = TransactionDirection.Outbound;

        // Act & Assert
        transaction.RequiresExportPermit().Should().BeFalse();
    }

    [Fact]
    public void RequiresExportPermit_ReturnsFalse_WhenCrossBorderInbound()
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.OriginCountry = "DE";
        transaction.DestinationCountry = "NL";
        transaction.Direction = TransactionDirection.Inbound;

        // Act & Assert
        transaction.RequiresExportPermit().Should().BeFalse();
    }

    #endregion

    #region CanProceed Tests

    [Theory]
    [InlineData(ValidationStatus.Passed)]
    [InlineData(ValidationStatus.ApprovedWithOverride)]
    public void CanProceed_ReturnsTrue_ForPassingStatuses(ValidationStatus status)
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.ValidationStatus = status;

        // Act & Assert
        transaction.CanProceed().Should().BeTrue();
    }

    [Theory]
    [InlineData(ValidationStatus.Pending)]
    [InlineData(ValidationStatus.Failed)]
    [InlineData(ValidationStatus.RejectedOverride)]
    public void CanProceed_ReturnsFalse_ForNonPassingStatuses(ValidationStatus status)
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.ValidationStatus = status;

        // Act & Assert
        transaction.CanProceed().Should().BeFalse();
    }

    #endregion

    #region IsBlocked Tests

    [Theory]
    [InlineData(ValidationStatus.Failed)]
    [InlineData(ValidationStatus.RejectedOverride)]
    public void IsBlocked_ReturnsTrue_ForBlockedStatuses(ValidationStatus status)
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.ValidationStatus = status;

        // Act & Assert
        transaction.IsBlocked().Should().BeTrue();
    }

    [Theory]
    [InlineData(ValidationStatus.Pending)]
    [InlineData(ValidationStatus.Passed)]
    [InlineData(ValidationStatus.ApprovedWithOverride)]
    public void IsBlocked_ReturnsFalse_ForNonBlockedStatuses(ValidationStatus status)
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.ValidationStatus = status;

        // Act & Assert
        transaction.IsBlocked().Should().BeFalse();
    }

    #endregion

    #region IsAwaitingOverride Tests

    [Fact]
    public void IsAwaitingOverride_ReturnsTrue_WhenRequiresOverrideAndPending()
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.RequiresOverride = true;
        transaction.OverrideStatus = OverrideStatus.Pending;

        // Act & Assert
        transaction.IsAwaitingOverride().Should().BeTrue();
    }

    [Fact]
    public void IsAwaitingOverride_ReturnsFalse_WhenDoesNotRequireOverride()
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.RequiresOverride = false;
        transaction.OverrideStatus = OverrideStatus.Pending;

        // Act & Assert
        transaction.IsAwaitingOverride().Should().BeFalse();
    }

    [Fact]
    public void IsAwaitingOverride_ReturnsFalse_WhenOverrideApproved()
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.RequiresOverride = true;
        transaction.OverrideStatus = OverrideStatus.Approved;

        // Act & Assert
        transaction.IsAwaitingOverride().Should().BeFalse();
    }

    #endregion

    #region SetValidationResult Tests

    [Fact]
    public void SetValidationResult_SetsPassedStatus_WhenResultIsValid()
    {
        // Arrange
        var transaction = CreateTransaction();
        var result = ValidationResult.Success();
        var validationTime = DateTime.UtcNow;

        // Act
        transaction.SetValidationResult(result, validationTime);

        // Assert
        transaction.ValidationStatus.Should().Be(ValidationStatus.Passed);
        transaction.RequiresOverride.Should().BeFalse();
        transaction.ValidationDate.Should().Be(validationTime);
        transaction.ComplianceErrors.Should().BeEmpty();
    }

    [Fact]
    public void SetValidationResult_SetsFailedStatus_WhenResultIsNotValid()
    {
        // Arrange
        var transaction = CreateTransaction();
        var violations = new[]
        {
            new ValidationViolation
            {
                ErrorCode = "TEST_ERROR",
                Message = "Test violation",
                Severity = ViolationSeverity.Critical,
                CanOverride = true
            }
        };
        var result = ValidationResult.Failure(violations);
        var validationTime = DateTime.UtcNow;

        // Act
        transaction.SetValidationResult(result, validationTime);

        // Assert
        transaction.ValidationStatus.Should().Be(ValidationStatus.Failed);
        transaction.RequiresOverride.Should().BeTrue();
        transaction.OverrideStatus.Should().Be(OverrideStatus.Pending);
        transaction.ValidationDate.Should().Be(validationTime);
        transaction.ComplianceErrors.Should().Contain("Test violation");
    }

    [Fact]
    public void SetValidationResult_DoesNotAllowOverride_WhenViolationsNotOverridable()
    {
        // Arrange
        var transaction = CreateTransaction();
        var violations = new[]
        {
            new ValidationViolation
            {
                ErrorCode = "TEST_ERROR",
                Message = "Non-overridable violation",
                Severity = ViolationSeverity.Critical,
                CanOverride = false
            }
        };
        var result = ValidationResult.Failure(violations);
        var validationTime = DateTime.UtcNow;

        // Act
        transaction.SetValidationResult(result, validationTime);

        // Assert
        transaction.RequiresOverride.Should().BeFalse();
        transaction.OverrideStatus.Should().Be(OverrideStatus.None);
    }

    [Fact]
    public void SetValidationResult_SeparatesWarningsAndErrors()
    {
        // Arrange
        var transaction = CreateTransaction();
        var violations = new[]
        {
            new ValidationViolation
            {
                ErrorCode = "ERROR",
                Message = "Critical error",
                Severity = ViolationSeverity.Critical,
                CanOverride = true
            },
            new ValidationViolation
            {
                ErrorCode = "WARNING",
                Message = "Warning message",
                Severity = ViolationSeverity.Warning,
                CanOverride = true
            }
        };
        var result = ValidationResult.Failure(violations);

        // Act
        transaction.SetValidationResult(result, DateTime.UtcNow);

        // Assert
        transaction.ComplianceErrors.Should().Contain("Critical error");
        transaction.ComplianceWarnings.Should().Contain("Warning message");
    }

    #endregion

    #region ApproveOverride Tests

    [Fact]
    public void ApproveOverride_SetsApprovedStatus()
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.RequiresOverride = true;
        transaction.OverrideStatus = OverrideStatus.Pending;
        transaction.ValidationStatus = ValidationStatus.Failed;
        var approvalTime = DateTime.UtcNow;

        // Act
        transaction.ApproveOverride("user123", "Business justification", approvalTime);

        // Assert
        transaction.OverrideStatus.Should().Be(OverrideStatus.Approved);
        transaction.ValidationStatus.Should().Be(ValidationStatus.ApprovedWithOverride);
        transaction.OverrideDecisionBy.Should().Be("user123");
        transaction.OverrideJustification.Should().Be("Business justification");
        transaction.OverrideDecisionDate.Should().Be(approvalTime);
    }

    [Fact]
    public void ApproveOverride_ThrowsException_WhenOverrideNotRequired()
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.RequiresOverride = false;

        // Act & Assert
        var action = () => transaction.ApproveOverride("user123", "Justification", DateTime.UtcNow);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not require override*");
    }

    #endregion

    #region RejectOverride Tests

    [Fact]
    public void RejectOverride_SetsRejectedStatus()
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.RequiresOverride = true;
        transaction.OverrideStatus = OverrideStatus.Pending;
        transaction.ValidationStatus = ValidationStatus.Failed;
        var rejectionTime = DateTime.UtcNow;

        // Act
        transaction.RejectOverride("user123", "Insufficient justification", rejectionTime);

        // Assert
        transaction.OverrideStatus.Should().Be(OverrideStatus.Rejected);
        transaction.ValidationStatus.Should().Be(ValidationStatus.RejectedOverride);
        transaction.OverrideDecisionBy.Should().Be("user123");
        transaction.OverrideRejectionReason.Should().Be("Insufficient justification");
        transaction.OverrideDecisionDate.Should().Be(rejectionTime);
    }

    [Fact]
    public void RejectOverride_ThrowsException_WhenOverrideNotRequired()
    {
        // Arrange
        var transaction = CreateTransaction();
        transaction.RequiresOverride = false;

        // Act & Assert
        var action = () => transaction.RejectOverride("user123", "Reason", DateTime.UtcNow);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not require override*");
    }

    #endregion

    #region Helper Methods

    private static Transaction CreateTransaction()
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            ExternalId = "ORD-2024-001",
            TransactionType = TransactionType.Order,
            Direction = TransactionDirection.Internal,
            CustomerAccount = "CUST-001",
            CustomerDataAreaId = "nlpd",
            CustomerName = "Test Customer",
            OriginCountry = "NL",
            TransactionDate = DateTime.UtcNow,
            Status = "Confirmed",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
    }

    #endregion
}
