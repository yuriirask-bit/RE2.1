using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.ComplianceCore.Services.LicenceValidation;

namespace RE2.ComplianceCore.Tests.Services;

public class CachedLicenceServiceTests
{
    private readonly Mock<ILicenceService> _innerMock = new();
    private readonly Mock<ICacheService> _cacheMock = new();
    private readonly Mock<ILogger<CachedLicenceService>> _loggerMock = new();
    private readonly CachedLicenceService _sut;

    public CachedLicenceServiceTests()
    {
        _sut = new CachedLicenceService(_innerMock.Object, _cacheMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_CacheHit_ReturnsFromCache()
    {
        var id = Guid.NewGuid();
        var licence = new Licence { LicenceId = id, LicenceNumber = "LIC-001", HolderType = "Company", IssuingAuthority = "Test", Status = "Valid" };
        _cacheMock.Setup(c => c.GetAsync<Licence>($"re2:licence:id:{id}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);

        var result = await _sut.GetByIdAsync(id);

        result.Should().BeSameAs(licence);
        _innerMock.Verify(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_CacheMiss_CallsInnerAndCaches()
    {
        var id = Guid.NewGuid();
        var licence = new Licence { LicenceId = id, LicenceNumber = "LIC-001", HolderType = "Company", IssuingAuthority = "Test", Status = "Valid" };
        _cacheMock.Setup(c => c.GetAsync<Licence>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licence?)null);
        _innerMock.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);

        var result = await _sut.GetByIdAsync(id);

        result.Should().BeSameAs(licence);
        _cacheMock.Verify(c => c.SetAsync(
            $"re2:licence:id:{id}",
            licence,
            TimeSpan.FromMinutes(30),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_InnerReturnsNull_DoesNotCache()
    {
        var id = Guid.NewGuid();
        _cacheMock.Setup(c => c.GetAsync<Licence>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licence?)null);
        _innerMock.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licence?)null);

        var result = await _sut.GetByIdAsync(id);

        result.Should().BeNull();
        _cacheMock.Verify(c => c.SetAsync(
            It.IsAny<string>(), It.IsAny<Licence>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByLicenceNumberAsync_CacheHit_ReturnsFromCache()
    {
        var licence = new Licence { LicenceId = Guid.NewGuid(), LicenceNumber = "LIC-001", HolderType = "Company", IssuingAuthority = "Test", Status = "Valid" };
        _cacheMock.Setup(c => c.GetAsync<Licence>("re2:licence:num:LIC-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);

        var result = await _sut.GetByLicenceNumberAsync("LIC-001");

        result.Should().BeSameAs(licence);
        _innerMock.Verify(s => s.GetByLicenceNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByHolderAsync_CacheMiss_CallsInnerAndCaches()
    {
        var holderId = Guid.NewGuid();
        var licences = new List<Licence> { new() { LicenceId = Guid.NewGuid(), LicenceNumber = "LIC-X", HolderType = "Company", IssuingAuthority = "Test", Status = "Valid" } };
        _cacheMock.Setup(c => c.GetAsync<List<Licence>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<Licence>?)null);
        _innerMock.Setup(s => s.GetByHolderAsync(holderId, "Company", It.IsAny<CancellationToken>()))
            .ReturnsAsync(licences);

        var result = await _sut.GetByHolderAsync(holderId, "Company");

        result.Should().BeEquivalentTo(licences);
        _cacheMock.Verify(c => c.SetAsync(
            $"re2:licence:holder:{holderId}:Company",
            It.IsAny<List<Licence>>(),
            TimeSpan.FromMinutes(30),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidatesCache_OnSuccess()
    {
        var licence = new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = "LIC-001",
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "Test",
            Status = "Valid"
        };
        _innerMock.Setup(s => s.CreateAsync(licence, It.IsAny<CancellationToken>()))
            .ReturnsAsync((licence.LicenceId, ValidationResult.Success()));

        await _sut.CreateAsync(licence);

        _cacheMock.Verify(c => c.RemoveAsync($"re2:licence:id:{licence.LicenceId}", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync($"re2:licence:num:{licence.LicenceNumber}", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefixAsync($"re2:licence:holder:{licence.HolderId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DoesNotInvalidateCache_OnFailure()
    {
        var licence = new Licence { LicenceId = Guid.NewGuid(), LicenceNumber = "LIC-001", HolderType = "Company", IssuingAuthority = "Test", Status = "Valid" };
        var failedResult = ValidationResult.Failure("TEST_ERROR", "error");
        _innerMock.Setup(s => s.CreateAsync(licence, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Guid?)null, failedResult));

        await _sut.CreateAsync(licence);

        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_InvalidatesCache_OnSuccess()
    {
        var licence = new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = "LIC-001",
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "Test",
            Status = "Valid"
        };
        _innerMock.Setup(s => s.UpdateAsync(licence, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        await _sut.UpdateAsync(licence);

        _cacheMock.Verify(c => c.RemoveAsync($"re2:licence:id:{licence.LicenceId}", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync($"re2:licence:num:{licence.LicenceNumber}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_InvalidatesCache_OnSuccess()
    {
        var id = Guid.NewGuid();
        var holderId = Guid.NewGuid();
        var licence = new Licence { LicenceId = id, LicenceNumber = "LIC-001", HolderType = "Company", HolderId = holderId, IssuingAuthority = "Test", Status = "Valid" };
        _innerMock.Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);
        _innerMock.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        await _sut.DeleteAsync(id);

        _cacheMock.Verify(c => c.RemoveAsync($"re2:licence:id:{id}", It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefixAsync($"re2:licence:holder:{holderId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToInner()
    {
        var licences = new List<Licence> { new() { LicenceId = Guid.NewGuid(), LicenceNumber = "LIC-X", HolderType = "Company", IssuingAuthority = "Test", Status = "Valid" } };
        _innerMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(licences);

        var result = await _sut.GetAllAsync();

        result.Should().BeSameAs(licences);
    }
}
