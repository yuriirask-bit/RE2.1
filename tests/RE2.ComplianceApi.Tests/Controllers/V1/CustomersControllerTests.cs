using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RE2.ComplianceApi.Controllers.V1;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Tests.Controllers.V1;

/// <summary>
/// T084: Integration tests for GET /api/v1/customers/{id}/compliance-status endpoint.
/// Tests CustomersController with mocked dependencies.
/// </summary>
public class CustomersControllerTests
{
    private readonly Mock<ICustomerService> _mockCustomerService;
    private readonly Mock<ILogger<CustomersController>> _mockLogger;
    private readonly CustomersController _controller;

    public CustomersControllerTests()
    {
        _mockCustomerService = new Mock<ICustomerService>();
        _mockLogger = new Mock<ILogger<CustomersController>>();

        _controller = new CustomersController(_mockCustomerService.Object, _mockLogger.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GET /api/v1/customers/{id}/compliance-status Tests

    [Fact]
    public async Task GetComplianceStatus_ReturnsOk_WhenCustomerExists()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var complianceStatus = CreateTestComplianceStatus(customerId);

        _mockCustomerService
            .Setup(s => s.GetComplianceStatusAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(complianceStatus);

        // Act
        var result = await _controller.GetComplianceStatus(customerId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<CustomerComplianceStatusResponse>().Subject;
        response.CustomerId.Should().Be(customerId);
        response.BusinessName.Should().Be("Test Customer");
        response.CanTransact.Should().BeTrue();
    }

    [Fact]
    public async Task GetComplianceStatus_ReturnsNotFound_WhenCustomerDoesNotExist()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var complianceStatus = new CustomerComplianceStatus
        {
            CustomerId = customerId,
            BusinessName = "Unknown",
            ApprovalStatus = ApprovalStatus.Rejected,
            CanTransact = false,
            Warnings = new List<ComplianceWarning>
            {
                new ComplianceWarning
                {
                    WarningCode = ErrorCodes.NOT_FOUND,
                    Message = "Customer not found",
                    Severity = "Error"
                }
            }
        };

        _mockCustomerService
            .Setup(s => s.GetComplianceStatusAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(complianceStatus);

        // Act
        var result = await _controller.GetComplianceStatus(customerId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.NOT_FOUND);
    }

    [Fact]
    public async Task GetComplianceStatus_ReturnsSuspendedWarning_WhenCustomerSuspended()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var complianceStatus = new CustomerComplianceStatus
        {
            CustomerId = customerId,
            BusinessName = "Suspended Customer",
            ApprovalStatus = ApprovalStatus.Approved,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            IsSuspended = true,
            SuspensionReason = "Under investigation",
            CanTransact = false,
            Warnings = new List<ComplianceWarning>
            {
                new ComplianceWarning
                {
                    WarningCode = ErrorCodes.CUSTOMER_SUSPENDED,
                    Message = "Customer is suspended: Under investigation",
                    Severity = "Error"
                }
            }
        };

        _mockCustomerService
            .Setup(s => s.GetComplianceStatusAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(complianceStatus);

        // Act
        var result = await _controller.GetComplianceStatus(customerId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<CustomerComplianceStatusResponse>().Subject;
        response.IsSuspended.Should().BeTrue();
        response.CanTransact.Should().BeFalse();
        response.Warnings.Should().Contain(w => w.WarningCode == ErrorCodes.CUSTOMER_SUSPENDED);
    }

    [Fact]
    public async Task GetComplianceStatus_ReturnsPendingWarning_WhenCustomerPending()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var complianceStatus = new CustomerComplianceStatus
        {
            CustomerId = customerId,
            BusinessName = "Pending Customer",
            ApprovalStatus = ApprovalStatus.Pending,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            IsSuspended = false,
            CanTransact = false,
            Warnings = new List<ComplianceWarning>
            {
                new ComplianceWarning
                {
                    WarningCode = ErrorCodes.CUSTOMER_NOT_APPROVED,
                    Message = "Customer qualification is pending",
                    Severity = "Warning"
                }
            }
        };

        _mockCustomerService
            .Setup(s => s.GetComplianceStatusAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(complianceStatus);

        // Act
        var result = await _controller.GetComplianceStatus(customerId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<CustomerComplianceStatusResponse>().Subject;
        response.ApprovalStatus.Should().Be("Pending");
        response.CanTransact.Should().BeFalse();
        response.Warnings.Should().Contain(w => w.WarningCode == ErrorCodes.CUSTOMER_NOT_APPROVED);
    }

    [Fact]
    public async Task GetComplianceStatus_ReturnsReVerificationDueWarning_WhenDue()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var complianceStatus = new CustomerComplianceStatus
        {
            CustomerId = customerId,
            BusinessName = "Overdue Customer",
            ApprovalStatus = ApprovalStatus.Approved,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            IsSuspended = false,
            CanTransact = true,
            NextReVerificationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            IsReVerificationDue = true,
            Warnings = new List<ComplianceWarning>
            {
                new ComplianceWarning
                {
                    WarningCode = "REVERIFICATION_DUE",
                    Message = $"Customer re-verification was due on {DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30))}",
                    Severity = "Warning"
                }
            }
        };

        _mockCustomerService
            .Setup(s => s.GetComplianceStatusAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(complianceStatus);

        // Act
        var result = await _controller.GetComplianceStatus(customerId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<CustomerComplianceStatusResponse>().Subject;
        response.IsReVerificationDue.Should().BeTrue();
        response.Warnings.Should().Contain(w => w.WarningCode == "REVERIFICATION_DUE");
    }

    #endregion

    #region GET /api/v1/customers Tests

    [Fact]
    public async Task GetCustomers_ReturnsAllCustomers_WhenNoFilters()
    {
        // Arrange
        var customers = CreateTestCustomers();
        _mockCustomerService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        // Act
        var result = await _controller.GetCustomers();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<CustomerResponseDto>>().Subject;
        response.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetCustomers_FiltersByStatus_WhenStatusProvided()
    {
        // Arrange
        var customers = CreateTestCustomers().Where(c => c.ApprovalStatus == ApprovalStatus.Approved);
        _mockCustomerService
            .Setup(s => s.GetByApprovalStatusAsync(ApprovalStatus.Approved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        // Act
        var result = await _controller.GetCustomers(status: "Approved");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<CustomerResponseDto>>().Subject;
        response.Should().OnlyContain(c => c.ApprovalStatus == "Approved");
    }

    [Fact]
    public async Task GetCustomers_ReturnsEmpty_WhenNoCustomersExist()
    {
        // Arrange
        _mockCustomerService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Customer>());

        // Act
        var result = await _controller.GetCustomers();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<CustomerResponseDto>>().Subject;
        response.Should().BeEmpty();
    }

    #endregion

    #region GET /api/v1/customers/{id} Tests

    [Fact]
    public async Task GetCustomer_ReturnsCustomer_WhenExists()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = CreateTestCustomer(customerId);
        _mockCustomerService
            .Setup(s => s.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var result = await _controller.GetCustomer(customerId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<CustomerResponseDto>().Subject;
        response.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public async Task GetCustomer_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        _mockCustomerService
            .Setup(s => s.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _controller.GetCustomer(customerId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.NOT_FOUND);
    }

    #endregion

    #region POST /api/v1/customers/{id}/suspend Tests

    [Fact]
    public async Task SuspendCustomer_ReturnsOk_WhenSuccess()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var request = new SuspendCustomerRequestDto { Reason = "Suspicious activity" };

        _mockCustomerService
            .Setup(s => s.SuspendCustomerAsync(customerId, request.Reason, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        var complianceStatus = new CustomerComplianceStatus
        {
            CustomerId = customerId,
            BusinessName = "Suspended Customer",
            ApprovalStatus = ApprovalStatus.Approved,
            IsSuspended = true,
            SuspensionReason = request.Reason,
            CanTransact = false,
            Warnings = new List<ComplianceWarning>
            {
                new ComplianceWarning
                {
                    WarningCode = ErrorCodes.CUSTOMER_SUSPENDED,
                    Message = $"Customer is suspended: {request.Reason}",
                    Severity = "Error"
                }
            }
        };

        _mockCustomerService
            .Setup(s => s.GetComplianceStatusAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(complianceStatus);

        // Act
        var result = await _controller.SuspendCustomer(customerId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<CustomerComplianceStatusResponse>().Subject;
        response.IsSuspended.Should().BeTrue();
        response.CanTransact.Should().BeFalse();
    }

    [Fact]
    public async Task SuspendCustomer_ReturnsNotFound_WhenCustomerDoesNotExist()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var request = new SuspendCustomerRequestDto { Reason = "Test" };

        _mockCustomerService
            .Setup(s => s.SuspendCustomerAsync(customerId, request.Reason, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Customer with ID '{customerId}' not found"
                }
            }));

        // Act
        var result = await _controller.SuspendCustomer(customerId, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.NOT_FOUND);
    }

    #endregion

    #region POST /api/v1/customers/{id}/reinstate Tests

    [Fact]
    public async Task ReinstateCustomer_ReturnsOk_WhenSuccess()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        _mockCustomerService
            .Setup(s => s.ReinstateCustomerAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        var complianceStatus = new CustomerComplianceStatus
        {
            CustomerId = customerId,
            BusinessName = "Reinstated Customer",
            ApprovalStatus = ApprovalStatus.Approved,
            IsSuspended = false,
            CanTransact = true,
            Warnings = new List<ComplianceWarning>()
        };

        _mockCustomerService
            .Setup(s => s.GetComplianceStatusAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(complianceStatus);

        // Act
        var result = await _controller.ReinstateCustomer(customerId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<CustomerComplianceStatusResponse>().Subject;
        response.IsSuspended.Should().BeFalse();
        response.CanTransact.Should().BeTrue();
    }

    #endregion

    #region GET /api/v1/customers/reverification-due Tests

    [Fact]
    public async Task GetReVerificationDue_ReturnsCustomers_WithDefaultDays()
    {
        // Arrange
        var customers = CreateTestCustomers().Take(1);
        _mockCustomerService
            .Setup(s => s.GetReVerificationDueAsync(90, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        // Act
        var result = await _controller.GetReVerificationDue();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<CustomerResponseDto>>().Subject;
        response.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetReVerificationDue_ReturnsCustomers_WithCustomDays()
    {
        // Arrange
        var daysAhead = 30;
        var customers = CreateTestCustomers().Take(1);
        _mockCustomerService
            .Setup(s => s.GetReVerificationDueAsync(daysAhead, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        // Act
        var result = await _controller.GetReVerificationDue(daysAhead);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        _mockCustomerService.Verify(s => s.GetReVerificationDueAsync(daysAhead, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static Customer CreateTestCustomer(Guid customerId)
    {
        return new Customer
        {
            CustomerId = customerId,
            BusinessName = "Test Pharmacy",
            RegistrationNumber = "KVK-12345678",
            BusinessCategory = BusinessCategory.CommunityPharmacy,
            Country = "NL",
            ApprovalStatus = ApprovalStatus.Approved,
            OnboardingDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-6)),
            NextReVerificationDate = DateOnly.FromDateTime(DateTime.Now.AddYears(2).AddMonths(6)),
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            IsSuspended = false,
            CreatedDate = DateTime.UtcNow.AddMonths(-6),
            ModifiedDate = DateTime.UtcNow
        };
    }

    private static List<Customer> CreateTestCustomers()
    {
        return new List<Customer>
        {
            new Customer
            {
                CustomerId = Guid.NewGuid(),
                BusinessName = "Hospital Pharmacy",
                BusinessCategory = BusinessCategory.HospitalPharmacy,
                Country = "NL",
                ApprovalStatus = ApprovalStatus.Approved,
                GdpQualificationStatus = GdpQualificationStatus.NotRequired,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new Customer
            {
                CustomerId = Guid.NewGuid(),
                BusinessName = "Pending Wholesaler",
                BusinessCategory = BusinessCategory.WholesalerEU,
                Country = "DE",
                ApprovalStatus = ApprovalStatus.Pending,
                GdpQualificationStatus = GdpQualificationStatus.Pending,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new Customer
            {
                CustomerId = Guid.NewGuid(),
                BusinessName = "Suspended Manufacturer",
                BusinessCategory = BusinessCategory.Manufacturer,
                Country = "BE",
                ApprovalStatus = ApprovalStatus.Approved,
                GdpQualificationStatus = GdpQualificationStatus.Approved,
                IsSuspended = true,
                SuspensionReason = "Under investigation",
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            }
        };
    }

    private static CustomerComplianceStatus CreateTestComplianceStatus(Guid customerId)
    {
        return new CustomerComplianceStatus
        {
            CustomerId = customerId,
            BusinessName = "Test Customer",
            ApprovalStatus = ApprovalStatus.Approved,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            IsSuspended = false,
            CanTransact = true,
            NextReVerificationDate = DateOnly.FromDateTime(DateTime.Now.AddYears(2)),
            IsReVerificationDue = false,
            Warnings = new List<ComplianceWarning>()
        };
    }

    #endregion
}
