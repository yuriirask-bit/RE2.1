using FluentAssertions;
using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for Alert domain model.
/// T107 verification: Tests Alert per data-model.md entity 11.
/// </summary>
public class AlertTests
{
    [Fact]
    public void Alert_ShouldHaveCorrectDefaultValues()
    {
        // Arrange & Act
        var alert = new Alert();

        // Assert
        alert.AlertId.Should().Be(Guid.Empty);
        alert.TargetEntityId.Should().Be(Guid.Empty);
        alert.Message.Should().BeEmpty();
        alert.AcknowledgedDate.Should().BeNull();
        alert.AcknowledgedBy.Should().BeNull();
        alert.AcknowledgerName.Should().BeNull();
        alert.Details.Should().BeNull();
        alert.RelatedEntityId.Should().BeNull();
        alert.DueDate.Should().BeNull();
    }

    [Fact]
    public void Validate_WithValidAlert_ShouldPass()
    {
        // Arrange
        var alert = new Alert
        {
            AlertId = Guid.NewGuid(),
            AlertType = AlertType.LicenceExpiring,
            Severity = AlertSeverity.Warning,
            TargetEntityType = TargetEntityType.Licence,
            TargetEntityId = Guid.NewGuid(),
            GeneratedDate = DateTime.UtcNow,
            Message = "Licence WHL-2023-001 expires in 60 days"
        };

        // Act
        var result = alert.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyTargetEntityId_ShouldFail()
    {
        // Arrange
        var alert = new Alert
        {
            AlertId = Guid.NewGuid(),
            AlertType = AlertType.LicenceExpired,
            Severity = AlertSeverity.Critical,
            TargetEntityType = TargetEntityType.Licence,
            TargetEntityId = Guid.Empty,
            GeneratedDate = DateTime.UtcNow,
            Message = "Test message"
        };

        // Act
        var result = alert.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("TargetEntityId is required"));
    }

