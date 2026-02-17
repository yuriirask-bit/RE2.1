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
/// T288: Tests for GDP Change Control API controller per US12 (FR-051).
/// </summary>
public class GdpChangeControlControllerTests
{
    private readonly Mock<IGdpChangeRepository> _mockChangeRepo;
    private readonly Mock<ILogger<GdpChangeControlController>> _mockLogger;
    private readonly GdpChangeControlController _controller;

    public GdpChangeControlControllerTests()
    {
        _mockChangeRepo = new Mock<IGdpChangeRepository>();
        _mockLogger = new Mock<ILogger<GdpChangeControlController>>();

        _controller = new GdpChangeControlController(_mockChangeRepo.Object, _mockLogger.Object);
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    }

    [Fact]
    public async Task GetChangeRecords_ReturnsOk_WithList()
    {
        var records = new List<GdpChangeRecord> { CreateTestChange() };
        _mockChangeRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(records);

        var result = await _controller.GetChangeRecords();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<GdpChangeRecordResponseDto>>().Subject;
        response.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetChangeRecord_WhenFound_ReturnsOk()
    {
        var id = Guid.NewGuid();
        _mockChangeRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(CreateTestChange(id));

        var result = await _controller.GetChangeRecord(id);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetChangeRecord_WhenNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _mockChangeRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((GdpChangeRecord?)null);

        var result = await _controller.GetChangeRecord(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetPendingChanges_ReturnsOk_WithPendingList()
    {
        var records = new List<GdpChangeRecord> { CreateTestChange() };
        _mockChangeRepo.Setup(r => r.GetPendingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(records);

        var result = await _controller.GetPendingChanges();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<GdpChangeRecordResponseDto>>().Subject;
        response.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateChangeRecord_WithValidRequest_ReturnsCreated()
    {
        _mockChangeRepo.Setup(r => r.CreateAsync(It.IsAny<GdpChangeRecord>(), It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());

        var request = new CreateChangeRecordRequestDto
        {
            ChangeNumber = "CHG-2026-001",
            ChangeType = GdpChangeType.NewWarehouse,
            Description = "Adding new warehouse"
        };

        var result = await _controller.CreateChangeRecord(request);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateChangeRecord_WithMissingDescription_ReturnsBadRequest()
    {
        var request = new CreateChangeRecordRequestDto
        {
            ChangeNumber = "CHG-2026-001",
            Description = ""
        };

        var result = await _controller.CreateChangeRecord(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ApproveChange_WhenFound_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _mockChangeRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(CreateTestChange(id));

        var result = await _controller.ApproveChange(id, new ApproveRejectRequest { UserId = Guid.NewGuid() });

        result.Should().BeOfType<NoContentResult>();
        _mockChangeRepo.Verify(r => r.ApproveAsync(id, It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveChange_WhenNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _mockChangeRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((GdpChangeRecord?)null);

        var result = await _controller.ApproveChange(id, new ApproveRejectRequest { UserId = Guid.NewGuid() });

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RejectChange_WhenFound_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _mockChangeRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(CreateTestChange(id));

        var result = await _controller.RejectChange(id, new ApproveRejectRequest { UserId = Guid.NewGuid() });

        result.Should().BeOfType<NoContentResult>();
        _mockChangeRepo.Verify(r => r.RejectAsync(id, It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static GdpChangeRecord CreateTestChange(Guid? id = null) => new()
    {
        ChangeRecordId = id ?? Guid.NewGuid(),
        ChangeNumber = "CHG-2026-001",
        ChangeType = GdpChangeType.NewWarehouse,
        Description = "Adding new warehouse",
        ApprovalStatus = ChangeApprovalStatus.Pending,
        CreatedDate = DateTime.UtcNow,
        ModifiedDate = DateTime.UtcNow
    };
}
