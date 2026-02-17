using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RE2.ComplianceApi.Controllers.V1;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Tests.Controllers.V1;

/// <summary>
/// T263: Tests for GDP Operations API controller per US11 (FR-046, FR-047, FR-048).
/// </summary>
public class GdpOperationsControllerTests
{
    private readonly Mock<IGdpOperationalService> _mockService;
    private readonly Mock<ILogger<GdpOperationsController>> _mockLogger;
    private readonly GdpOperationsController _controller;

    public GdpOperationsControllerTests()
    {
        _mockService = new Mock<IGdpOperationalService>();
        _mockLogger = new Mock<ILogger<GdpOperationsController>>();

        _controller = new GdpOperationsController(_mockService.Object, _mockLogger.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region ValidateSiteAssignment

    [Fact]
    public async Task ValidateSiteAssignment_WhenAllowed_ReturnsOkWithAllowed()
    {
        _mockService.Setup(s => s.ValidateSiteAssignmentAsync("WH01", "phr", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Site is GDP-active with valid WDA coverage."));

        var result = await _controller.ValidateSiteAssignment(new SiteAssignmentValidationRequest { WarehouseId = "WH01", DataAreaId = "phr" });

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<SiteAssignmentValidationResponse>().Subject;
        response.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateSiteAssignment_WhenBlocked_ReturnsOkWithBlocked()
    {
        _mockService.Setup(s => s.ValidateSiteAssignmentAsync("WH99", "phr", It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Not GDP-configured."));

        var result = await _controller.ValidateSiteAssignment(new SiteAssignmentValidationRequest { WarehouseId = "WH99", DataAreaId = "phr" });

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<SiteAssignmentValidationResponse>().Subject;
        response.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateSiteAssignment_WithMissingWarehouseId_ReturnsBadRequest()
    {
        var result = await _controller.ValidateSiteAssignment(new SiteAssignmentValidationRequest { WarehouseId = "", DataAreaId = "phr" });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region ValidateProviderAssignment

    [Fact]
    public async Task ValidateProviderAssignment_WhenAllowed_ReturnsOkWithAllowed()
    {
        var providerId = Guid.NewGuid();
        _mockService.Setup(s => s.ValidateProviderAssignmentAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, "Provider is GDP-qualified."));

        var result = await _controller.ValidateProviderAssignment(new ProviderAssignmentValidationRequest { ProviderId = providerId });

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ProviderAssignmentValidationResponse>().Subject;
        response.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateProviderAssignment_WhenBlocked_ReturnsOkWithBlocked()
    {
        var providerId = Guid.NewGuid();
        _mockService.Setup(s => s.ValidateProviderAssignmentAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "Not qualified."));

        var result = await _controller.ValidateProviderAssignment(new ProviderAssignmentValidationRequest { ProviderId = providerId });

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ProviderAssignmentValidationResponse>().Subject;
        response.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateProviderAssignment_WithEmptyProviderId_ReturnsBadRequest()
    {
        var result = await _controller.ValidateProviderAssignment(new ProviderAssignmentValidationRequest { ProviderId = Guid.Empty });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetApprovedProviders

    [Fact]
    public async Task GetApprovedProviders_ReturnsOk_WithProviderList()
    {
        var providers = new List<GdpServiceProvider>
        {
            new() { ProviderId = Guid.NewGuid(), ProviderName = "Test Provider" }
        };
        _mockService.Setup(s => s.GetApprovedProvidersAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        var result = await _controller.GetApprovedProviders();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<ApprovedProviderDto>>().Subject;
        response.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetApprovedProviders_WithTempFilter_PassesFilterToService()
    {
        _mockService.Setup(s => s.GetApprovedProvidersAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<GdpServiceProvider>());

        var result = await _controller.GetApprovedProviders(tempControlled: true);

        result.Should().BeOfType<OkObjectResult>();
        _mockService.Verify(s => s.GetApprovedProvidersAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Equipment CRUD

    [Fact]
    public async Task GetEquipment_ReturnsOk_WithList()
    {
        var equipment = new List<GdpEquipmentQualification> { CreateTestEquipment() };
        _mockService.Setup(s => s.GetAllEquipmentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment);

        var result = await _controller.GetEquipment();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<EquipmentQualificationResponseDto>>().Subject;
        response.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetEquipmentById_WhenFound_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var equipment = CreateTestEquipment(id);
        _mockService.Setup(s => s.GetEquipmentAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(equipment);

        var result = await _controller.GetEquipmentById(id);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<EquipmentQualificationResponseDto>().Subject;
        response.EquipmentQualificationId.Should().Be(id);
    }

    [Fact]
    public async Task GetEquipmentById_WhenNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _mockService.Setup(s => s.GetEquipmentAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpEquipmentQualification?)null);

        var result = await _controller.GetEquipmentById(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateEquipment_WithValidRequest_ReturnsCreated()
    {
        var id = Guid.NewGuid();
        _mockService.Setup(s => s.CreateEquipmentAsync(It.IsAny<GdpEquipmentQualification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((id, ValidationResult.Success()));

        var request = new CreateEquipmentRequestDto
        {
            EquipmentName = "Test Vehicle",
            EquipmentType = GdpEquipmentType.TemperatureControlledVehicle,
            QualificationDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
            QualifiedBy = "Test Engineer"
        };

        var result = await _controller.CreateEquipment(request);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateEquipment_WithInvalidRequest_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.CreateEquipmentAsync(It.IsAny<GdpEquipmentQualification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, ValidationResult.Failure(new[] { new ValidationViolation { ErrorCode = "VALIDATION_ERROR", Message = "Name required" } })));

        var request = new CreateEquipmentRequestDto();
        var result = await _controller.CreateEquipment(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteEquipment_WhenFound_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _mockService.Setup(s => s.DeleteEquipmentAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        var result = await _controller.DeleteEquipment(id);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteEquipment_WhenNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _mockService.Setup(s => s.DeleteEquipmentAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(new[] { new ValidationViolation { ErrorCode = "NOT_FOUND", Message = "Not found" } }));

        var result = await _controller.DeleteEquipment(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    private static GdpEquipmentQualification CreateTestEquipment(Guid? id = null) => new()
    {
        EquipmentQualificationId = id ?? Guid.NewGuid(),
        EquipmentName = "Test Vehicle TRK-001",
        EquipmentType = GdpEquipmentType.TemperatureControlledVehicle,
        QualificationDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
        RequalificationDueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(11),
        QualificationStatus = GdpQualificationStatusType.Qualified,
        QualifiedBy = "Test Engineer",
        CreatedDate = DateTime.UtcNow,
        ModifiedDate = DateTime.UtcNow
    };
}