    [Fact]
    public void Validate_WithEmptyMessage_ShouldFail()
    {
        // Arrange
        var alert = new Alert
        {
            AlertId = Guid.NewGuid(),
            AlertType = AlertType.LicenceExpiring,
            Severity = AlertSeverity.Warning,
            TargetEntityType = TargetEntityType.Licence,
            TargetEntityId = Guid.NewGuid(),
            GeneratedDate = DateTime.UtcNow,
            Message = ""
        };

        // Act
        var result = alert.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("Message is required"));
    }

    [Fact]
    public void Validate_WithAcknowledgedDateButNoAcknowledgedBy_ShouldFail()
    {
        // Arrange
        var alert = new Alert
        {
            AlertId = Guid.NewGuid(),
            AlertType = AlertType.LicenceExpiring,
            Severity = AlertSeverity.Warning,
            TargetEntityType = TargetEntityType.Licence,
            TargetEntityId = Guid.NewGuid(),
            GeneratedDate = DateTime.UtcNow,
            Message = "Test message",
            AcknowledgedDate = DateTime.UtcNow,
            AcknowledgedBy = null
        };

        // Act
        var result = alert.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("AcknowledgedBy is required"));
    }

    [Fact]
    public void IsAcknowledged_WhenAcknowledged_ShouldReturnTrue()
    {
        // Arrange
        var alert = new Alert
        {
            AcknowledgedDate = DateTime.UtcNow,
            AcknowledgedBy = Guid.NewGuid()
        };

        // Act & Assert
        alert.IsAcknowledged().Should().BeTrue();
    }

    [Fact]
    public void IsAcknowledged_WhenNotAcknowledged_ShouldReturnFalse()
    {
        // Arrange
        var alert = new Alert
        {
            AcknowledgedDate = null,
            AcknowledgedBy = null
        };

        // Act & Assert
        alert.IsAcknowledged().Should().BeFalse();
    }

    [Fact]
    public void Acknowledge_ShouldSetAcknowledgementFields()
    {
        // Arrange
        var alert = new Alert
        {
            AlertId = Guid.NewGuid(),
            Message = "Test alert"
        };
        var userId = Guid.NewGuid();

        // Act
        alert.Acknowledge(userId, "Jan de Vries");

        // Assert
        alert.IsAcknowledged().Should().BeTrue();
        alert.AcknowledgedBy.Should().Be(userId);
        alert.AcknowledgerName.Should().Be("Jan de Vries");
        alert.AcknowledgedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(AlertSeverity.Critical, true)]
    [InlineData(AlertSeverity.Warning, false)]
    [InlineData(AlertSeverity.Info, false)]
    public void IsCritical_ShouldReturnCorrectValue(AlertSeverity severity, bool expected)
    {
        // Arrange
        var alert = new Alert { Severity = severity };

        // Act & Assert
        alert.IsCritical().Should().Be(expected);
    }

    [Fact]
    public void IsOverdue_WithPastDueDate_ShouldReturnTrue()
    {
        // Arrange
        var alert = new Alert
        {
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5))
        };

        // Act & Assert
        alert.IsOverdue().Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_WithFutureDueDate_ShouldReturnFalse()
    {
        // Arrange
        var alert = new Alert
        {
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))
        };

        // Act & Assert
        alert.IsOverdue().Should().BeFalse();
    }

    [Fact]
    public void IsOverdue_WithNoDueDate_ShouldReturnFalse()
    {
        // Arrange
        var alert = new Alert { DueDate = null };

        // Act & Assert
        alert.IsOverdue().Should().BeFalse();
    }

    [Fact]
    public void GetAgeDays_ShouldReturnCorrectAge()
    {
        // Arrange
        var alert = new Alert
        {
            GeneratedDate = DateTime.UtcNow.AddDays(-15)
        };

        // Act
        var ageDays = alert.GetAgeDays();

        // Assert
        ageDays.Should().BeGreaterOrEqualTo(15);
        ageDays.Should().BeLessThan(17); // Account for test execution time
    }

    [Theory]
    [InlineData(30, AlertSeverity.Critical)]
    [InlineData(45, AlertSeverity.Warning)]
    [InlineData(60, AlertSeverity.Warning)]
    [InlineData(90, AlertSeverity.Info)]
    public void CreateLicenceExpiryAlert_ShouldSetCorrectSeverity(int daysUntilExpiry, AlertSeverity expectedSeverity)
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var expiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysUntilExpiry));

        // Act
        var alert = Alert.CreateLicenceExpiryAlert(licenceId, "WHL-2023-001", daysUntilExpiry, expiryDate);

        // Assert
        alert.Severity.Should().Be(expectedSeverity);
        alert.AlertType.Should().Be(AlertType.LicenceExpiring);
        alert.TargetEntityType.Should().Be(TargetEntityType.Licence);
        alert.TargetEntityId.Should().Be(licenceId);
        alert.DueDate.Should().Be(expiryDate);
    }

    [Fact]
    public void CreateLicenceExpiryAlert_WithExpiredLicence_ShouldCreateExpiredAlert()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var expiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5));

        // Act
        var alert = Alert.CreateLicenceExpiryAlert(licenceId, "WHL-2023-001", -5, expiryDate);

        // Assert
        alert.AlertType.Should().Be(AlertType.LicenceExpired);
        alert.Severity.Should().Be(AlertSeverity.Critical);
        alert.Message.Should().Contain("has expired");
    }

    [Fact]
    public void CreateReVerificationAlert_ShouldCreateCorrectAlert()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45));

        // Act
        var alert = Alert.CreateReVerificationAlert(customerId, "Apotheek Van der Berg", dueDate);

        // Assert
        alert.AlertType.Should().Be(AlertType.ReVerificationDue);
        alert.Severity.Should().Be(AlertSeverity.Warning);
        alert.TargetEntityType.Should().Be(TargetEntityType.Customer);
        alert.TargetEntityId.Should().Be(customerId);
        alert.DueDate.Should().Be(dueDate);
        alert.Message.Should().Contain("Apotheek Van der Berg");
        alert.Message.Should().Contain("re-verification");
    }

    [Theory]
    [InlineData(AlertType.LicenceExpiring)]
    [InlineData(AlertType.LicenceExpired)]
    [InlineData(AlertType.MissingDocumentation)]
    [InlineData(AlertType.ThresholdExceeded)]
    [InlineData(AlertType.ReVerificationDue)]
    [InlineData(AlertType.GdpCertificateExpiring)]
    [InlineData(AlertType.GdpCertificateExpired)]
    [InlineData(AlertType.VerificationOverdue)]
    [InlineData(AlertType.ReclassificationImpact)]
    public void AlertType_ShouldSupportAllValues(AlertType alertType)
    {
        // Arrange
        var alert = new Alert
        {
            AlertId = Guid.NewGuid(),
            AlertType = alertType,
            Severity = AlertSeverity.Warning,
            TargetEntityType = TargetEntityType.Licence,
            TargetEntityId = Guid.NewGuid(),
            GeneratedDate = DateTime.UtcNow,
            Message = "Test message"
        };

        // Act & Assert
        alert.AlertType.Should().Be(alertType);
        alert.Validate().IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(TargetEntityType.Customer)]
    [InlineData(TargetEntityType.Licence)]
    [InlineData(TargetEntityType.Threshold)]
    [InlineData(TargetEntityType.GdpSite)]
    [InlineData(TargetEntityType.GdpCredential)]
    [InlineData(TargetEntityType.Transaction)]
    public void TargetEntityType_ShouldSupportAllValues(TargetEntityType entityType)
    {
        // Arrange
        var alert = new Alert
        {
            AlertId = Guid.NewGuid(),
            AlertType = AlertType.LicenceExpiring,
            Severity = AlertSeverity.Warning,
            TargetEntityType = entityType,
            TargetEntityId = Guid.NewGuid(),
            GeneratedDate = DateTime.UtcNow,
            Message = "Test message"
        };

        // Act & Assert
        alert.TargetEntityType.Should().Be(entityType);
        alert.Validate().IsValid.Should().BeTrue();
    }
}
