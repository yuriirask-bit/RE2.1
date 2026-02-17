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
/// T215: Integration tests for GDP Inspections and CAPA API endpoints.
/// Tests GdpInspectionsController with mocked dependencies.
/// </summary>
public class GdpInspectionsControllerTests
{
    private readonly Mock<IGdpComplianceService> _mockGdpService;
    private readonly Mock<ILogger<GdpInspectionsController>> _mockLogger;
    private readonly GdpInspectionsController _controller;

    public GdpInspectionsControllerTests()
    {
        _mockGdpService = new Mock<IGdpComplianceService>();
        _mockLogger = new Mock<ILogger<GdpInspectionsController>>();

        _controller = new GdpInspectionsController(_mockGdpService.Object, _mockLogger.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GET /api/v1/gdpinspections Tests

    [Fact]
    public async Task GetInspections_ReturnsOk_WithInspectionList()
    {
        // Arrange
        var inspections = new List<GdpInspection>
        {
            CreateTestInspection(Guid.NewGuid(), "IGJ Inspector"),
            CreateTestInspection(Guid.NewGuid(), "Internal QA")
        };

        _mockGdpService
            .Setup(s => s.GetAllInspectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(inspections);

        // Act
        var result = await _controller.GetInspections();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<GdpInspectionResponseDto>>().Subject;
        response.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetInspection_ReturnsOk_WhenFound()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var inspection = CreateTestInspection(inspectionId, "IGJ Inspector");

        _mockGdpService
            .Setup(s => s.GetInspectionAsync(inspectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inspection);

        // Act
        var result = await _controller.GetInspection(inspectionId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GdpInspectionResponseDto>().Subject;
        response.InspectionId.Should().Be(inspectionId);
    }

    [Fact]
    public async Task GetInspection_ReturnsNotFound_WhenNotExists()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        _mockGdpService
            .Setup(s => s.GetInspectionAsync(inspectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpInspection?)null);

        // Act
        var result = await _controller.GetInspection(inspectionId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetInspectionsBySite_ReturnsOk_WithFilteredList()
    {
        // Arrange
        var siteId = Guid.NewGuid();
        var inspections = new List<GdpInspection>
        {
            CreateTestInspection(Guid.NewGuid(), "IGJ", siteId)
        };

        _mockGdpService
            .Setup(s => s.GetInspectionsBySiteAsync(siteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inspections);

        // Act
        var result = await _controller.GetInspectionsBySite(siteId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<GdpInspectionResponseDto>>().Subject;
        response.Should().HaveCount(1);
    }

    #endregion

    #region POST /api/v1/gdpinspections Tests

    [Fact]
    public async Task CreateInspection_ReturnsCreated_WithValidRequest()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var request = new CreateInspectionRequestDto
        {
            InspectionDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
            InspectorName = "IGJ Inspector",
            InspectionType = GdpInspectionType.RegulatoryAuthority,
            SiteId = Guid.NewGuid(),
            FindingsSummary = "Routine inspection passed"
        };

        _mockGdpService
            .Setup(s => s.CreateInspectionAsync(It.IsAny<GdpInspection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((inspectionId, ValidationResult.Success()));

        _mockGdpService
            .Setup(s => s.GetInspectionAsync(inspectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestInspection(inspectionId, request.InspectorName));

        // Act
        var result = await _controller.CreateInspection(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task CreateInspection_ReturnsBadRequest_WithInvalidData()
    {
        // Arrange
        var request = new CreateInspectionRequestDto
        {
            InspectionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            InspectorName = "",
            InspectionType = GdpInspectionType.Internal,
            SiteId = Guid.Empty
        };

        _mockGdpService
            .Setup(s => s.CreateInspectionAsync(It.IsAny<GdpInspection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, ValidationResult.Failure(new[]
            {
                new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "InspectorName is required" }
            })));

        // Act
        var result = await _controller.CreateInspection(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Findings Tests

    [Fact]
    public async Task GetFindings_ReturnsOk_WithFindingList()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var findings = new List<GdpInspectionFinding>
        {
            CreateTestFinding(Guid.NewGuid(), inspectionId, FindingClassification.Critical),
            CreateTestFinding(Guid.NewGuid(), inspectionId, FindingClassification.Major)
        };

        _mockGdpService
            .Setup(s => s.GetFindingsAsync(inspectionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(findings);

        // Act
        var result = await _controller.GetFindings(inspectionId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<FindingResponseDto>>().Subject;
        response.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateFinding_ReturnsCreated_WithValidData()
    {
        // Arrange
        var inspectionId = Guid.NewGuid();
        var findingId = Guid.NewGuid();
        var request = new CreateFindingRequestDto
        {
            InspectionId = inspectionId,
            FindingDescription = "Temperature excursion detected",
            Classification = FindingClassification.Critical,
            FindingNumber = "F-001"
        };

        _mockGdpService
            .Setup(s => s.CreateFindingAsync(It.IsAny<GdpInspectionFinding>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((findingId, ValidationResult.Success()));

        _mockGdpService
            .Setup(s => s.GetFindingAsync(findingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestFinding(findingId, inspectionId, FindingClassification.Critical));

        // Act
        var result = await _controller.CreateFinding(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task DeleteFinding_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        var findingId = Guid.NewGuid();
        _mockGdpService
            .Setup(s => s.DeleteFindingAsync(findingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var result = await _controller.DeleteFinding(findingId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    #endregion

    #region CAPA Tests

    [Fact]
    public async Task GetCapas_ReturnsOk_WithCapaList()
    {
        // Arrange
        var capas = new List<Capa>
        {
            CreateTestCapa(Guid.NewGuid(), "CAPA-2025-001", CapaStatus.Open),
            CreateTestCapa(Guid.NewGuid(), "CAPA-2025-002", CapaStatus.Completed)
        };

        _mockGdpService
            .Setup(s => s.GetAllCapasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(capas);

        // Act
        var result = await _controller.GetCapas();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<CapaResponseDto>>().Subject;
        response.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCapa_ReturnsOk_WhenFound()
    {
        // Arrange
        var capaId = Guid.NewGuid();
        var capa = CreateTestCapa(capaId, "CAPA-2025-001", CapaStatus.Open);

        _mockGdpService
            .Setup(s => s.GetCapaAsync(capaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(capa);

        // Act
        var result = await _controller.GetCapa(capaId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<CapaResponseDto>().Subject;
        response.CapaNumber.Should().Be("CAPA-2025-001");
    }

    [Fact]
    public async Task GetCapa_ReturnsNotFound_WhenNotExists()
    {
        // Arrange
        var capaId = Guid.NewGuid();
        _mockGdpService
            .Setup(s => s.GetCapaAsync(capaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Capa?)null);

        // Act
        var result = await _controller.GetCapa(capaId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetOverdueCapas_ReturnsOk_WithOverdueList()
    {
        // Arrange
        var overdueCapas = new List<Capa>
        {
            CreateTestCapa(Guid.NewGuid(), "CAPA-2025-002", CapaStatus.Open)
        };

        _mockGdpService
            .Setup(s => s.GetOverdueCapasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(overdueCapas);

        // Act
        var result = await _controller.GetOverdueCapas();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<CapaResponseDto>>().Subject;
        response.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateCapa_ReturnsCreated_WithValidRequest()
    {
        // Arrange
        var capaId = Guid.NewGuid();
        var findingId = Guid.NewGuid();
        var request = new CreateCapaRequestDto
        {
            CapaNumber = "CAPA-2025-001",
            FindingId = findingId,
            Description = "Corrective action for temperature excursion",
            OwnerName = "Jan de Vries",
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(1)
        };

        _mockGdpService
            .Setup(s => s.CreateCapaAsync(It.IsAny<Capa>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((capaId, ValidationResult.Success()));

        _mockGdpService
            .Setup(s => s.GetCapaAsync(capaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestCapa(capaId, "CAPA-2025-001", CapaStatus.Open));

        // Act
        var result = await _controller.CreateCapa(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task CreateCapa_ReturnsBadRequest_WithInvalidData()
    {
        // Arrange
        var request = new CreateCapaRequestDto
        {
            CapaNumber = "",
            FindingId = Guid.Empty,
            Description = "",
            OwnerName = "",
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        _mockGdpService
            .Setup(s => s.CreateCapaAsync(It.IsAny<Capa>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, ValidationResult.Failure(new[]
            {
                new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "CapaNumber is required" }
            })));

        // Act
        var result = await _controller.CreateCapa(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CompleteCapa_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var capaId = Guid.NewGuid();
        var request = new CompleteCapaRequestDto
        {
            CompletionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            VerificationNotes = "Action verified effective"
        };

        _mockGdpService
            .Setup(s => s.CompleteCapaAsync(capaId, request.CompletionDate, request.VerificationNotes, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockGdpService
            .Setup(s => s.GetCapaAsync(capaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestCapa(capaId, "CAPA-2025-001", CapaStatus.Completed));

        // Act
        var result = await _controller.CompleteCapa(capaId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<CapaResponseDto>().Subject;
        response.Status.Should().Be(CapaStatus.Completed);
    }

    [Fact]
    public async Task CompleteCapa_ReturnsNotFound_WhenCapaNotExists()
    {
        // Arrange
        var capaId = Guid.NewGuid();
        var request = new CompleteCapaRequestDto
        {
            CompletionDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        _mockGdpService
            .Setup(s => s.CompleteCapaAsync(capaId, request.CompletionDate, request.VerificationNotes, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(new[]
            {
                new ValidationViolation { ErrorCode = ErrorCodes.NOT_FOUND, Message = "CAPA not found" }
            }));

        // Act
        var result = await _controller.CompleteCapa(capaId, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Test Helpers

    private static GdpInspection CreateTestInspection(Guid inspectionId, string inspectorName, Guid? siteId = null)
    {
        return new GdpInspection
        {
            InspectionId = inspectionId,
            InspectionDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7),
            InspectorName = inspectorName,
            InspectionType = GdpInspectionType.RegulatoryAuthority,
            SiteId = siteId ?? Guid.NewGuid(),
            FindingsSummary = "Test inspection findings",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }

    private static GdpInspectionFinding CreateTestFinding(Guid findingId, Guid inspectionId, FindingClassification classification)
    {
        return new GdpInspectionFinding
        {
            FindingId = findingId,
            InspectionId = inspectionId,
            FindingDescription = $"Test {classification} finding",
            Classification = classification,
            FindingNumber = $"F-{findingId.ToString()[..4]}"
        };
    }

    private static Capa CreateTestCapa(Guid capaId, string capaNumber, CapaStatus status)
    {
        return new Capa
        {
            CapaId = capaId,
            CapaNumber = capaNumber,
            FindingId = Guid.NewGuid(),
            Description = "Test CAPA description",
            OwnerName = "Test Owner",
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(1),
            CompletionDate = status == CapaStatus.Completed ? DateOnly.FromDateTime(DateTime.UtcNow) : null,
            Status = status,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }

    #endregion
}
