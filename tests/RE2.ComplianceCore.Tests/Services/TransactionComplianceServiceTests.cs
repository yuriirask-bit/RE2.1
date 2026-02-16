using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.ComplianceCore.Services.TransactionValidation;
using RE2.Shared.Constants;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceCore.Tests.Services;

/// <summary>
/// Unit tests for TransactionComplianceService.
/// T149a: Service tests per FR-018 through FR-024.
/// </summary>
public class TransactionComplianceServiceTests
{
    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly Mock<IThresholdRepository> _thresholdRepoMock;
    private readonly Mock<ILicenceRepository> _licenceRepoMock;
    private readonly Mock<ICustomerRepository> _customerRepoMock;
    private readonly Mock<IControlledSubstanceRepository> _substanceRepoMock;
    private readonly Mock<ILogger<TransactionComplianceService>> _loggerMock;
    private readonly TransactionComplianceService _service;

    public TransactionComplianceServiceTests()
    {
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _thresholdRepoMock = new Mock<IThresholdRepository>();
        _licenceRepoMock = new Mock<ILicenceRepository>();
        _customerRepoMock = new Mock<ICustomerRepository>();
        _substanceRepoMock = new Mock<IControlledSubstanceRepository>();
        _loggerMock = new Mock<ILogger<TransactionComplianceService>>();

        _service = new TransactionComplianceService(
            _transactionRepoMock.Object,
            _thresholdRepoMock.Object,
            _licenceRepoMock.Object,
            _customerRepoMock.Object,
            _substanceRepoMock.Object,
            _loggerMock.Object);
    }

    #region ValidateTransactionAsync Tests

    [Fact]
    public async Task ValidateTransactionAsync_PassesValidation_WhenAllRequirementsMet()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var licenceId = Guid.NewGuid();

        var customer = CreateApprovedCustomer();
        var substance = CreateControlledSubstance(substanceId);
        var licence = CreateValidLicence(licenceId, customer.ComplianceExtensionId);
        var transaction = CreateTransaction(customer.CustomerAccount, customer.DataAreaId, substanceId);

        SetupMocks(customer, substance, licence);

        // Act
        var result = await _service.ValidateTransactionAsync(transaction);

