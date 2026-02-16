using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.ComplianceCore.Services.Reporting;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceCore.Tests.Services;

/// <summary>
/// T151: Unit tests for ReportingService per FR-026, FR-029.
/// Tests report generation for transactions, licences, and customer compliance history.
/// </summary>
public class ReportingServiceTests
{
    private readonly Mock<IAuditRepository> _auditRepoMock;
    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly Mock<ILicenceRepository> _licenceRepoMock;
    private readonly Mock<ICustomerRepository> _customerRepoMock;
    private readonly Mock<ILogger<ReportingService>> _loggerMock;
    private readonly ReportingService _service;

    public ReportingServiceTests()
    {
        _auditRepoMock = new Mock<IAuditRepository>();
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _licenceRepoMock = new Mock<ILicenceRepository>();
        _customerRepoMock = new Mock<ICustomerRepository>();
        _loggerMock = new Mock<ILogger<ReportingService>>();

        _service = new ReportingService(
            _auditRepoMock.Object,
            _transactionRepoMock.Object,
            _licenceRepoMock.Object,
            _customerRepoMock.Object,
            _loggerMock.Object);
    }

    #region Transaction Audit Report Tests (FR-026)

    [Fact]
    public async Task GenerateTransactionAuditReportAsync_ReturnsEmptyReport_WhenNoTransactions()
    {
        // Arrange
        var criteria = new TransactionAuditReportCriteria
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow
        };

