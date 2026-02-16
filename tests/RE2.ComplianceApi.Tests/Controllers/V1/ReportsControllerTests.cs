using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RE2.ComplianceApi.Controllers.V1;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.ComplianceCore.Services.Reporting;
using RE2.Shared.Constants;
using RE2.Shared.Models;
using System.Security.Claims;

namespace RE2.ComplianceApi.Tests.Controllers.V1;

/// <summary>
/// Integration tests for ReportsController.
/// T152: Tests for audit report generation per FR-026, FR-029.
/// </summary>
public class ReportsControllerTests
{
    private readonly Mock<IReportingService> _mockReportingService;
    private readonly Mock<ILicenceCorrectionImpactService> _mockLicenceCorrectionImpactService;
    private readonly Mock<ILogger<ReportsController>> _mockLogger;
    private readonly ReportsController _controller;

    public ReportsControllerTests()
    {
        _mockReportingService = new Mock<IReportingService>();
        _mockLicenceCorrectionImpactService = new Mock<ILicenceCorrectionImpactService>();
        _mockLogger = new Mock<ILogger<ReportsController>>();

        _controller = new ReportsController(
            _mockReportingService.Object,
            _mockLicenceCorrectionImpactService.Object,
            _mockLogger.Object);

        // Setup default HttpContext with user in ComplianceManager role
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "compliance@example.com"),
            new Claim(ClaimTypes.Role, "ComplianceManager")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    #region GET /api/v1/reports/transaction-audit Tests (FR-026)

    [Fact]
    public async Task GetTransactionAuditReport_ReturnsOk_WithValidDateRange()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;

        var report = new TransactionAuditReport
        {
            GeneratedDate = DateTime.UtcNow,
            FromDate = fromDate,
            ToDate = toDate,
            Transactions = new List<TransactionAuditReportItem>
            {
                new()
                {
                    TransactionId = Guid.NewGuid(),
                    ExternalTransactionId = "EXT-001",
                    TransactionDate = DateTime.UtcNow.AddDays(-5),
                    CustomerAccount = "CUST-001",
                    CustomerDataAreaId = "nlpd",
                    TransactionType = "Order",
                    ValidationStatus = "Passed",
                    TotalQuantity = 100
                }
            },
            TotalCount = 1
        };

