using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RE2.ComplianceApi.Controllers.V1;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceApi.Tests.Controllers.V1;

/// <summary>
/// T288: Tests for GDP SOPs API controller per US12 (FR-049).
/// </summary>
public class GdpSopsControllerTests
{
    private readonly Mock<IGdpComplianceService> _mockGdpService;
    private readonly Mock<IGdpSopRepository> _mockSopRepo;
    private readonly Mock<ILogger<GdpSopsController>> _mockLogger;
    private readonly GdpSopsController _controller;

    public GdpSopsControllerTests()
    {
        _mockGdpService = new Mock<IGdpComplianceService>();
        _mockSopRepo = new Mock<IGdpSopRepository>();
        _mockLogger = new Mock<ILogger<GdpSopsController>>();

        _controller = new GdpSopsController(_mockGdpService.Object, _mockSopRepo.Object, _mockLogger.Object);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [Fact]
    public async Task GetSops_ReturnsOk_WithList()
    {
        var sops = new List<GdpSop> { CreateTestSop() };
        _mockSopRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sops);

        var result = await _controller.GetSops();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<GdpSopResponseDto>>().Subject;
        response.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetSop_WhenFound_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _mockSopRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(CreateTestSop(id));

        var result = await _controller.GetSop(id);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<GdpSopResponseDto>();
    }

    [Fact]
    public async Task GetSop_WhenNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _mockSopRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((GdpSop?)null);

        var result = await _controller.GetSop(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateSop_WithValidRequest_ReturnsCreated()
    {
        _mockSopRepo.Setup(r => r.CreateAsync(It.IsAny<GdpSop>(), It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());

        var request = new CreateGdpSopRequestDto
        {
            SopNumber = "SOP-GDP-001",
            Title = "Returns Handling",
            Category = GdpSopCategory.Returns,
            Version = "1.0",
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30)
        };

        var result = await _controller.CreateSop(request);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateSop_WithMissingTitle_ReturnsBadRequest()
    {
        var request = new CreateGdpSopRequestDto
        {
            SopNumber = "SOP-GDP-001",
            Title = "",
            Version = "1.0"
        };

        var result = await _controller.CreateSop(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteSop_WhenFound_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _mockSopRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(CreateTestSop(id));

        var result = await _controller.DeleteSop(id);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task LinkSopToSite_ReturnsNoContent()
    {
        var sopId = Guid.NewGuid();
        var siteId = Guid.NewGuid();

        var result = await _controller.LinkSopToSite(sopId, siteId);

        result.Should().BeOfType<NoContentResult>();
        _mockSopRepo.Verify(r => r.LinkSopToSiteAsync(siteId, sopId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static GdpSop CreateTestSop(Guid? id = null) => new()
    {
        SopId = id ?? Guid.NewGuid(),
        SopNumber = "SOP-GDP-001",
        Title = "Returns Handling",
        Category = GdpSopCategory.Returns,
        Version = "1.0",
        EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
        IsActive = true
    };
}
