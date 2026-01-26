using FluentAssertions;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// T150: Unit tests for AuditEvent model per data-model.md entity 15.
/// Tests audit event creation, validation, and factory methods.
/// </summary>
public class AuditEventTests
{
    #region Validation Tests

    [Fact]
    public void Validate_ReturnsSuccess_WhenAllRequiredFieldsPresent()
    {
        // Arrange
        var auditEvent = new AuditEvent
        {
            EventId = Guid.NewGuid(),
            EventType = AuditEventType.LicenceCreated,
            EventDate = DateTime.UtcNow,
            PerformedBy = Guid.NewGuid(),
            EntityType = AuditEntityType.Licence,
            EntityId = Guid.NewGuid()
        };

        // Act
        var result = auditEvent.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenPerformedByEmpty()
    {
        // Arrange
        var auditEvent = new AuditEvent
        {
            EventId = Guid.NewGuid(),
            EventType = AuditEventType.LicenceCreated,
            EventDate = DateTime.UtcNow,
            PerformedBy = Guid.Empty,
            EntityType = AuditEntityType.Licence,
            EntityId = Guid.NewGuid()
        };

        // Act
        var result = auditEvent.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("PerformedBy"));
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenEntityIdEmpty()
    {
        // Arrange
        var auditEvent = new AuditEvent
        {
            EventId = Guid.NewGuid(),
            EventType = AuditEventType.LicenceCreated,
            EventDate = DateTime.UtcNow,
            PerformedBy = Guid.NewGuid(),
            EntityType = AuditEntityType.Licence,
            EntityId = Guid.Empty
        };

        // Act
        var result = auditEvent.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("EntityId"));
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenEventDateDefault()
    {
        // Arrange
        var auditEvent = new AuditEvent
        {
            EventId = Guid.NewGuid(),
            EventType = AuditEventType.LicenceCreated,
            EventDate = default,
            PerformedBy = Guid.NewGuid(),
            EntityType = AuditEntityType.Licence,
            EntityId = Guid.NewGuid()
        };

        // Act
        var result = auditEvent.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("EventDate"));
    }

    #endregion

    #region SetDetails/GetDetails Tests

    [Fact]
    public void SetDetails_SerializesObjectToJson()
    {
        // Arrange
        var auditEvent = new AuditEvent();
        var details = new { Field1 = "value1", Field2 = 123 };

        // Act
        auditEvent.SetDetails(details);

        // Assert
        auditEvent.Details.Should().NotBeNullOrEmpty();
        auditEvent.Details.Should().Contain("field1");
        auditEvent.Details.Should().Contain("value1");
    }

    [Fact]
    public void GetDetails_DeserializesJsonToObject()
    {
        // Arrange
        var auditEvent = new AuditEvent();
        var originalDetails = new TestDetails { Field1 = "value1", Field2 = 123 };
        auditEvent.SetDetails(originalDetails);

        // Act
        var retrievedDetails = auditEvent.GetDetails<TestDetails>();

        // Assert
        retrievedDetails.Should().NotBeNull();
        retrievedDetails!.Field1.Should().Be("value1");
        retrievedDetails.Field2.Should().Be(123);
    }

    [Fact]
    public void GetDetails_ReturnsNull_WhenDetailsEmpty()
    {
        // Arrange
        var auditEvent = new AuditEvent { Details = null };

        // Act
        var retrievedDetails = auditEvent.GetDetails<TestDetails>();

        // Assert
        retrievedDetails.Should().BeNull();
    }

    private class TestDetails
    {
        public string? Field1 { get; set; }
        public int Field2 { get; set; }
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void ForCreate_CreatesLicenceCreatedEvent_ForLicenceEntityType()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var performedBy = Guid.NewGuid();

        // Act
        var auditEvent = AuditEvent.ForCreate(AuditEntityType.Licence, entityId, performedBy);

        // Assert
        auditEvent.EventType.Should().Be(AuditEventType.LicenceCreated);
        auditEvent.EntityType.Should().Be(AuditEntityType.Licence);
        auditEvent.EntityId.Should().Be(entityId);
        auditEvent.PerformedBy.Should().Be(performedBy);
        auditEvent.EventId.Should().NotBe(Guid.Empty);
        auditEvent.EventDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ForCreate_CreatesCustomerCreatedEvent_ForCustomerEntityType()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var performedBy = Guid.NewGuid();

        // Act
        var auditEvent = AuditEvent.ForCreate(AuditEntityType.Customer, entityId, performedBy);

        // Assert
        auditEvent.EventType.Should().Be(AuditEventType.CustomerCreated);
        auditEvent.EntityType.Should().Be(AuditEntityType.Customer);
    }

    [Fact]
    public void ForCreate_IncludesDetails_WhenProvided()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var performedBy = Guid.NewGuid();
        var details = new { Name = "Test Licence" };

        // Act
        var auditEvent = AuditEvent.ForCreate(AuditEntityType.Licence, entityId, performedBy, details);

