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
/// T184: Integration tests for GDP Sites API endpoints.
/// Tests GdpSitesController with mocked dependencies.
/// </summary>
public class GdpSitesControllerTests
{
    private readonly Mock<IGdpComplianceService> _mockGdpService;
    private readonly Mock<ILogger<GdpSitesController>> _mockLogger;
    private readonly GdpSitesController _controller;

    public GdpSitesControllerTests()
    {
        _mockGdpService = new Mock<IGdpComplianceService>();
        _mockLogger = new Mock<ILogger<GdpSitesController>>();

        _controller = new GdpSitesController(_mockGdpService.Object, _mockLogger.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GET /api/v1/gdpsites/warehouses Tests

    [Fact]
    public async Task GetWarehouses_ReturnsOk_WithWarehouseList()
    {
        // Arrange
        var warehouses = new List<GdpSite>
        {
            CreateTestWarehouse("WH-001", "Amsterdam Warehouse"),
            CreateTestWarehouse("WH-002", "Rotterdam Warehouse")
        };

        _mockGdpService
            .Setup(s => s.GetAllWarehousesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouses);

        // Act
        var result = await _controller.GetWarehouses();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<WarehouseResponseDto>>().Subject;
        response.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetWarehouse_ReturnsOk_WhenWarehouseExists()
    {
        // Arrange
        var warehouse = CreateTestWarehouse("WH-001", "Amsterdam Warehouse");
        _mockGdpService
            .Setup(s => s.GetWarehouseAsync("WH-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);

        // Act
        var result = await _controller.GetWarehouse("WH-001", "nlpd");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<WarehouseResponseDto>().Subject;
        response.WarehouseId.Should().Be("WH-001");
    }

    [Fact]
    public async Task GetWarehouse_ReturnsNotFound_WhenWarehouseDoesNotExist()
    {
        // Arrange
        _mockGdpService
            .Setup(s => s.GetWarehouseAsync("WH-NONE", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpSite?)null);

        // Act
        var result = await _controller.GetWarehouse("WH-NONE", "nlpd");

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.NOT_FOUND);
    }

    #endregion

    #region GET /api/v1/gdpsites Tests

    [Fact]
    public async Task GetGdpSites_ReturnsOk_WithConfiguredSites()
    {
        // Arrange
        var sites = new List<GdpSite>
        {
            CreateTestGdpSite("WH-001", GdpSiteType.Warehouse)
        };

        _mockGdpService
            .Setup(s => s.GetAllGdpSitesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sites);

        // Act
        var result = await _controller.GetGdpSites();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<GdpSiteResponseDto>>().Subject;
        response.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetGdpSite_ReturnsNotFound_WhenNotConfigured()
    {
        // Arrange
        _mockGdpService
            .Setup(s => s.GetGdpSiteAsync("WH-NONE", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpSite?)null);

        // Act
        var result = await _controller.GetGdpSite("WH-NONE", "nlpd");

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region POST /api/v1/gdpsites Tests

    [Fact]
    public async Task ConfigureGdp_ReturnsCreated_WithValidRequest()
    {
        // Arrange
        var request = new ConfigureGdpRequestDto
        {
            WarehouseId = "WH-001",
            DataAreaId = "nlpd",
            GdpSiteType = "Warehouse",
            PermittedActivities = (int)(GdpSiteActivity.StorageOver72h | GdpSiteActivity.TemperatureControlled),
            IsGdpActive = true
        };

        var extensionId = Guid.NewGuid();
        _mockGdpService
            .Setup(s => s.ConfigureGdpAsync(It.IsAny<GdpSite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((extensionId, ValidationResult.Success()));

        _mockGdpService
            .Setup(s => s.GetGdpSiteAsync("WH-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestGdpSite("WH-001", GdpSiteType.Warehouse));

        // Act
        var result = await _controller.ConfigureGdp(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.Value.Should().BeOfType<GdpSiteResponseDto>();
    }

    [Fact]
    public async Task ConfigureGdp_ReturnsBadRequest_WithInvalidData()
    {
        // Arrange
        var request = new ConfigureGdpRequestDto
        {
            WarehouseId = "WH-001",
            DataAreaId = "nlpd",
            GdpSiteType = "Warehouse",
            PermittedActivities = 0
        };

        _mockGdpService
            .Setup(s => s.ConfigureGdpAsync(It.IsAny<GdpSite>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, ValidationResult.Failure(ErrorCodes.VALIDATION_ERROR, "At least one permitted activity must be selected")));

        // Act
        var result = await _controller.ConfigureGdp(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    #endregion

    #region DELETE /api/v1/gdpsites/{warehouseId} Tests

    [Fact]
    public async Task RemoveGdpConfig_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        _mockGdpService
            .Setup(s => s.RemoveGdpConfigAsync("WH-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var result = await _controller.RemoveGdpConfig("WH-001", "nlpd");

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveGdpConfig_ReturnsNotFound_WhenNotConfigured()
    {
        // Arrange
        _mockGdpService
            .Setup(s => s.RemoveGdpConfigAsync("WH-NONE", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(ErrorCodes.NOT_FOUND, "GDP configuration not found"));

        // Act
        var result = await _controller.RemoveGdpConfig("WH-NONE", "nlpd");

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region WDA Coverage Tests

    [Fact]
    public async Task GetWdaCoverage_ReturnsOk_WithCoverageList()
    {
        // Arrange
        var coverages = new List<GdpSiteWdaCoverage>
        {
            new()
            {
                CoverageId = Guid.NewGuid(),
                WarehouseId = "WH-001",
                DataAreaId = "nlpd",
                LicenceId = Guid.NewGuid(),
                EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1))
            }
        };

        _mockGdpService
            .Setup(s => s.GetWdaCoverageAsync("WH-001", "nlpd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(coverages);

        // Act
        var result = await _controller.GetWdaCoverage("WH-001", "nlpd");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<WdaCoverageResponseDto>>().Subject;
        response.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddWdaCoverage_ReturnsBadRequest_WhenNonWdaLicence()
    {
        // Arrange - FR-033: only WDA licences can be used for coverage
        var request = new CreateWdaCoverageRequestDto
        {
            DataAreaId = "nlpd",
            LicenceId = Guid.NewGuid(),
            EffectiveDate = "2024-01-01"
        };

        _mockGdpService
            .Setup(s => s.AddWdaCoverageAsync(It.IsAny<GdpSiteWdaCoverage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, ValidationResult.Failure(ErrorCodes.VALIDATION_ERROR,
                "Licence is not a Wholesale Distribution Authorisation (WDA)")));

        // Act
        var result = await _controller.AddWdaCoverage("WH-001", request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.Message.Should().Contain("WDA");
    }

    [Fact]
    public async Task AddWdaCoverage_ReturnsCreated_WithValidWdaLicence()
    {
        // Arrange
        var coverageId = Guid.NewGuid();
        var request = new CreateWdaCoverageRequestDto
        {
            DataAreaId = "nlpd",
            LicenceId = Guid.NewGuid(),
            EffectiveDate = "2024-01-01"
        };

        _mockGdpService
            .Setup(s => s.AddWdaCoverageAsync(It.IsAny<GdpSiteWdaCoverage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((coverageId, ValidationResult.Success()));

        // Act
        var result = await _controller.AddWdaCoverage("WH-001", request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task RemoveWdaCoverage_ReturnsNoContent()
    {
        // Arrange
        var coverageId = Guid.NewGuid();
        _mockGdpService
            .Setup(s => s.RemoveWdaCoverageAsync(coverageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var result = await _controller.RemoveWdaCoverage("WH-001", coverageId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    #endregion

    #region Test Helpers

    private static GdpSite CreateTestWarehouse(string warehouseId, string name) => new()
    {
        WarehouseId = warehouseId,
        WarehouseName = name,
        OperationalSiteId = "SITE-001",
        OperationalSiteName = "Test Site",
        DataAreaId = "nlpd",
        WarehouseType = "Standard",
        City = "Amsterdam",
        CountryRegionId = "NL",
        FormattedAddress = "Test Street 1, Amsterdam"
    };

    private static GdpSite CreateTestGdpSite(string warehouseId, GdpSiteType siteType) => new()
    {
        WarehouseId = warehouseId,
        WarehouseName = "Test Warehouse",
        OperationalSiteId = "SITE-001",
        OperationalSiteName = "Test Site",
        DataAreaId = "nlpd",
        WarehouseType = "Standard",
        City = "Amsterdam",
        CountryRegionId = "NL",
        FormattedAddress = "Test Street 1, Amsterdam",
        GdpExtensionId = Guid.NewGuid(),
        GdpSiteType = siteType,
        PermittedActivities = GdpSiteActivity.StorageOver72h | GdpSiteActivity.TemperatureControlled,
        IsGdpActive = true,
        CreatedDate = DateTime.UtcNow,
        ModifiedDate = DateTime.UtcNow
    };

    #endregion
}
