using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.ComplianceCore.Services.CustomerQualification;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Tests.Services;

/// <summary>
/// T284: Unit tests for CustomerService.
/// Tests CRUD, compliance configuration, suspension, reinstatement, and compliance status queries.
/// </summary>
public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _customerRepoMock;
    private readonly Mock<ILicenceRepository> _licenceRepoMock;
    private readonly Mock<ILogger<CustomerService>> _loggerMock;
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _customerRepoMock = new Mock<ICustomerRepository>();
        _licenceRepoMock = new Mock<ILicenceRepository>();
        _loggerMock = new Mock<ILogger<CustomerService>>();

        _service = new CustomerService(
            _customerRepoMock.Object,
            _licenceRepoMock.Object,
            _loggerMock.Object);
    }

    #region GetByAccountAsync Tests

    [Fact]
    public async Task GetByAccountAsync_ReturnsCustomer_WhenFound()
    {
        // Arrange
        var customer = CreateValidCustomer();
        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var result = await _service.GetByAccountAsync("CUST-001", "nlpd");

        // Assert
        result.Should().NotBeNull();
        result!.CustomerAccount.Should().Be("CUST-001");
    }

    [Fact]
    public async Task GetByAccountAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        _customerRepoMock.Setup(r => r.GetByAccountAsync("NONEXISTENT", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _service.GetByAccountAsync("NONEXISTENT", "nlpd");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllCustomers()
    {
        // Arrange
        var customers = new List<Customer> { CreateValidCustomer(), CreateValidCustomer("CUST-002") };
        _customerRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetAllD365CustomersAsync Tests

    [Fact]
    public async Task GetAllD365CustomersAsync_ReturnsList()
    {
        // Arrange
        var customers = new List<Customer> { CreateValidCustomer() };
        _customerRepoMock.Setup(r => r.GetAllD365CustomersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        // Act
        var result = await _service.GetAllD365CustomersAsync();

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region GetByApprovalStatusAsync Tests

    [Fact]
    public async Task GetByApprovalStatusAsync_ReturnsFilteredCustomers()
    {
        // Arrange
        var customers = new List<Customer> { CreateValidCustomer() };
        _customerRepoMock.Setup(r => r.GetByApprovalStatusAsync(ApprovalStatus.Approved, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        // Act
        var result = await _service.GetByApprovalStatusAsync(ApprovalStatus.Approved);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region GetByBusinessCategoryAsync Tests

    [Fact]
    public async Task GetByBusinessCategoryAsync_ReturnsFilteredCustomers()
    {
        // Arrange
        var customers = new List<Customer> { CreateValidCustomer() };
        _customerRepoMock.Setup(r => r.GetByBusinessCategoryAsync(BusinessCategory.CommunityPharmacy, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        // Act
        var result = await _service.GetByBusinessCategoryAsync(BusinessCategory.CommunityPharmacy);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region ConfigureComplianceAsync Tests

    [Fact]
    public async Task ConfigureComplianceAsync_Succeeds_WhenValid()
    {
        // Arrange
        var customer = CreateValidCustomer();
        var expectedId = Guid.NewGuid();

        _customerRepoMock.Setup(r => r.GetD365CustomerAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);
        _customerRepoMock.Setup(r => r.SaveComplianceExtensionAsync(customer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        var (id, result) = await _service.ConfigureComplianceAsync(customer);

        // Assert
        result.IsValid.Should().BeTrue();
        id.Should().Be(expectedId);
    }

    [Fact]
    public async Task ConfigureComplianceAsync_Fails_WhenD365CustomerNotFound()
    {
        // Arrange
        var customer = CreateValidCustomer();

        _customerRepoMock.Setup(r => r.GetD365CustomerAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var (id, result) = await _service.ConfigureComplianceAsync(customer);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.NOT_FOUND);
    }

    [Fact]
    public async Task ConfigureComplianceAsync_Fails_WhenAlreadyConfigured()
    {
        // Arrange
        var customer = CreateValidCustomer();
        var existing = CreateValidCustomer();

        _customerRepoMock.Setup(r => r.GetD365CustomerAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var (id, result) = await _service.ConfigureComplianceAsync(customer);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.Message.Contains("already configured"));
    }

    #endregion

    #region UpdateComplianceAsync Tests

    [Fact]
    public async Task UpdateComplianceAsync_Succeeds_WhenFound()
    {
        // Arrange
        var customer = CreateValidCustomer();
        var existing = CreateValidCustomer();
        existing.ComplianceExtensionId = Guid.NewGuid();

        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _service.UpdateComplianceAsync(customer);

        // Assert
        result.IsValid.Should().BeTrue();
        _customerRepoMock.Verify(r => r.UpdateComplianceExtensionAsync(
            It.Is<Customer>(c => c.ComplianceExtensionId == existing.ComplianceExtensionId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateComplianceAsync_Fails_WhenNotFound()
    {
        // Arrange
        var customer = CreateValidCustomer();
        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _service.UpdateComplianceAsync(customer);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.NOT_FOUND);
    }

    #endregion

    #region RemoveComplianceAsync Tests

    [Fact]
    public async Task RemoveComplianceAsync_Succeeds_WhenFound()
    {
        // Arrange
        var existing = CreateValidCustomer();
        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _service.RemoveComplianceAsync("CUST-001", "nlpd");

        // Assert
        result.IsValid.Should().BeTrue();
        _customerRepoMock.Verify(r => r.DeleteComplianceExtensionAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveComplianceAsync_Fails_WhenNotFound()
    {
        // Arrange
        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _service.RemoveComplianceAsync("CUST-001", "nlpd");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.NOT_FOUND);
    }

    #endregion

    #region SuspendCustomerAsync Tests

    [Fact]
    public async Task SuspendCustomerAsync_Succeeds_WhenFound()
    {
        // Arrange
        var customer = CreateValidCustomer();
        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var result = await _service.SuspendCustomerAsync("CUST-001", "nlpd", "Compliance violation");

        // Assert
        result.IsValid.Should().BeTrue();
        _customerRepoMock.Verify(r => r.UpdateComplianceExtensionAsync(
            It.Is<Customer>(c => c.IsSuspended && c.SuspensionReason == "Compliance violation"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SuspendCustomerAsync_Fails_WhenNotFound()
    {
        // Arrange
        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _service.SuspendCustomerAsync("CUST-001", "nlpd", "Reason");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.NOT_FOUND);
    }

    #endregion

    #region ReinstateCustomerAsync Tests

    [Fact]
    public async Task ReinstateCustomerAsync_Succeeds_WhenFound()
    {
        // Arrange
        var customer = CreateValidCustomer();
        customer.IsSuspended = true;
        customer.SuspensionReason = "Previous violation";

        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var result = await _service.ReinstateCustomerAsync("CUST-001", "nlpd");

        // Assert
        result.IsValid.Should().BeTrue();
        _customerRepoMock.Verify(r => r.UpdateComplianceExtensionAsync(
            It.Is<Customer>(c => !c.IsSuspended && c.SuspensionReason == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReinstateCustomerAsync_Fails_WhenNotFound()
    {
        // Arrange
        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await _service.ReinstateCustomerAsync("CUST-001", "nlpd");

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region GetComplianceStatusAsync Tests

    [Fact]
    public async Task GetComplianceStatusAsync_ReturnsFullStatus_WhenFound()
    {
        // Arrange
        var customer = CreateValidCustomer();
        customer.ApprovalStatus = ApprovalStatus.Approved;

        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var status = await _service.GetComplianceStatusAsync("CUST-001", "nlpd");

        // Assert
        status.Should().NotBeNull();
        status.CustomerAccount.Should().Be("CUST-001");
        status.CanTransact.Should().BeTrue();
        status.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task GetComplianceStatusAsync_IncludesSuspendedWarning_WhenSuspended()
    {
        // Arrange
        var customer = CreateValidCustomer();
        customer.ApprovalStatus = ApprovalStatus.Approved;
        customer.IsSuspended = true;
        customer.SuspensionReason = "Compliance violation";

        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var status = await _service.GetComplianceStatusAsync("CUST-001", "nlpd");

        // Assert
        status.CanTransact.Should().BeFalse();
        status.IsSuspended.Should().BeTrue();
        status.Warnings.Should().Contain(w => w.WarningCode == ErrorCodes.CUSTOMER_SUSPENDED);
    }

    [Fact]
    public async Task GetComplianceStatusAsync_ReturnsNotFound_WhenCustomerMissing()
    {
        // Arrange
        _customerRepoMock.Setup(r => r.GetByAccountAsync("MISSING", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var status = await _service.GetComplianceStatusAsync("MISSING", "nlpd");

        // Assert
        status.CanTransact.Should().BeFalse();
        status.Warnings.Should().Contain(w => w.WarningCode == ErrorCodes.NOT_FOUND);
    }

    [Fact]
    public async Task GetComplianceStatusAsync_IncludesPendingApprovalWarning()
    {
        // Arrange
        var customer = CreateValidCustomer();
        customer.ApprovalStatus = ApprovalStatus.Pending;

        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var status = await _service.GetComplianceStatusAsync("CUST-001", "nlpd");

        // Assert
        status.Warnings.Should().Contain(w => w.WarningCode == ErrorCodes.CUSTOMER_NOT_APPROVED);
        status.CanTransact.Should().BeFalse();
    }

    [Fact]
    public async Task GetComplianceStatusAsync_IncludesReVerificationDueWarning()
    {
        // Arrange
        var customer = CreateValidCustomer();
        customer.ApprovalStatus = ApprovalStatus.Approved;
        customer.NextReVerificationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));

        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var status = await _service.GetComplianceStatusAsync("CUST-001", "nlpd");

        // Assert
        status.Warnings.Should().Contain(w => w.WarningCode == "REVERIFICATION_DUE");
        status.IsReVerificationDue.Should().BeTrue();
    }

    [Fact]
    public async Task GetComplianceStatusAsync_IncludesGdpQualificationWarning_WhenWholesaler()
    {
        // Arrange
        var customer = CreateValidCustomer();
        customer.ApprovalStatus = ApprovalStatus.Approved;
        customer.BusinessCategory = BusinessCategory.WholesalerEU;
        customer.GdpQualificationStatus = GdpQualificationStatus.Pending;

        _customerRepoMock.Setup(r => r.GetByAccountAsync("CUST-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        var status = await _service.GetComplianceStatusAsync("CUST-001", "nlpd");

        // Assert
        status.Warnings.Should().Contain(w => w.WarningCode == "GDP_NOT_QUALIFIED");
    }

    #endregion

    #region SearchByNameAsync Tests

    [Fact]
    public async Task SearchByNameAsync_ReturnsResults()
    {
        // Arrange
        var customers = new List<Customer> { CreateValidCustomer() };
        _customerRepoMock.Setup(r => r.SearchByNameAsync("Test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        // Act
        var result = await _service.SearchByNameAsync("Test");

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region Helper Methods

    private static Customer CreateValidCustomer(string customerAccount = "CUST-001")
    {
        return new Customer
        {
            CustomerAccount = customerAccount,
            DataAreaId = "nlpd",
            OrganizationName = "Test Pharmacy",
            AddressCountryRegionId = "NLD",
            ComplianceExtensionId = Guid.NewGuid(),
            BusinessCategory = BusinessCategory.CommunityPharmacy,
            ApprovalStatus = ApprovalStatus.Approved,
            GdpQualificationStatus = GdpQualificationStatus.NotRequired,
            OnboardingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
            NextReVerificationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6))
        };
    }

    #endregion
}
