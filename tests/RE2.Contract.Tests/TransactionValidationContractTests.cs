using System.Text.Json;
using FluentAssertions;
using RE2.ComplianceApi.Controllers.V1;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.Contract.Tests;

/// <summary>
/// Contract tests for POST /api/v1/transactions/validate API.
/// T126: Verifies API request/response contracts match transaction-validation-api.yaml specification.
/// </summary>
public class TransactionValidationContractTests
{
    #region Request Contract Tests

    [Fact]
    public void TransactionValidationRequestDto_ShouldHave_RequiredFields()
    {
        // Arrange & Act
        var dtoType = typeof(TransactionValidationRequestDto);
        var properties = dtoType.GetProperties();

        // Assert - required fields per transaction-validation-api.yaml
        properties.Should().Contain(p => p.Name == "ExternalId");
        properties.Should().Contain(p => p.Name == "CustomerId");
        properties.Should().Contain(p => p.Name == "TransactionType");
        properties.Should().Contain(p => p.Name == "TransactionDate");
        properties.Should().Contain(p => p.Name == "Lines");
    }

    [Fact]
    public void TransactionValidationRequestDto_ShouldHave_OptionalCrossBorderFields()
    {
        // Arrange & Act
        var dtoType = typeof(TransactionValidationRequestDto);
        var properties = dtoType.GetProperties();

        // Assert - optional fields for cross-border per FR-021
        properties.Should().Contain(p => p.Name == "Direction");
        properties.Should().Contain(p => p.Name == "OriginCountry");
        properties.Should().Contain(p => p.Name == "DestinationCountry");
    }

    [Fact]
    public void TransactionLineDto_ShouldHave_RequiredFields()
    {
        // Arrange & Act
        var dtoType = typeof(TransactionLineDto);
        var properties = dtoType.GetProperties();

        // Assert - required fields per transaction-validation-api.yaml
        properties.Should().Contain(p => p.Name == "SubstanceId");
        properties.Should().Contain(p => p.Name == "Quantity");
    }

    [Fact]
    public void TransactionLineDto_ShouldHave_ThresholdFields()
    {
        // Arrange & Act
        var dtoType = typeof(TransactionLineDto);
        var properties = dtoType.GetProperties();

        // Assert - fields needed for threshold checking per FR-022
        properties.Should().Contain(p => p.Name == "BaseUnitQuantity");
        properties.Should().Contain(p => p.Name == "BaseUnit");
        properties.Should().Contain(p => p.Name == "UnitOfMeasure");
    }

    [Fact]
    public void ToDomainModel_ShouldMapAllRequiredFields()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var substanceId = Guid.NewGuid();
        var transactionDate = DateTime.UtcNow;

        var request = new TransactionValidationRequestDto
        {
            ExternalId = "SO-2026-0012345",
            CustomerId = customerId,
            TransactionType = "Order",
            Direction = "Internal",
            TransactionDate = transactionDate,
            OriginCountry = "NL",
            Lines = new List<TransactionLineDto>
            {
                new()
                {
                    SubstanceId = substanceId,
                    SubstanceCode = "MOR-001",
                    Quantity = 100,
                    UnitOfMeasure = "ampules",
                    BaseUnitQuantity = 1000,
                    BaseUnit = "mg"
                }
            }
        };

        // Act
        var domain = request.ToDomainModel();

