using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.ComplianceCore.Services.CustomerQualification;

namespace RE2.ComplianceCore.Tests.Services;

public class CachedCustomerServiceTests
{
    private readonly Mock<ICustomerService> _innerMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly Mock<ILogger<CachedCustomerService>> _loggerMock = new();
    private readonly CachedCustomerService _sut;

    public CachedCustomerServiceTests()
    {
        _sut = new CachedCustomerService(_innerMock.Object, _cacheMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetByAccountAsync_CacheHit_ReturnsFromCache()
    {
        var customer = new Customer { CustomerAccount = "CUST001", DataAreaId = "dat" };
        _cacheMock.Setup(c => c.GetAsync<Customer>("re2:customer:acct:CUST001:dat", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _sut.GetByAccountAsync("CUST001", "dat");

        result.Should().BeSameAs(customer);
        _innerMock.Verify(s => s.GetByAccountAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByAccountAsync_CacheMiss_CallsInnerAndCaches()
    {
        var customer = new Customer { CustomerAccount = "CUST001", DataAreaId = "dat" };
        _cacheMock.Setup(c => c.GetAsync<Customer>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);
        _innerMock.Setup(s => s.GetByAccountAsync("CUST001", "dat", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _sut.GetByAccountAsync("CUST001", "dat");

        result.Should().BeSameAs(customer);
        _cacheMock.Verify(c => c.SetAsync(
            "re2:customer:acct:CUST001:dat",
            customer,
            TimeSpan.FromMinutes(30),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByAccountAsync_InnerReturnsNull_DoesNotCache()
    {
        _cacheMock.Setup(c => c.GetAsync<Customer>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);
        _innerMock.Setup(s => s.GetByAccountAsync("CUST001", "dat", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var result = await _sut.GetByAccountAsync("CUST001", "dat");

        result.Should().BeNull();
        _cacheMock.Verify(c => c.SetAsync(
            It.IsAny<string>(), It.IsAny<Customer>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetComplianceStatusAsync_CacheHit_ReturnsFromCache()
    {
        var status = new CustomerComplianceStatus { CustomerAccount = "CUST001", CanTransact = true };
        _cacheMock.Setup(c => c.GetAsync<CustomerComplianceStatus>(
            "re2:customer:status:CUST001:dat", It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        var result = await _sut.GetComplianceStatusAsync("CUST001", "dat");

        result.Should().BeSameAs(status);
        _innerMock.Verify(s => s.GetComplianceStatusAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetComplianceStatusAsync_CacheMiss_CachesWithShortTtl()
    {
        var status = new CustomerComplianceStatus { CustomerAccount = "CUST001", CanTransact = true };
        _cacheMock.Setup(c => c.GetAsync<CustomerComplianceStatus>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerComplianceStatus?)null);
        _innerMock.Setup(s => s.GetComplianceStatusAsync("CUST001", "dat", It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        var result = await _sut.GetComplianceStatusAsync("CUST001", "dat");

        result.Should().BeSameAs(status);
        _cacheMock.Verify(c => c.SetAsync(
            "re2:customer:status:CUST001:dat",
            status,
            TimeSpan.FromMinutes(5),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfigureComplianceAsync_InvalidatesCache_OnSuccess()
    {
        var customer = new Customer { CustomerAccount = "CUST001", DataAreaId = "dat" };
        _innerMock.Setup(s => s.ConfigureComplianceAsync(customer, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid.NewGuid(), ValidationResult.Success()));

        await _sut.ConfigureComplianceAsync(customer);

        _cacheMock.Verify(c => c.RemoveAsync("re2:customer:acct:CUST001:dat", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("re2:customer:status:CUST001:dat", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfigureComplianceAsync_DoesNotInvalidate_OnFailure()
    {
        var customer = new Customer { CustomerAccount = "CUST001", DataAreaId = "dat" };
        var failedResult = ValidationResult.Failure("TEST_ERROR", "error");
        _innerMock.Setup(s => s.ConfigureComplianceAsync(customer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Guid?)null, failedResult));

        await _sut.ConfigureComplianceAsync(customer);

        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateComplianceAsync_InvalidatesCache_OnSuccess()
    {
        var customer = new Customer { CustomerAccount = "CUST001", DataAreaId = "dat" };
        _innerMock.Setup(s => s.UpdateComplianceAsync(customer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        await _sut.UpdateComplianceAsync(customer);

        _cacheMock.Verify(c => c.RemoveAsync("re2:customer:acct:CUST001:dat", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("re2:customer:status:CUST001:dat", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SuspendCustomerAsync_InvalidatesCache_OnSuccess()
    {
        _innerMock.Setup(s => s.SuspendCustomerAsync("CUST001", "dat", "reason", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        await _sut.SuspendCustomerAsync("CUST001", "dat", "reason");

        _cacheMock.Verify(c => c.RemoveAsync("re2:customer:acct:CUST001:dat", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("re2:customer:status:CUST001:dat", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReinstateCustomerAsync_InvalidatesCache_OnSuccess()
    {
        _innerMock.Setup(s => s.ReinstateCustomerAsync("CUST001", "dat", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        await _sut.ReinstateCustomerAsync("CUST001", "dat");

        _cacheMock.Verify(c => c.RemoveAsync("re2:customer:acct:CUST001:dat", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync("re2:customer:status:CUST001:dat", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToInner()
    {
        var customers = new List<Customer> { new() { CustomerAccount = "C1" } };
        _innerMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(customers);

        var result = await _sut.GetAllAsync();

        result.Should().BeSameAs(customers);
    }

    [Fact]
    public async Task SearchByNameAsync_DelegatesToInner()
    {
        var customers = new List<Customer> { new() { CustomerAccount = "C1" } };
        _innerMock.Setup(s => s.SearchByNameAsync("test", It.IsAny<CancellationToken>())).ReturnsAsync(customers);

        var result = await _sut.SearchByNameAsync("test");

        result.Should().BeSameAs(customers);
    }
}