        _transactionRepoMock.Setup(r => r.GetByDateRangeAsync(
                criteria.FromDate, criteria.ToDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Transaction>());

        // Act
        var report = await _service.GenerateTransactionAuditReportAsync(criteria);

        // Assert
        report.Should().NotBeNull();
        report.Transactions.Should().BeEmpty();
        report.TotalCount.Should().Be(0);
        report.GeneratedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GenerateTransactionAuditReportAsync_FiltersBySubstance_WhenSubstanceCodeProvided()
    {
        // Arrange
        var substanceCode = "Morphine";
        var criteria = new TransactionAuditReportCriteria
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            SubstanceCode = substanceCode
        };

        var transactions = new List<Transaction>
        {
            CreateTestTransaction("CUST-001", "nlpd", substanceCode),
            CreateTestTransaction("CUST-002", "nlpd", substanceCode)
        };

        _transactionRepoMock.Setup(r => r.GetBySubstanceAsync(
                substanceCode, criteria.FromDate, criteria.ToDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        // Act
        var report = await _service.GenerateTransactionAuditReportAsync(criteria);

        // Assert
        report.TotalCount.Should().Be(2);
        report.Transactions.Should().HaveCount(2);
        _transactionRepoMock.Verify(r => r.GetBySubstanceAsync(
            substanceCode, criteria.FromDate, criteria.ToDate, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateTransactionAuditReportAsync_FiltersByCustomer_WhenCustomerAccountProvided()
    {
        // Arrange
        var criteria = new TransactionAuditReportCriteria
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            CustomerAccount = "CUST-001",
            CustomerDataAreaId = "nlpd"
        };

        var transactions = new List<Transaction>
        {
            CreateTestTransaction("CUST-001", "nlpd", "Morphine")
        };

        _transactionRepoMock.Setup(r => r.GetByDateRangeAsync(
                criteria.FromDate, criteria.ToDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        // Act
        var report = await _service.GenerateTransactionAuditReportAsync(criteria);

        // Assert
        report.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GenerateTransactionAuditReportAsync_IncludesLicenceInfo_WhenIncludeLicenceDetailsTrue()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var criteria = new TransactionAuditReportCriteria
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            IncludeLicenceDetails = true
        };

        var transaction = CreateTestTransaction("CUST-001", "nlpd", "Morphine");
        transaction.LicenceUsages = new List<TransactionLicenceUsage>
        {
            new() { LicenceId = licenceId, TransactionId = transaction.Id }
        };

        _transactionRepoMock.Setup(r => r.GetByDateRangeAsync(
                criteria.FromDate, criteria.ToDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { transaction });

        _licenceRepoMock.Setup(r => r.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Licence
            {
                LicenceId = licenceId,
                LicenceNumber = "LIC-001",
                IssuingAuthority = "Test Authority",
                HolderType = "Customer",
                Status = "Valid"
            });

        // Act
        var report = await _service.GenerateTransactionAuditReportAsync(criteria);

        // Assert
        report.Transactions.Should().HaveCount(1);
        _licenceRepoMock.Verify(r => r.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Licence Usage Report Tests (FR-026)

    [Fact]
    public async Task GenerateLicenceUsageReportAsync_ReturnsUsageStatistics()
    {
        // Arrange
        var criteria = new LicenceUsageReportCriteria
        {
            FromDate = DateTime.UtcNow.AddDays(-90),
            ToDate = DateTime.UtcNow
        };

        var licences = new List<Licence>
        {
            new() { LicenceId = Guid.NewGuid(), LicenceNumber = "LIC-001", IssuingAuthority = "IGJ", HolderType = "Customer", Status = "Valid" },
            new() { LicenceId = Guid.NewGuid(), LicenceNumber = "LIC-002", IssuingAuthority = "IGJ", HolderType = "Customer", Status = "Valid" }
        };

        _licenceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(licences);

        _transactionRepoMock.Setup(r => r.GetByDateRangeAsync(
                criteria.FromDate, criteria.ToDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Transaction>());

        // Act
        var report = await _service.GenerateLicenceUsageReportAsync(criteria);

        // Assert
        report.Should().NotBeNull();
        report.LicenceUsages.Should().HaveCount(2);
        report.TotalLicences.Should().Be(2);
    }

    [Fact]
    public async Task GenerateLicenceUsageReportAsync_CalculatesTransactionCounts()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var criteria = new LicenceUsageReportCriteria
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            LicenceId = licenceId
        };

        var licence = new Licence
        {
            LicenceId = licenceId,
            LicenceNumber = "LIC-001",
            IssuingAuthority = "IGJ",
            HolderType = "Customer",
            Status = "Valid"
        };

        var transactions = new List<Transaction>
        {
            CreateTestTransactionWithLicence(licenceId),
            CreateTestTransactionWithLicence(licenceId),
            CreateTestTransactionWithLicence(licenceId)
        };

        _licenceRepoMock.Setup(r => r.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);

        _transactionRepoMock.Setup(r => r.GetByDateRangeAsync(
                criteria.FromDate, criteria.ToDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        // Act
        var report = await _service.GenerateLicenceUsageReportAsync(criteria);

        // Assert
        report.LicenceUsages.Should().ContainSingle();
        report.LicenceUsages.First().TransactionCount.Should().Be(3);
    }

    #endregion

    #region Customer Compliance History Report Tests (FR-029)

    [Fact]
    public async Task GenerateCustomerComplianceHistoryAsync_ReturnsFullHistory()
    {
        // Arrange
        var complianceExtensionId = Guid.NewGuid();
        var criteria = new CustomerComplianceHistoryCriteria
        {
            CustomerAccount = "CUST-001",
            DataAreaId = "nlpd"
        };

        var customer = new Customer
        {
            CustomerAccount = "CUST-001",
            DataAreaId = "nlpd",
            ComplianceExtensionId = complianceExtensionId,
            OrganizationName = "Test Customer",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
            ApprovalStatus = ApprovalStatus.Approved
        };

        var auditEvents = new List<AuditEvent>
        {
            AuditEvent.ForCreate(AuditEntityType.Customer, complianceExtensionId, Guid.NewGuid()),
            AuditEvent.ForCustomerStatusChange(complianceExtensionId, Guid.NewGuid(), AuditEventType.CustomerApproved)
        };

        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _auditRepoMock.Setup(r => r.GetCustomerComplianceHistoryAsync(
                complianceExtensionId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auditEvents);

        // Act
        var report = await _service.GenerateCustomerComplianceHistoryAsync(criteria);

        // Assert
        report.Should().NotBeNull();
        report.CustomerAccount.Should().Be("CUST-001");
        report.DataAreaId.Should().Be("nlpd");
        report.CustomerName.Should().Be("Test Customer");
        report.Events.Should().HaveCount(2);
    }

    [Fact]
    public async Task GenerateCustomerComplianceHistoryAsync_IncludesLicenceStatus()
    {
        // Arrange
        var complianceExtensionId = Guid.NewGuid();
        var criteria = new CustomerComplianceHistoryCriteria
        {
            CustomerAccount = "CUST-001",
            DataAreaId = "nlpd",
            IncludeLicenceStatus = true
        };

        var customer = new Customer
        {
            CustomerAccount = "CUST-001",
            DataAreaId = "nlpd",
            ComplianceExtensionId = complianceExtensionId,
            OrganizationName = "Test Customer",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
            ApprovalStatus = ApprovalStatus.Approved
        };

        var licences = new List<Licence>
        {
            new() { LicenceId = Guid.NewGuid(), LicenceNumber = "LIC-001", IssuingAuthority = "IGJ", HolderType = "Customer", Status = "Valid" }
        };

        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _licenceRepoMock.Setup(r => r.GetByHolderAsync(complianceExtensionId, "Customer", It.IsAny<CancellationToken>()))
            .ReturnsAsync(licences);

        _auditRepoMock.Setup(r => r.GetCustomerComplianceHistoryAsync(
                complianceExtensionId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<AuditEvent>());

        // Act
        var report = await _service.GenerateCustomerComplianceHistoryAsync(criteria);

        // Assert
        report.CurrentLicences.Should().HaveCount(1);
        _licenceRepoMock.Verify(r => r.GetByHolderAsync(
            complianceExtensionId, "Customer", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateCustomerComplianceHistoryAsync_FiltersByDateRange()
    {
        // Arrange
        var complianceExtensionId = Guid.NewGuid();
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;
        var criteria = new CustomerComplianceHistoryCriteria
        {
            CustomerAccount = "CUST-001",
            DataAreaId = "nlpd",
            FromDate = fromDate,
            ToDate = toDate
        };

        var customer = new Customer
        {
            CustomerAccount = "CUST-001",
            DataAreaId = "nlpd",
            ComplianceExtensionId = complianceExtensionId,
            OrganizationName = "Test Customer",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
            ApprovalStatus = ApprovalStatus.Approved
        };

        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _auditRepoMock.Setup(r => r.GetCustomerComplianceHistoryAsync(
                complianceExtensionId, fromDate, toDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<AuditEvent>());

        // Act
        var report = await _service.GenerateCustomerComplianceHistoryAsync(criteria);

        // Assert
        _auditRepoMock.Verify(r => r.GetCustomerComplianceHistoryAsync(
            complianceExtensionId, fromDate, toDate, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateCustomerComplianceHistoryAsync_ReturnsNull_WhenCustomerNotFound()
    {
        // Arrange
        var criteria = new CustomerComplianceHistoryCriteria
        {
            CustomerAccount = "CUST-NOTFOUND",
            DataAreaId = "nlpd"
        };

        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-NOTFOUND", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var report = await _service.GenerateCustomerComplianceHistoryAsync(criteria);

        // Assert
        report.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private static Transaction CreateTestTransaction(string customerAccount, string customerDataAreaId, string substanceCode)
    {
        var transactionId = Guid.NewGuid();
        return new Transaction
        {
            Id = transactionId,
            ExternalId = $"EXT-{Guid.NewGuid():N}".Substring(0, 20),
            CustomerAccount = customerAccount,
            CustomerDataAreaId = customerDataAreaId,
            TransactionType = TransactionType.Order,
            TransactionDate = DateTime.UtcNow.AddDays(-5),
            ValidationStatus = ValidationStatus.Passed,
            Lines = new List<TransactionLine>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    TransactionId = transactionId,
                    ItemNumber = "ITEM-001",
                    DataAreaId = customerDataAreaId,
                    SubstanceCode = substanceCode,
                    Quantity = 100
                }
            }
        };
    }

    private static Transaction CreateTestTransactionWithLicence(Guid licenceId)
    {
        var transactionId = Guid.NewGuid();
        return new Transaction
        {
            Id = transactionId,
            ExternalId = $"EXT-{Guid.NewGuid():N}".Substring(0, 20),
            CustomerAccount = "CUST-001",
            CustomerDataAreaId = "nlpd",
            TransactionType = TransactionType.Order,
            TransactionDate = DateTime.UtcNow.AddDays(-5),
            ValidationStatus = ValidationStatus.Passed,
            LicenceUsages = new List<TransactionLicenceUsage>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    TransactionId = transactionId,
                    LicenceId = licenceId
                }
            }
        };
    }

    #endregion
}