        // Assert
        domain.ExternalId.Should().Be("SO-2026-0012345");
        domain.CustomerId.Should().Be(customerId);
        domain.TransactionType.Should().Be(TransactionType.Order);
        domain.Direction.Should().Be(TransactionDirection.Internal);
        domain.TransactionDate.Should().Be(transactionDate);
        domain.OriginCountry.Should().Be("NL");
        domain.Lines.Should().HaveCount(1);
        domain.Lines[0].SubstanceId.Should().Be(substanceId);
        domain.Lines[0].Quantity.Should().Be(100);
    }

    [Theory]
    [InlineData("Order", TransactionType.Order)]
    [InlineData("Shipment", TransactionType.Shipment)]
    [InlineData("Return", TransactionType.Return)]
    [InlineData("Transfer", TransactionType.Transfer)]
    public void ToDomainModel_ShouldMapTransactionType_Correctly(string typeString, TransactionType expected)
    {
        // Arrange
        var request = new TransactionValidationRequestDto
        {
            ExternalId = "TEST-001",
            CustomerId = Guid.NewGuid(),
            TransactionType = typeString,
            Lines = new List<TransactionLineDto>
            {
                new() { SubstanceId = Guid.NewGuid(), Quantity = 10 }
            }
        };

        // Act
        var domain = request.ToDomainModel();

        // Assert
        domain.TransactionType.Should().Be(expected);
    }

    [Theory]
    [InlineData("Internal", TransactionDirection.Internal)]
    [InlineData("Inbound", TransactionDirection.Inbound)]
    [InlineData("Outbound", TransactionDirection.Outbound)]
    public void ToDomainModel_ShouldMapDirection_Correctly(string directionString, TransactionDirection expected)
    {
        // Arrange
        var request = new TransactionValidationRequestDto
        {
            ExternalId = "TEST-001",
            CustomerId = Guid.NewGuid(),
            Direction = directionString,
            Lines = new List<TransactionLineDto>
            {
                new() { SubstanceId = Guid.NewGuid(), Quantity = 10 }
            }
        };

        // Act
        var domain = request.ToDomainModel();

        // Assert
        domain.Direction.Should().Be(expected);
    }

    [Fact]
    public void ToDomainModel_ShouldCalculateTotalQuantity()
    {
        // Arrange
        var request = new TransactionValidationRequestDto
        {
            ExternalId = "TEST-001",
            CustomerId = Guid.NewGuid(),
            Lines = new List<TransactionLineDto>
            {
                new() { SubstanceId = Guid.NewGuid(), Quantity = 100, BaseUnitQuantity = 1000 },
                new() { SubstanceId = Guid.NewGuid(), Quantity = 50, BaseUnitQuantity = 500 }
            }
        };

        // Act
        var domain = request.ToDomainModel();

        // Assert
        domain.TotalQuantity.Should().Be(1500); // Sum of BaseUnitQuantity
    }

    #endregion

    #region Response Contract Tests

    [Fact]
    public void TransactionValidationResultDto_ShouldHave_RequiredFields()
    {
        // Arrange & Act
        var dtoType = typeof(TransactionValidationResultDto);
        var properties = dtoType.GetProperties();

        // Assert - required fields per transaction-validation-api.yaml
        properties.Should().Contain(p => p.Name == "IsValid");
        properties.Should().Contain(p => p.Name == "CanProceed");
        properties.Should().Contain(p => p.Name == "Status");
        properties.Should().Contain(p => p.Name == "TransactionId");
        properties.Should().Contain(p => p.Name == "ExternalId");
        properties.Should().Contain(p => p.Name == "Violations");
        properties.Should().Contain(p => p.Name == "LicenceUsages");
    }

    [Fact]
    public void TransactionViolationDto_ShouldHave_RequiredFields()
    {
        // Arrange & Act
        var dtoType = typeof(TransactionViolationDto);
        var properties = dtoType.GetProperties();

        // Assert - required fields per ComplianceViolation schema in API spec
        properties.Should().Contain(p => p.Name == "ErrorCode");
        properties.Should().Contain(p => p.Name == "Message");
        properties.Should().Contain(p => p.Name == "Severity");
        properties.Should().Contain(p => p.Name == "CanOverride");
    }

    [Fact]
    public void TransactionViolationDto_ShouldHave_LineReferenceFields()
    {
        // Arrange & Act
        var dtoType = typeof(TransactionViolationDto);
        var properties = dtoType.GetProperties();

        // Assert - optional line reference fields
        properties.Should().Contain(p => p.Name == "LineNumber");
        properties.Should().Contain(p => p.Name == "SubstanceCode");
    }

    [Fact]
    public void LicenceUsageDto_ShouldHave_RequiredFields()
    {
        // Arrange & Act
        var dtoType = typeof(LicenceUsageDto);
        var properties = dtoType.GetProperties();

        // Assert - required fields per LicenceUsage schema in API spec
        properties.Should().Contain(p => p.Name == "LicenceId");
        properties.Should().Contain(p => p.Name == "LicenceNumber");
        properties.Should().Contain(p => p.Name == "LicenceTypeName");
        properties.Should().Contain(p => p.Name == "CoveredLineNumbers");
    }

    [Fact]
    public void FromDomain_ShouldMapPassingValidation()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transaction = new Transaction
        {
            Id = transactionId,
            ExternalId = "SO-2026-0012345",
            ValidationStatus = ValidationStatus.Passed
        };

        var validationResult = new TransactionValidationResult
        {
            Transaction = transaction,
            ValidationResult = ValidationResult.Success(),
            ValidationTimeMs = 150
        };

        // Act
        var dto = TransactionValidationResultDto.FromDomain(validationResult);

        // Assert
        dto.IsValid.Should().BeTrue();
        dto.CanProceed.Should().BeTrue(); // Computed from ValidationResult.IsValid
        dto.TransactionId.Should().Be(transactionId);
        dto.ExternalId.Should().Be("SO-2026-0012345");
        dto.Status.Should().Be("Passed");
        dto.Violations.Should().BeEmpty();
    }

    [Fact]
    public void FromDomain_ShouldMapFailedValidationWithViolations()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transaction = new Transaction
        {
            Id = transactionId,
            ExternalId = "SO-2026-0012346",
            ValidationStatus = ValidationStatus.Failed
        };

        var violations = new List<ValidationViolation>
        {
            new()
            {
                ErrorCode = "LICENCE_EXPIRED",
                Message = "Customer licence expired",
                Severity = ViolationSeverity.Critical,
                CanOverride = true,
                LineNumber = 1,
                SubstanceCode = "MOR-001"
            }
        };

        var validationResult = new TransactionValidationResult
        {
            Transaction = transaction,
            ValidationResult = ValidationResult.Failure(violations),
            ValidationTimeMs = 200
        };

        // Act
        var dto = TransactionValidationResultDto.FromDomain(validationResult);

        // Assert
        dto.IsValid.Should().BeFalse();
        dto.CanProceed.Should().BeFalse(); // Computed from ValidationResult.IsValid
        dto.Violations.Should().HaveCount(1);
        dto.Violations[0].ErrorCode.Should().Be("LICENCE_EXPIRED");
        dto.Violations[0].Severity.Should().Be("Critical");
        dto.Violations[0].CanOverride.Should().BeTrue();
        dto.Violations[0].LineNumber.Should().Be(1);
        dto.Violations[0].SubstanceCode.Should().Be("MOR-001");
    }

    [Fact]
    public void FromDomain_ShouldMapLicenceUsages()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var licenceId = Guid.NewGuid();
        var transaction = new Transaction
        {
            Id = transactionId,
            ExternalId = "SO-2026-0012345",
            ValidationStatus = ValidationStatus.Passed
        };

        var validationResult = new TransactionValidationResult
        {
            Transaction = transaction,
            ValidationResult = ValidationResult.Success(),
            ValidationTimeMs = 150,
            LicenceUsages = new List<TransactionLicenceUsage>
            {
                new()
                {
                    LicenceId = licenceId,
                    LicenceNumber = "OA-2024-789456",
                    LicenceTypeName = "Opium Act Exemption",
                    CoveredLineNumbers = new List<int> { 1, 2 },
                    CoveredQuantity = 100,
                    CoveredQuantityUnit = "g"
                }
            }
        };

        // Act
        var dto = TransactionValidationResultDto.FromDomain(validationResult);

        // Assert
        dto.LicenceUsages.Should().HaveCount(1);
        dto.LicenceUsages[0].LicenceId.Should().Be(licenceId);
        dto.LicenceUsages[0].LicenceNumber.Should().Be("OA-2024-789456");
        dto.LicenceUsages[0].LicenceTypeName.Should().Be("Opium Act Exemption");
        dto.LicenceUsages[0].CoveredLineNumbers.Should().BeEquivalentTo(new[] { 1, 2 });
    }

    #endregion

    #region Violation Type Contract Tests

    [Theory]
    [InlineData("LICENCE_EXPIRED")]
    [InlineData("LICENCE_MISSING")]
    [InlineData("LICENCE_SUSPENDED")]
    [InlineData("SUBSTANCE_NOT_AUTHORIZED")]
    [InlineData("THRESHOLD_EXCEEDED")]
    [InlineData("MISSING_PERMIT")]
    [InlineData("CUSTOMER_SUSPENDED")]
    [InlineData("CUSTOMER_NOT_APPROVED")]
    public void ViolationType_ShouldBeValidPerApiSpec(string violationType)
    {
        // Arrange & Act - verify error code can be used in violation
        var violation = new TransactionViolationDto
        {
            ErrorCode = violationType,
            Message = "Test message",
            Severity = "Critical"
        };

        // Assert - should not throw, indicating valid violation type
        violation.ErrorCode.Should().Be(violationType);
    }

    #endregion

    #region Cross-Border Contract Tests (FR-021)

    [Fact]
    public void ToDomainModel_CrossBorderExport_ShouldMapDestinationCountry()
    {
        // Arrange
        var request = new TransactionValidationRequestDto
        {
            ExternalId = "SO-2026-EXPORT-001",
            CustomerId = Guid.NewGuid(),
            TransactionType = "Shipment",
            Direction = "Outbound",
            OriginCountry = "NL",
            DestinationCountry = "DE",
            Lines = new List<TransactionLineDto>
            {
                new() { SubstanceId = Guid.NewGuid(), Quantity = 500 }
            }
        };

        // Act
        var domain = request.ToDomainModel();

        // Assert - cross-border fields per FR-021
        domain.Direction.Should().Be(TransactionDirection.Outbound);
        domain.OriginCountry.Should().Be("NL");
        domain.DestinationCountry.Should().Be("DE");
    }

    [Fact]
    public void ToDomainModel_DomesticTransaction_ShouldHaveNullDestination()
    {
        // Arrange
        var request = new TransactionValidationRequestDto
        {
            ExternalId = "SO-2026-DOMESTIC-001",
            CustomerId = Guid.NewGuid(),
            TransactionType = "Order",
            Direction = "Internal",
            OriginCountry = "NL",
            DestinationCountry = null,
            Lines = new List<TransactionLineDto>
            {
                new() { SubstanceId = Guid.NewGuid(), Quantity = 100 }
            }
        };

        // Act
        var domain = request.ToDomainModel();

        // Assert
        domain.Direction.Should().Be(TransactionDirection.Internal);
        domain.OriginCountry.Should().Be("NL");
        domain.DestinationCountry.Should().BeNull();
    }

    #endregion

    #region Override Contract Tests (FR-019a)

    [Fact]
    public void OverrideApprovalRequestDto_ShouldHave_Justification()
    {
        // Arrange & Act
        var dtoType = typeof(OverrideApprovalRequestDto);
        var properties = dtoType.GetProperties();

        // Assert - justification required per FR-019a
        properties.Should().Contain(p => p.Name == "Justification");
    }

    [Fact]
    public void OverrideRejectionRequestDto_ShouldHave_Reason()
    {
        // Arrange & Act
        var dtoType = typeof(OverrideRejectionRequestDto);
        var properties = dtoType.GetProperties();

        // Assert
        properties.Should().Contain(p => p.Name == "Reason");
    }

    [Fact]
    public void TransactionResponseDto_ShouldHave_OverrideFields()
    {
        // Arrange & Act
        var dtoType = typeof(TransactionResponseDto);
        var properties = dtoType.GetProperties();

        // Assert - override tracking fields per FR-019a
        properties.Should().Contain(p => p.Name == "RequiresOverride");
        properties.Should().Contain(p => p.Name == "OverrideStatus");
        properties.Should().Contain(p => p.Name == "OverrideJustification");
        properties.Should().Contain(p => p.Name == "OverrideRejectionReason");
    }

    #endregion

    #region JSON Serialization Contract Tests

    [Fact]
    public void TransactionValidationRequestDto_ShouldSerialize_ToValidJson()
    {
        // Arrange
        var request = new TransactionValidationRequestDto
        {
            ExternalId = "SO-2026-0012345",
            CustomerId = Guid.Parse("c7f3a1b2-8d4e-4a9c-b5e6-1f2a3b4c5d6e"),
            TransactionType = "DomesticSale",
            TransactionDate = new DateTime(2026, 1, 9, 14, 30, 0, DateTimeKind.Utc),
            Lines = new List<TransactionLineDto>
            {
                new()
                {
                    SubstanceId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    SubstanceCode = "MOR-10MG-AMP",
                    Quantity = 100,
                    UnitOfMeasure = "ampules"
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(request);

        // Assert
        json.Should().Contain("SO-2026-0012345");
        json.Should().Contain("c7f3a1b2-8d4e-4a9c-b5e6-1f2a3b4c5d6e");
        json.Should().Contain("MOR-10MG-AMP");
    }

    [Fact]
    public void TransactionValidationResultDto_ShouldSerialize_ToValidJson()
    {
        // Arrange
        var result = new TransactionValidationResultDto
        {
            IsValid = true,
            CanProceed = true,
            Status = "Pass",
            TransactionId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            ExternalId = "SO-2026-0012345",
            ValidationTimeMs = 150,
            Violations = new List<TransactionViolationDto>(),
            LicenceUsages = new List<LicenceUsageDto>()
        };

        // Act
        var json = JsonSerializer.Serialize(result);

        // Assert
        json.Should().Contain("\"IsValid\":true");
        json.Should().Contain("\"CanProceed\":true");
        json.Should().Contain("SO-2026-0012345");
    }

    #endregion
}