        // Assert
        result.ValidationResult.IsValid.Should().BeTrue();
        result.CanProceed.Should().BeTrue();
        result.Transaction.ValidationStatus.Should().Be(ValidationStatus.Passed);
    }

    [Fact]
    public async Task ValidateTransactionAsync_FailsValidation_WhenCustomerNotFound()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var transaction = CreateTransaction("CUST-NOTFOUND", "nlpd", substanceId);

        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-NOTFOUND", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _service.ValidateTransactionAsync(transaction);

        // Assert
        result.ValidationResult.IsValid.Should().BeFalse();
        result.ValidationResult.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.CUSTOMER_NOT_FOUND);
    }

    [Fact]
    public async Task ValidateTransactionAsync_FailsValidation_WhenCustomerSuspended()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var customer = CreateApprovedCustomer();
        customer.IsSuspended = true;
        customer.SuspensionReason = "Compliance violation";

        var substance = CreateControlledSubstance(substanceId);
        var transaction = CreateTransaction(customer.CustomerAccount, customer.DataAreaId, substanceId);

        SetupMocks(customer, substance, null);

        // Act
        var result = await _service.ValidateTransactionAsync(transaction);

        // Assert
        result.ValidationResult.IsValid.Should().BeFalse();
        result.ValidationResult.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.CUSTOMER_SUSPENDED);
        result.Transaction.RequiresOverride.Should().BeFalse(); // Cannot override suspended
    }

    [Fact]
    public async Task ValidateTransactionAsync_FailsValidation_WhenCustomerNotApproved()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var customer = CreateApprovedCustomer();
        customer.ApprovalStatus = ApprovalStatus.Pending;

        var substance = CreateControlledSubstance(substanceId);
        var transaction = CreateTransaction(customer.CustomerAccount, customer.DataAreaId, substanceId);

        SetupMocks(customer, substance, null);

        // Act
        var result = await _service.ValidateTransactionAsync(transaction);

        // Assert
        result.ValidationResult.IsValid.Should().BeFalse();
        result.ValidationResult.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.CUSTOMER_NOT_APPROVED);
    }

    [Fact]
    public async Task ValidateTransactionAsync_FailsValidation_WhenNoLicenceCoverage()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var customer = CreateApprovedCustomer();
        var substance = CreateControlledSubstance(substanceId);
        var transaction = CreateTransaction(customer.CustomerAccount, customer.DataAreaId, substanceId);

        SetupMocks(customer, substance, null); // No licence

        // Act
        var result = await _service.ValidateTransactionAsync(transaction);

        // Assert
        result.ValidationResult.IsValid.Should().BeFalse();
        result.ValidationResult.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.LICENCE_MISSING);
        result.Transaction.RequiresOverride.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTransactionAsync_FailsValidation_WhenLicenceExpired()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var licenceId = Guid.NewGuid();

        var customer = CreateApprovedCustomer();
        var substance = CreateControlledSubstance(substanceId);
        var licence = CreateValidLicence(licenceId, customer.ComplianceExtensionId);
        licence.Status = "Expired";
        licence.ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));

        var transaction = CreateTransaction(customer.CustomerAccount, customer.DataAreaId, substanceId);

        SetupMocks(customer, substance, licence);

        // Act
        var result = await _service.ValidateTransactionAsync(transaction);

        // Assert
        result.ValidationResult.IsValid.Should().BeFalse();
        result.ValidationResult.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.LICENCE_EXPIRED);
    }

    [Fact]
    public async Task ValidateTransactionAsync_FailsValidation_WhenLicenceSuspended()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var licenceId = Guid.NewGuid();

        var customer = CreateApprovedCustomer();
        var substance = CreateControlledSubstance(substanceId);
        var licence = CreateValidLicence(licenceId, customer.ComplianceExtensionId);
        licence.Status = "Suspended";

        var transaction = CreateTransaction(customer.CustomerAccount, customer.DataAreaId, substanceId);

        SetupMocks(customer, substance, licence);

        // Act
        var result = await _service.ValidateTransactionAsync(transaction);

        // Assert
        result.ValidationResult.IsValid.Should().BeFalse();
        result.ValidationResult.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.LICENCE_SUSPENDED);
        result.Transaction.RequiresOverride.Should().BeFalse(); // Cannot override suspended licence
    }

    [Fact]
    public async Task ValidateTransactionAsync_FailsValidation_WhenSubstanceNotFound()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var customer = CreateApprovedCustomer();
        var transaction = CreateTransaction(customer.CustomerAccount, customer.DataAreaId, substanceId);

        _customerRepoMock.Setup(r => r.GetByAccountAsync(customer.CustomerAccount, customer.DataAreaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _substanceRepoMock.Setup(r => r.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);
        _licenceRepoMock.Setup(r => r.GetByHolderAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Licence>());
        _thresholdRepoMock.Setup(r => r.GetApplicableThresholdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<Guid>(), It.IsAny<BusinessCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Threshold>());
        _thresholdRepoMock.Setup(r => r.GetByTypeAsync(It.IsAny<ThresholdType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Threshold>());

        // Act
        var result = await _service.ValidateTransactionAsync(transaction);

        // Assert
        result.ValidationResult.IsValid.Should().BeFalse();
        result.ValidationResult.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.SUBSTANCE_NOT_FOUND);
    }

    #endregion

    #region ApproveOverrideAsync Tests

    [Fact]
    public async Task ApproveOverrideAsync_Succeeds_WhenTransactionPendingOverride()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transaction = CreateFailedTransaction(transactionId);
        transaction.RequiresOverride = true;
        transaction.OverrideStatus = OverrideStatus.Pending;

        _transactionRepoMock.Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _service.ApproveOverrideAsync(transactionId, "approver@test.com", "Business justification");

        // Assert
        result.IsValid.Should().BeTrue();
        _transactionRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Transaction>(t => t.OverrideStatus == OverrideStatus.Approved),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveOverrideAsync_Fails_WhenTransactionNotFound()
    {
        // Arrange
        var transactionId = Guid.NewGuid();

        _transactionRepoMock.Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        // Act
        var result = await _service.ApproveOverrideAsync(transactionId, "approver@test.com", "Justification");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.TRANSACTION_NOT_FOUND);
    }

    [Fact]
    public async Task ApproveOverrideAsync_Fails_WhenTransactionDoesNotRequireOverride()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transaction = CreateFailedTransaction(transactionId);
        transaction.RequiresOverride = false;

        _transactionRepoMock.Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _service.ApproveOverrideAsync(transactionId, "approver@test.com", "Justification");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("does not require override"));
    }

    [Fact]
    public async Task ApproveOverrideAsync_Fails_WhenOverrideAlreadyProcessed()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transaction = CreateFailedTransaction(transactionId);
        transaction.RequiresOverride = true;
        transaction.OverrideStatus = OverrideStatus.Approved;

        _transactionRepoMock.Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _service.ApproveOverrideAsync(transactionId, "approver@test.com", "Justification");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("not pending"));
    }

    [Fact]
    public async Task ApproveOverrideAsync_Fails_WhenJustificationEmpty()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transaction = CreateFailedTransaction(transactionId);
        transaction.RequiresOverride = true;
        transaction.OverrideStatus = OverrideStatus.Pending;

        _transactionRepoMock.Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _service.ApproveOverrideAsync(transactionId, "approver@test.com", "");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("justification"));
    }

    #endregion

    #region RejectOverrideAsync Tests

    [Fact]
    public async Task RejectOverrideAsync_Succeeds_WhenTransactionPendingOverride()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transaction = CreateFailedTransaction(transactionId);
        transaction.RequiresOverride = true;
        transaction.OverrideStatus = OverrideStatus.Pending;

        _transactionRepoMock.Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _service.RejectOverrideAsync(transactionId, "rejecter@test.com", "Insufficient justification");

        // Assert
        result.IsValid.Should().BeTrue();
        _transactionRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Transaction>(t => t.OverrideStatus == OverrideStatus.Rejected),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectOverrideAsync_Fails_WhenTransactionNotFound()
    {
        // Arrange
        var transactionId = Guid.NewGuid();

        _transactionRepoMock.Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        // Act
        var result = await _service.RejectOverrideAsync(transactionId, "rejecter@test.com", "Reason");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.TRANSACTION_NOT_FOUND);
    }

    [Fact]
    public async Task RejectOverrideAsync_Fails_WhenReasonEmpty()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transaction = CreateFailedTransaction(transactionId);
        transaction.RequiresOverride = true;
        transaction.OverrideStatus = OverrideStatus.Pending;

        _transactionRepoMock.Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _service.RejectOverrideAsync(transactionId, "rejecter@test.com", "  ");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("reason"));
    }

    #endregion

    #region GetPendingOverridesAsync Tests

    [Fact]
    public async Task GetPendingOverridesAsync_ReturnsTransactions()
    {
        // Arrange
        var transactions = new[]
        {
            CreateFailedTransaction(Guid.NewGuid()),
            CreateFailedTransaction(Guid.NewGuid())
        };
        foreach (var t in transactions)
        {
            t.RequiresOverride = true;
            t.OverrideStatus = OverrideStatus.Pending;
        }

        _transactionRepoMock.Setup(r => r.GetPendingOverrideAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _service.GetPendingOverridesAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetTransactionByIdAsync Tests

    [Fact]
    public async Task GetTransactionByIdAsync_ReturnsTransaction_WhenExists()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transaction = CreateTransaction("CUST-001", "nlpd", Guid.NewGuid());
        transaction.Id = transactionId;

        _transactionRepoMock.Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _service.GetTransactionByIdAsync(transactionId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(transactionId);
    }

    [Fact]
    public async Task GetTransactionByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Arrange
        var transactionId = Guid.NewGuid();

        _transactionRepoMock.Setup(r => r.GetByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        // Act
        var result = await _service.GetTransactionByIdAsync(transactionId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private void SetupMocks(Customer? customer, ControlledSubstance? substance, Licence? licence)
    {
        if (customer != null)
        {
            _customerRepoMock.Setup(r => r.GetByAccountAsync(customer.CustomerAccount, customer.DataAreaId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);
        }

        if (substance != null)
        {
            _substanceRepoMock.Setup(r => r.GetByIdAsync(substance.SubstanceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(substance);
        }

        var licences = licence != null ? new[] { licence } : Array.Empty<Licence>();
        _licenceRepoMock.Setup(r => r.GetByHolderAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(licences);

        _thresholdRepoMock.Setup(r => r.GetApplicableThresholdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<Guid>(), It.IsAny<BusinessCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Threshold>());
        _thresholdRepoMock.Setup(r => r.GetByTypeAsync(It.IsAny<ThresholdType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Threshold>());
    }

    private static Customer CreateApprovedCustomer(string customerAccount = "CUST-001", string dataAreaId = "nlpd")
    {
        return new Customer
        {
            CustomerAccount = customerAccount,
            DataAreaId = dataAreaId,
            ComplianceExtensionId = Guid.NewGuid(),
            OrganizationName = "Test Pharmacy",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
            ApprovalStatus = ApprovalStatus.Approved,
            GdpQualificationStatus = GdpQualificationStatus.Approved,
            IsSuspended = false,
            AddressCountryRegionId = "NL"
        };
    }

    private static ControlledSubstance CreateControlledSubstance(Guid substanceId)
    {
        return new ControlledSubstance
        {
            SubstanceId = substanceId,
            SubstanceName = "Morphine Sulfate",
            InternalCode = "MORPH-001",
            OpiumActList = SubstanceCategories.OpiumActList.ListI,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
            IsActive = true
        };
    }

    private static Licence CreateValidLicence(Guid licenceId, Guid holderId)
    {
        return new Licence
        {
            LicenceId = licenceId,
            LicenceNumber = "LIC-2024-001",
            LicenceTypeId = WellKnownIds.OpiumExemptionTypeId,
            HolderType = "Customer",
            HolderId = holderId,
            IssuingAuthority = "IGJ",
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(4)),
            Status = "Valid",
            PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                 LicenceTypes.PermittedActivity.Store |
                                 LicenceTypes.PermittedActivity.Distribute
        };
    }

    private static Transaction CreateTransaction(string customerAccount, string customerDataAreaId, Guid substanceId)
    {
        var transaction = new Transaction
        {
            Id = Guid.Empty,
            ExternalId = "ORD-2024-001",
            TransactionType = TransactionType.Order,
            Direction = TransactionDirection.Internal,
            CustomerAccount = customerAccount,
            CustomerDataAreaId = customerDataAreaId,
            OriginCountry = "NL",
            TransactionDate = DateTime.UtcNow,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };

        transaction.Lines.Add(new TransactionLine
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            LineNumber = 1,
            SubstanceId = substanceId,
            SubstanceCode = "MORPH-001",
            Quantity = 100,
            BaseUnitQuantity = 100,
            BaseUnit = "g",
            UnitOfMeasure = "EA"
        });

        transaction.TotalQuantity = 100;

        return transaction;
    }

    private static Transaction CreateFailedTransaction(Guid transactionId)
    {
        var transaction = CreateTransaction("CUST-001", "nlpd", Guid.NewGuid());
        transaction.Id = transactionId;
        transaction.ValidationStatus = ValidationStatus.Failed;
        return transaction;
    }

    #endregion
}