        // Assert
        auditEvent.Details.Should().NotBeNullOrEmpty();
        auditEvent.Details.Should().Contain("Test Licence");
    }

    [Fact]
    public void ForModify_CreatesLicenceModifiedEvent_ForLicenceEntityType()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var performedBy = Guid.NewGuid();

        // Act
        var auditEvent = AuditEvent.ForModify(AuditEntityType.Licence, entityId, performedBy);

        // Assert
        auditEvent.EventType.Should().Be(AuditEventType.LicenceModified);
        auditEvent.EntityType.Should().Be(AuditEntityType.Licence);
        auditEvent.EntityId.Should().Be(entityId);
        auditEvent.PerformedBy.Should().Be(performedBy);
    }

    [Fact]
    public void ForModify_IncludesChanges_WhenProvided()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var performedBy = Guid.NewGuid();
        var changes = new EntityModificationDetails
        {
            Before = new Dictionary<string, object?> { { "Status", "Active" } },
            After = new Dictionary<string, object?> { { "Status", "Suspended" } },
            ChangedFields = new List<string> { "Status" }
        };

        // Act
        var auditEvent = AuditEvent.ForModify(AuditEntityType.Licence, entityId, performedBy, changes);

        // Assert
        auditEvent.Details.Should().NotBeNullOrEmpty();
        var retrieved = auditEvent.GetDetails<EntityModificationDetails>();
        retrieved.Should().NotBeNull();
        retrieved!.ChangedFields.Should().Contain("Status");
    }

    [Fact]
    public void ForOverrideApproval_CreatesOverrideApprovedEvent()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var approvedBy = Guid.NewGuid();
        var justification = "Emergency supply needed";

        // Act
        var auditEvent = AuditEvent.ForOverrideApproval(transactionId, approvedBy, justification);

        // Assert
        auditEvent.EventType.Should().Be(AuditEventType.OverrideApproved);
        auditEvent.EntityType.Should().Be(AuditEntityType.Transaction);
        auditEvent.EntityId.Should().Be(transactionId);
        auditEvent.PerformedBy.Should().Be(approvedBy);
        auditEvent.Details.Should().Contain(justification);
    }

    [Fact]
    public void ForConflictResolution_CreatesConflictResolvedEvent()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var resolvedBy = Guid.NewGuid();
        var details = new ConflictResolutionDetails
        {
            LocalVersion = "v1",
            RemoteVersion = "v2",
            ResolutionMethod = "KeepRemote",
            ConflictingFields = new List<string> { "Status", "ExpiryDate" },
            Notes = "Remote version was more recent"
        };

        // Act
        var auditEvent = AuditEvent.ForConflictResolution(AuditEntityType.Licence, entityId, resolvedBy, details);

        // Assert
        auditEvent.EventType.Should().Be(AuditEventType.ConflictResolved);
        auditEvent.EntityType.Should().Be(AuditEntityType.Licence);
        auditEvent.EntityId.Should().Be(entityId);
        auditEvent.PerformedBy.Should().Be(resolvedBy);

        var retrieved = auditEvent.GetDetails<ConflictResolutionDetails>();
        retrieved.Should().NotBeNull();
        retrieved!.ResolutionMethod.Should().Be("KeepRemote");
        retrieved.ConflictingFields.Should().Contain("Status");
    }

    [Fact]
    public void ForCustomerStatusChange_CreatesCorrectEvent()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var performedBy = Guid.NewGuid();
        var reason = "Compliance violation";

        // Act
        var auditEvent = AuditEvent.ForCustomerStatusChange(
            customerId, performedBy, AuditEventType.CustomerSuspended, reason);

        // Assert
        auditEvent.EventType.Should().Be(AuditEventType.CustomerSuspended);
        auditEvent.EntityType.Should().Be(AuditEntityType.Customer);
        auditEvent.EntityId.Should().Be(customerId);
        auditEvent.Details.Should().Contain(reason);
    }

    #endregion

    #region Enum Tests

    [Theory]
    [InlineData(AuditEventType.LicenceCreated, 10)]
    [InlineData(AuditEventType.LicenceModified, 11)]
    [InlineData(AuditEventType.CustomerApproved, 22)]
    [InlineData(AuditEventType.CustomerSuspended, 23)]
    [InlineData(AuditEventType.TransactionValidated, 30)]
    [InlineData(AuditEventType.OverrideApproved, 31)]
    [InlineData(AuditEventType.InspectionRecorded, 50)]
    [InlineData(AuditEventType.ConflictResolved, 60)]
    public void AuditEventType_HasCorrectNumericValues(AuditEventType eventType, int expectedValue)
    {
        // Assert
        ((int)eventType).Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(AuditEntityType.Customer, 1)]
    [InlineData(AuditEntityType.Licence, 2)]
    [InlineData(AuditEntityType.Transaction, 3)]
    [InlineData(AuditEntityType.GdpSite, 4)]
    [InlineData(AuditEntityType.Inspection, 5)]
    public void AuditEntityType_HasCorrectNumericValues(AuditEntityType entityType, int expectedValue)
    {
        // Assert
        ((int)entityType).Should().Be(expectedValue);
    }

    #endregion
}
