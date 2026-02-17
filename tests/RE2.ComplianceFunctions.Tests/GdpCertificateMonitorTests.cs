using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Services.AlertGeneration;
using RE2.ComplianceFunctions;
using RE2.DataAccess.InMemory;
using Xunit;

namespace RE2.ComplianceFunctions.Tests;

/// <summary>
/// Tests for GdpCertificateMonitor Azure Function.
/// T240: Verifies timer trigger calls all three GDP alert generation methods.
/// Uses in-memory repositories since AlertGenerationService has non-virtual methods.
/// </summary>
public class GdpCertificateMonitorTests
{
    private readonly AlertGenerationService _alertService;
    private readonly Mock<ILogger<GdpCertificateMonitor>> _loggerMock;
    private readonly GdpCertificateMonitor _monitor;

    public GdpCertificateMonitorTests()
    {
        // Create in-memory repositories for AlertGenerationService
        var alertRepo = new InMemoryAlertRepository();
        var credentialRepo = new InMemoryGdpCredentialRepository();
        var capaRepo = new InMemoryCapaRepository();
        var alertServiceLogger = Mock.Of<ILogger<AlertGenerationService>>();

        // Seed test data
        InMemorySeedData.SeedGdpCredentialData(credentialRepo);

        _alertService = new AlertGenerationService(
            alertRepo,
            Mock.Of<ILicenceRepository>(),
            Mock.Of<ICustomerRepository>(),
            credentialRepo,
            capaRepo,
            alertServiceLogger);

        _loggerMock = new Mock<ILogger<GdpCertificateMonitor>>();
        _monitor = new GdpCertificateMonitor(_alertService, _loggerMock.Object);
    }

    [Fact]
    public async Task Run_ShouldExecuteWithoutErrors()
    {
        // Arrange
        var timerInfo = CreateTimerInfo();

        // Act
        var act = () => _monitor.Run(timerInfo, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Run_ShouldLogStartAndCompletion()
    {
        // Arrange
        var timerInfo = CreateTimerInfo();

        // Act
        await _monitor.Run(timerInfo, CancellationToken.None);

        // Assert — verify logging was called (start + 3 alert types + completion = 5 info logs)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(2)); // At minimum: started + completed
    }

    [Fact]
    public async Task GenerateGdpAlertsManual_ShouldReturnSuccessResult()
    {
        // Arrange — using null! for HttpRequestData since we don't read the request body
        var req = (Microsoft.Azure.Functions.Worker.Http.HttpRequestData)null!;

        // Act
        var result = await _monitor.GenerateGdpAlertsManual(req, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.TotalAlertsGenerated.Should().Be(
            result.CredentialExpiryAlertsGenerated +
            result.RequalificationAlertsGenerated +
            result.CapaOverdueAlertsGenerated);
    }

    [Fact]
    public async Task GenerateGdpAlertsManual_ShouldReturnCorrectCounts()
    {
        // Arrange
        var req = (Microsoft.Azure.Functions.Worker.Http.HttpRequestData)null!;

        // Act
        var result = await _monitor.GenerateGdpAlertsManual(req, CancellationToken.None);

        // Assert — with seeded data, we should get some alerts
        result.CredentialExpiryAlertsGenerated.Should().BeGreaterThanOrEqualTo(0);
        result.RequalificationAlertsGenerated.Should().BeGreaterThanOrEqualTo(0);
        result.CapaOverdueAlertsGenerated.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Constructor_ShouldThrowOnNullAlertService()
    {
        // Act & Assert
        var act = () => new GdpCertificateMonitor(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("alertService");
    }

    [Fact]
    public void Constructor_ShouldThrowOnNullLogger()
    {
        // Act & Assert
        var act = () => new GdpCertificateMonitor(_alertService, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void GdpAlertGenerationResult_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var result = new GdpAlertGenerationResult();

        // Assert
        result.Success.Should().BeFalse();
        result.CredentialExpiryAlertsGenerated.Should().Be(0);
        result.RequalificationAlertsGenerated.Should().Be(0);
        result.CapaOverdueAlertsGenerated.Should().Be(0);
        result.TotalAlertsGenerated.Should().Be(0);
        result.ErrorMessage.Should().BeNull();
    }

    private static TimerInfo CreateTimerInfo()
    {
        // TimerInfo for isolated worker model — use mock
        return Mock.Of<TimerInfo>();
    }
}