        _mockReportingService
            .Setup(s => s.GenerateTransactionAuditReportAsync(
                It.IsAny<TransactionAuditReportCriteria>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTransactionAuditReport(fromDate, toDate);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TransactionAuditReportDto>().Subject;
        response.TotalCount.Should().Be(1);
        response.Transactions.Should().HaveCount(1);
        response.FromDate.Should().Be(fromDate);
        response.ToDate.Should().Be(toDate);
    }

    [Fact]
    public async Task GetTransactionAuditReport_ReturnsBadRequest_WhenFromDateAfterToDate()
    {
        // Arrange
        var fromDate = DateTime.UtcNow;
        var toDate = DateTime.UtcNow.AddDays(-30);

        // Act
        var result = await _controller.GetTransactionAuditReport(fromDate, toDate);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
        error.Message.Should().Contain("FromDate must be before");
    }

    [Fact]
    public async Task GetTransactionAuditReport_FiltersBySubstance_WhenSubstanceCodeProvided()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;
        var substanceCode = "Morphine";

        var report = new TransactionAuditReport
        {
            GeneratedDate = DateTime.UtcNow,
            FromDate = fromDate,
            ToDate = toDate,
            Transactions = new List<TransactionAuditReportItem>(),
            TotalCount = 0,
            FilteredBySubstanceCode = substanceCode
        };

        _mockReportingService
            .Setup(s => s.GenerateTransactionAuditReportAsync(
                It.Is<TransactionAuditReportCriteria>(c => c.SubstanceCode == substanceCode),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTransactionAuditReport(
            fromDate, toDate, substanceCode: substanceCode);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TransactionAuditReportDto>().Subject;
        response.FilteredBySubstanceCode.Should().Be(substanceCode);
    }

    [Fact]
    public async Task GetTransactionAuditReport_FiltersByCustomer_WhenCustomerAccountProvided()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;

        var report = new TransactionAuditReport
        {
            GeneratedDate = DateTime.UtcNow,
            FromDate = fromDate,
            ToDate = toDate,
            Transactions = new List<TransactionAuditReportItem>(),
            TotalCount = 0,
            FilteredByCustomerAccount = "CUST-001",
            FilteredByCustomerDataAreaId = "nlpd"
        };

        _mockReportingService
            .Setup(s => s.GenerateTransactionAuditReportAsync(
                It.Is<TransactionAuditReportCriteria>(c =>
                    c.CustomerAccount == "CUST-001" && c.CustomerDataAreaId == "nlpd"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTransactionAuditReport(
            fromDate, toDate, customerAccount: "CUST-001", customerDataAreaId: "nlpd");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TransactionAuditReportDto>().Subject;
        response.FilteredByCustomerAccount.Should().Be("CUST-001");
        response.FilteredByCustomerDataAreaId.Should().Be("nlpd");
    }

    [Fact]
    public async Task GetTransactionAuditReport_FiltersByCountry_WhenCountryCodeProvided()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;
        var countryCode = "NL";

        var report = new TransactionAuditReport
        {
            GeneratedDate = DateTime.UtcNow,
            FromDate = fromDate,
            ToDate = toDate,
            Transactions = new List<TransactionAuditReportItem>(),
            TotalCount = 0,
            FilteredByCountry = countryCode
        };

        _mockReportingService
            .Setup(s => s.GenerateTransactionAuditReportAsync(
                It.Is<TransactionAuditReportCriteria>(c => c.CountryCode == countryCode),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTransactionAuditReport(
            fromDate, toDate, countryCode: countryCode);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TransactionAuditReportDto>().Subject;
        response.FilteredByCountry.Should().Be(countryCode);
    }

    [Fact]
    public async Task GetTransactionAuditReport_IncludesLicenceDetails_WhenRequested()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;

        var report = new TransactionAuditReport
        {
            GeneratedDate = DateTime.UtcNow,
            FromDate = fromDate,
            ToDate = toDate,
            Transactions = new List<TransactionAuditReportItem>
            {
                new()
                {
                    TransactionId = Guid.NewGuid(),
                    ExternalTransactionId = "EXT-001",
                    TransactionDate = DateTime.UtcNow.AddDays(-5),
                    CustomerAccount = "CUST-001",
                    CustomerDataAreaId = "nlpd",
                    TransactionType = "Order",
                    ValidationStatus = "Passed",
                    TotalQuantity = 100,
                    LicencesUsed = new List<LicenceReportDetail>
                    {
                        new()
                        {
                            LicenceId = Guid.NewGuid(),
                            LicenceNumber = "LIC-001",
                            Status = "Valid",
                            IssuingAuthority = "IGJ"
                        }
                    }
                }
            },
            TotalCount = 1
        };

        _mockReportingService
            .Setup(s => s.GenerateTransactionAuditReportAsync(
                It.Is<TransactionAuditReportCriteria>(c => c.IncludeLicenceDetails),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetTransactionAuditReport(
            fromDate, toDate, includeLicenceDetails: true);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TransactionAuditReportDto>().Subject;
        response.Transactions.First().LicencesUsed.Should().HaveCount(1);
    }

    #endregion

    #region POST /api/v1/reports/transaction-audit Tests

    [Fact]
    public async Task GenerateTransactionAuditReport_ReturnsOk_WithValidRequest()
    {
        // Arrange
        var request = new TransactionAuditReportRequestDto
        {
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            SubstanceCode = "Morphine",
            IncludeLicenceDetails = true
        };

        var report = new TransactionAuditReport
        {
            GeneratedDate = DateTime.UtcNow,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Transactions = new List<TransactionAuditReportItem>(),
            TotalCount = 0,
            FilteredBySubstanceCode = request.SubstanceCode
        };

        _mockReportingService
            .Setup(s => s.GenerateTransactionAuditReportAsync(
                It.IsAny<TransactionAuditReportCriteria>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GenerateTransactionAuditReport(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TransactionAuditReportDto>().Subject;
        response.FilteredBySubstanceCode.Should().Be(request.SubstanceCode);
    }

    #endregion

    #region GET /api/v1/reports/licence-usage Tests (FR-026)

    [Fact]
    public async Task GetLicenceUsageReport_ReturnsOk_WithValidDateRange()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-90);
        var toDate = DateTime.UtcNow;

        var report = new LicenceUsageReport
        {
            GeneratedDate = DateTime.UtcNow,
            FromDate = fromDate,
            ToDate = toDate,
            LicenceUsages = new List<LicenceUsageReportItem>
            {
                new()
                {
                    LicenceId = Guid.NewGuid(),
                    LicenceNumber = "LIC-001",
                    Status = "Valid",
                    IssuingAuthority = "IGJ",
                    TransactionCount = 10,
                    TotalQuantityProcessed = 5000,
                    LastUsedDate = DateTime.UtcNow.AddDays(-2)
                }
            },
            TotalLicences = 1,
            TotalTransactions = 10
        };

        _mockReportingService
            .Setup(s => s.GenerateLicenceUsageReportAsync(
                It.IsAny<LicenceUsageReportCriteria>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetLicenceUsageReport(fromDate, toDate);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LicenceUsageReportDto>().Subject;
        response.TotalLicences.Should().Be(1);
        response.TotalTransactions.Should().Be(10);
        response.LicenceUsages.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetLicenceUsageReport_ReturnsBadRequest_WhenFromDateAfterToDate()
    {
        // Arrange
        var fromDate = DateTime.UtcNow;
        var toDate = DateTime.UtcNow.AddDays(-30);

        // Act
        var result = await _controller.GetLicenceUsageReport(fromDate, toDate);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    [Fact]
    public async Task GetLicenceUsageReport_FiltersByLicence_WhenLicenceIdProvided()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;
        var licenceId = Guid.NewGuid();

        var report = new LicenceUsageReport
        {
            GeneratedDate = DateTime.UtcNow,
            FromDate = fromDate,
            ToDate = toDate,
            LicenceUsages = new List<LicenceUsageReportItem>
            {
                new()
                {
                    LicenceId = licenceId,
                    LicenceNumber = "LIC-001",
                    Status = "Valid",
                    IssuingAuthority = "IGJ",
                    TransactionCount = 5
                }
            },
            TotalLicences = 1,
            TotalTransactions = 5
        };

        _mockReportingService
            .Setup(s => s.GenerateLicenceUsageReportAsync(
                It.Is<LicenceUsageReportCriteria>(c => c.LicenceId == licenceId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetLicenceUsageReport(fromDate, toDate, licenceId: licenceId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LicenceUsageReportDto>().Subject;
        response.LicenceUsages.Should().ContainSingle().Which.LicenceId.Should().Be(licenceId);
    }

    #endregion

    #region GET /api/v1/reports/customer-compliance/{customerId} Tests (FR-029)

    [Fact]
    public async Task GetCustomerComplianceHistory_ReturnsOk_WhenCustomerExists()
    {
        // Arrange
        var report = new CustomerComplianceHistoryReport
        {
            GeneratedDate = DateTime.UtcNow,
            CustomerAccount = "CUST-001",
            DataAreaId = "nlpd",
            CustomerName = "Test Pharmacy",
            BusinessCategory = "CommunityPharmacy",
            ApprovalStatus = "Approved",
            Events = new List<ComplianceEventItem>
            {
                new()
                {
                    EventId = Guid.NewGuid(),
                    EventType = "CustomerCreated",
                    EventDate = DateTime.UtcNow.AddMonths(-6),
                    PerformedBy = "admin@example.com"
                },
                new()
                {
                    EventId = Guid.NewGuid(),
                    EventType = "CustomerApproved",
                    EventDate = DateTime.UtcNow.AddMonths(-5),
                    PerformedBy = "compliance@example.com"
                }
            }
        };

        _mockReportingService
            .Setup(s => s.GenerateCustomerComplianceHistoryAsync(
                It.Is<CustomerComplianceHistoryCriteria>(c =>
                    c.CustomerAccount == "CUST-001" && c.DataAreaId == "nlpd"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetCustomerComplianceHistory("CUST-001", "nlpd");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<CustomerComplianceHistoryReportDto>().Subject;
        response.CustomerAccount.Should().Be("CUST-001");
        response.CustomerName.Should().Be("Test Pharmacy");
        response.Events.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCustomerComplianceHistory_ReturnsNotFound_WhenCustomerDoesNotExist()
    {
        // Arrange
        _mockReportingService
            .Setup(s => s.GenerateCustomerComplianceHistoryAsync(
                It.IsAny<CustomerComplianceHistoryCriteria>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerComplianceHistoryReport?)null);

        // Act
        var result = await _controller.GetCustomerComplianceHistory("CUST-NOTFOUND", "nlpd");

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.CUSTOMER_NOT_FOUND);
    }

    [Fact]
    public async Task GetCustomerComplianceHistory_FiltersByDateRange_WhenProvided()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;

        var report = new CustomerComplianceHistoryReport
        {
            GeneratedDate = DateTime.UtcNow,
            CustomerAccount = "CUST-001",
            DataAreaId = "nlpd",
            CustomerName = "Test Pharmacy",
            BusinessCategory = "CommunityPharmacy",
            ApprovalStatus = "Approved",
            Events = new List<ComplianceEventItem>()
        };

        _mockReportingService
            .Setup(s => s.GenerateCustomerComplianceHistoryAsync(
                It.Is<CustomerComplianceHistoryCriteria>(c =>
                    c.CustomerAccount == "CUST-001" &&
                    c.DataAreaId == "nlpd" &&
                    c.FromDate == fromDate &&
                    c.ToDate == toDate),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetCustomerComplianceHistory(
            "CUST-001", "nlpd", fromDate: fromDate, toDate: toDate);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockReportingService.Verify(s => s.GenerateCustomerComplianceHistoryAsync(
            It.Is<CustomerComplianceHistoryCriteria>(c => c.FromDate == fromDate && c.ToDate == toDate),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCustomerComplianceHistory_IncludesLicenceStatus_WhenRequested()
    {
        // Arrange
        var report = new CustomerComplianceHistoryReport
        {
            GeneratedDate = DateTime.UtcNow,
            CustomerAccount = "CUST-001",
            DataAreaId = "nlpd",
            CustomerName = "Test Pharmacy",
            BusinessCategory = "CommunityPharmacy",
            ApprovalStatus = "Approved",
            Events = new List<ComplianceEventItem>(),
            CurrentLicences = new List<LicenceStatusItem>
            {
                new()
                {
                    LicenceId = Guid.NewGuid(),
                    LicenceNumber = "LIC-001",
                    Status = "Valid",
                    ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
                    IssuingAuthority = "IGJ"
                }
            }
        };

        _mockReportingService
            .Setup(s => s.GenerateCustomerComplianceHistoryAsync(
                It.Is<CustomerComplianceHistoryCriteria>(c => c.IncludeLicenceStatus),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GetCustomerComplianceHistory(
            "CUST-001", "nlpd", includeLicenceStatus: true);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<CustomerComplianceHistoryReportDto>().Subject;
        response.CurrentLicences.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCustomerComplianceHistory_ReturnsBadRequest_WhenFromDateAfterToDate()
    {
        // Arrange
        var fromDate = DateTime.UtcNow;
        var toDate = DateTime.UtcNow.AddDays(-30);

        // Act
        var result = await _controller.GetCustomerComplianceHistory(
            "CUST-001", "nlpd", fromDate: fromDate, toDate: toDate);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    #endregion

    #region POST /api/v1/reports/customer-compliance Tests

    [Fact]
    public async Task GenerateCustomerComplianceHistory_ReturnsOk_WithValidRequest()
    {
        // Arrange
        var request = new CustomerComplianceHistoryRequestDto
        {
            CustomerAccount = "CUST-001",
            DataAreaId = "nlpd",
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow,
            IncludeLicenceStatus = true
        };

        var report = new CustomerComplianceHistoryReport
        {
            GeneratedDate = DateTime.UtcNow,
            CustomerAccount = request.CustomerAccount,
            DataAreaId = request.DataAreaId,
            CustomerName = "Test Pharmacy",
            BusinessCategory = "CommunityPharmacy",
            ApprovalStatus = "Approved",
            Events = new List<ComplianceEventItem>(),
            CurrentLicences = new List<LicenceStatusItem>()
        };

        _mockReportingService
            .Setup(s => s.GenerateCustomerComplianceHistoryAsync(
                It.IsAny<CustomerComplianceHistoryCriteria>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _controller.GenerateCustomerComplianceHistory(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<CustomerComplianceHistoryReportDto>().Subject;
        response.CustomerAccount.Should().Be(request.CustomerAccount);
    }

    [Fact]
    public async Task GenerateCustomerComplianceHistory_ReturnsNotFound_WhenCustomerDoesNotExist()
    {
        // Arrange
        var request = new CustomerComplianceHistoryRequestDto
        {
            CustomerAccount = "CUST-NOTFOUND",
            DataAreaId = "nlpd"
        };

        _mockReportingService
            .Setup(s => s.GenerateCustomerComplianceHistoryAsync(
                It.IsAny<CustomerComplianceHistoryCriteria>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerComplianceHistoryReport?)null);

        // Act
        var result = await _controller.GenerateCustomerComplianceHistory(request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.CUSTOMER_NOT_FOUND);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetTransactionAuditReport_ReturnsBadRequest_WhenServiceThrowsException()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;

        _mockReportingService
            .Setup(s => s.GenerateTransactionAuditReportAsync(
                It.IsAny<TransactionAuditReportCriteria>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _controller.GetTransactionAuditReport(fromDate, toDate);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.INTERNAL_ERROR);
    }

    [Fact]
    public async Task GetLicenceUsageReport_ReturnsBadRequest_WhenServiceThrowsException()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;

        _mockReportingService
            .Setup(s => s.GenerateLicenceUsageReportAsync(
                It.IsAny<LicenceUsageReportCriteria>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _controller.GetLicenceUsageReport(fromDate, toDate);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.INTERNAL_ERROR);
    }

    [Fact]
    public async Task GetCustomerComplianceHistory_ReturnsBadRequest_WhenServiceThrowsException()
    {
        // Arrange
        _mockReportingService
            .Setup(s => s.GenerateCustomerComplianceHistoryAsync(
                It.IsAny<CustomerComplianceHistoryCriteria>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _controller.GetCustomerComplianceHistory("CUST-001", "nlpd");

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.INTERNAL_ERROR);
    }

    #endregion
}
