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
/// T080o: Integration tests for SubstanceReclassificationController.
/// Tests reclassification API endpoints with mocked dependencies per FR-066.
/// </summary>
public class SubstanceReclassificationControllerTests
{
    private readonly Mock<ISubstanceReclassificationService> _mockReclassificationService;
    private readonly Mock<IControlledSubstanceRepository> _mockSubstanceRepository;
    private readonly Mock<ILogger<SubstanceReclassificationController>> _mockLogger;
    private readonly SubstanceReclassificationController _controller;

    public SubstanceReclassificationControllerTests()
    {
        _mockReclassificationService = new Mock<ISubstanceReclassificationService>();
        _mockSubstanceRepository = new Mock<IControlledSubstanceRepository>();
        _mockLogger = new Mock<ILogger<SubstanceReclassificationController>>();

        _controller = new SubstanceReclassificationController(
            _mockReclassificationService.Object,
            _mockSubstanceRepository.Object,
            _mockLogger.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region POST /api/v1/substances/{substanceCode}/reclassify Tests

    [Fact]
    public async Task CreateReclassification_ReturnsCreated_WhenValid()
    {
        // Arrange
        var substanceCode = "Morphine";
        var substance = CreateTestSubstance(substanceCode);
        var reclassificationId = Guid.NewGuid();

        var request = new CreateReclassificationRequestDto
        {
            NewOpiumActList = (int)SubstanceCategories.OpiumActList.ListI,
            NewPrecursorCategory = (int)SubstanceCategories.PrecursorCategory.None,
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            RegulatoryReference = "Staatscourant 2026/123",
            RegulatoryAuthority = "Ministry of Health"
        };

        var createdReclassification = CreateTestReclassification(reclassificationId, substanceCode);

        _mockSubstanceRepository
            .Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        _mockReclassificationService
            .Setup(s => s.CreateReclassificationAsync(It.IsAny<SubstanceReclassification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((reclassificationId, ValidationResult.Success()));

        _mockReclassificationService
            .Setup(s => s.GetByIdAsync(reclassificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdReclassification);

        // Act
        var result = await _controller.CreateReclassification(substanceCode, request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(SubstanceReclassificationController.GetReclassification));
        var response = createdResult.Value.Should().BeOfType<ReclassificationResponseDto>().Subject;
        response.ReclassificationId.Should().Be(reclassificationId);
    }

    [Fact]
    public async Task CreateReclassification_ReturnsNotFound_WhenSubstanceDoesNotExist()
    {
        // Arrange
        var substanceCode = "NONEXISTENT";
        var request = new CreateReclassificationRequestDto
        {
            NewOpiumActList = (int)SubstanceCategories.OpiumActList.ListI,
            NewPrecursorCategory = 0,
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            RegulatoryReference = "Staatscourant 2026/123",
            RegulatoryAuthority = "Ministry of Health"
        };

        _mockSubstanceRepository
            .Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);

        // Act
        var result = await _controller.CreateReclassification(substanceCode, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.NOT_FOUND);
    }

    [Fact]
    public async Task CreateReclassification_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var substanceCode = "Morphine";
        var substance = CreateTestSubstance(substanceCode);
        var request = new CreateReclassificationRequestDto
        {
            NewOpiumActList = (int)SubstanceCategories.OpiumActList.ListII, // Same as current - should fail
            NewPrecursorCategory = 0,
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            RegulatoryReference = "Staatscourant 2026/123",
            RegulatoryAuthority = "Ministry of Health"
        };

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "Must change classification" }
        });

        _mockSubstanceRepository
            .Setup(r => r.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        _mockReclassificationService
            .Setup(s => s.CreateReclassificationAsync(It.IsAny<SubstanceReclassification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, validationResult));

        // Act
        var result = await _controller.CreateReclassification(substanceCode, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    #endregion

    #region GET /api/v1/reclassifications/{id} Tests

    [Fact]
    public async Task GetReclassification_ReturnsOk_WhenExists()
    {
        // Arrange
        var reclassificationId = Guid.NewGuid();
        var reclassification = CreateTestReclassification(reclassificationId, "Morphine");

        _mockReclassificationService
            .Setup(s => s.GetByIdAsync(reclassificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reclassification);

        // Act
        var result = await _controller.GetReclassification(reclassificationId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ReclassificationResponseDto>().Subject;
        response.ReclassificationId.Should().Be(reclassificationId);
    }

    [Fact]
    public async Task GetReclassification_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var reclassificationId = Guid.NewGuid();

        _mockReclassificationService
            .Setup(s => s.GetByIdAsync(reclassificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubstanceReclassification?)null);

        // Act
        var result = await _controller.GetReclassification(reclassificationId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.NOT_FOUND);
    }

    #endregion

    #region GET /api/v1/substances/{substanceCode}/reclassifications Tests

    [Fact]
    public async Task GetSubstanceReclassifications_ReturnsOk_WithList()
    {
        // Arrange
        var substanceCode = "Morphine";
        var reclassifications = new[]
        {
            CreateTestReclassification(Guid.NewGuid(), substanceCode),
            CreateTestReclassification(Guid.NewGuid(), substanceCode)
        };

        _mockReclassificationService
            .Setup(s => s.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reclassifications);

        // Act
        var result = await _controller.GetSubstanceReclassifications(substanceCode);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<ReclassificationResponseDto>>().Subject;
        response.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSubstanceReclassifications_ReturnsEmptyList_WhenNoReclassifications()
    {
        // Arrange
        var substanceCode = "Morphine";

        _mockReclassificationService
            .Setup(s => s.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<SubstanceReclassification>());

        // Act
        var result = await _controller.GetSubstanceReclassifications(substanceCode);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<ReclassificationResponseDto>>().Subject;
        response.Should().BeEmpty();
    }

    #endregion

    #region GET /api/v1/reclassifications/pending Tests

    [Fact]
    public async Task GetPendingReclassifications_ReturnsOk_WithList()
    {
        // Arrange
        var reclassifications = new[]
        {
            CreateTestReclassification(Guid.NewGuid(), "Morphine"),
            CreateTestReclassification(Guid.NewGuid(), "Fentanyl")
        };

        _mockReclassificationService
            .Setup(s => s.GetPendingReclassificationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(reclassifications);

        // Act
        var result = await _controller.GetPendingReclassifications();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<ReclassificationResponseDto>>().Subject;
        response.Should().HaveCount(2);
    }

    #endregion

    #region POST /api/v1/reclassifications/{id}/process Tests

    [Fact]
    public async Task ProcessReclassification_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var reclassificationId = Guid.NewGuid();
        var reclassification = CreateTestReclassification(reclassificationId, "Morphine");
        reclassification.Status = ReclassificationStatus.Completed;
        reclassification.AffectedCustomerCount = 5;
        reclassification.FlaggedCustomerCount = 2;

        _mockReclassificationService
            .Setup(s => s.ProcessReclassificationAsync(reclassificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockReclassificationService
            .Setup(s => s.GetByIdAsync(reclassificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reclassification);

        // Act
        var result = await _controller.ProcessReclassification(reclassificationId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ReclassificationResponseDto>().Subject;
        response.Status.Should().Be("Completed");
        response.AffectedCustomerCount.Should().Be(5);
        response.FlaggedCustomerCount.Should().Be(2);
    }

    [Fact]
    public async Task ProcessReclassification_ReturnsBadRequest_WhenAlreadyProcessed()
    {
        // Arrange
        var reclassificationId = Guid.NewGuid();
        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "Already processed" }
        });

        _mockReclassificationService
            .Setup(s => s.ProcessReclassificationAsync(reclassificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.ProcessReclassification(reclassificationId);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    #endregion

    #region GET /api/v1/reclassifications/{id}/impact-analysis Tests

    [Fact]
    public async Task GetImpactAnalysis_ReturnsOk_WhenExists()
    {
        // Arrange
        var reclassificationId = Guid.NewGuid();
        var reclassification = CreateTestReclassification(reclassificationId, "Morphine");
        var analysis = new ReclassificationImpactAnalysis
        {
            Reclassification = reclassification,
            TotalAffectedCustomers = 10,
            CustomersWithSufficientLicences = 7,
            CustomersFlaggedForReQualification = 3
        };

        _mockReclassificationService
            .Setup(s => s.AnalyzeCustomerImpactAsync(reclassificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        // Act
        var result = await _controller.GetImpactAnalysis(reclassificationId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ImpactAnalysisResponseDto>().Subject;
        response.TotalAffectedCustomers.Should().Be(10);
        response.CustomersFlaggedForReQualification.Should().Be(3);
    }

    [Fact]
    public async Task GetImpactAnalysis_ReturnsNotFound_WhenReclassificationDoesNotExist()
    {
        // Arrange
        var reclassificationId = Guid.NewGuid();

        _mockReclassificationService
            .Setup(s => s.AnalyzeCustomerImpactAsync(reclassificationId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Reclassification {reclassificationId} not found"));

        // Act
        var result = await _controller.GetImpactAnalysis(reclassificationId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.NOT_FOUND);
    }

    #endregion

    #region GET /api/v1/reclassifications/{id}/notification Tests

    [Fact]
    public async Task GetComplianceNotification_ReturnsOk_WhenExists()
    {
        // Arrange
        var reclassificationId = Guid.NewGuid();
        var notification = new ComplianceNotification
        {
            ReclassificationId = reclassificationId,
            SubstanceName = "Test Substance",
            RegulatoryReference = "Staatscourant 2026/123",
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            TotalAffectedCustomers = 5,
            CustomersRequiringAction = 2
        };

        _mockReclassificationService
            .Setup(s => s.GenerateComplianceNotificationAsync(reclassificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        // Act
        var result = await _controller.GetComplianceNotification(reclassificationId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ComplianceNotification>().Subject;
        response.ReclassificationId.Should().Be(reclassificationId);
        response.TotalAffectedCustomers.Should().Be(5);
    }

    [Fact]
    public async Task GetComplianceNotification_ReturnsNotFound_WhenReclassificationDoesNotExist()
    {
        // Arrange
        var reclassificationId = Guid.NewGuid();

        _mockReclassificationService
            .Setup(s => s.GenerateComplianceNotificationAsync(reclassificationId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Reclassification {reclassificationId} not found"));

        // Act
        var result = await _controller.GetComplianceNotification(reclassificationId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.NOT_FOUND);
    }

    #endregion

    #region POST /api/v1/reclassifications/{id}/customers/{customerId}/requalify Tests

    [Fact]
    public async Task MarkCustomerReQualified_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var reclassificationId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        _mockReclassificationService
            .Setup(s => s.MarkCustomerReQualifiedAsync(reclassificationId, customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var result = await _controller.MarkCustomerReQualified(reclassificationId, customerId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task MarkCustomerReQualified_ReturnsBadRequest_WhenImpactNotFound()
    {
        // Arrange
        var reclassificationId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "Customer impact not found" }
        });

        _mockReclassificationService
            .Setup(s => s.MarkCustomerReQualifiedAsync(reclassificationId, customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.MarkCustomerReQualified(reclassificationId, customerId);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    #endregion

    #region GET /api/v1/customers/{customerId}/reclassification-status Tests

    [Fact]
    public async Task GetCustomerReclassificationStatus_ReturnsOk_WhenBlocked()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var impacts = new[]
        {
            new ReclassificationCustomerImpact
            {
                ImpactId = Guid.NewGuid(),
                CustomerId = customerId,
                RequiresReQualification = true
            }
        };

        _mockReclassificationService
            .Setup(s => s.CheckCustomerBlockedAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, impacts.AsEnumerable()));

        // Act
        var result = await _controller.GetCustomerReclassificationStatus(customerId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<CustomerReclassificationStatusDto>().Subject;
        response.CustomerId.Should().Be(customerId);
        response.IsBlocked.Should().BeTrue();
        response.RequiresReQualification.Should().BeTrue();
        response.BlockingImpacts.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCustomerReclassificationStatus_ReturnsOk_WhenNotBlocked()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        _mockReclassificationService
            .Setup(s => s.CheckCustomerBlockedAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, Enumerable.Empty<ReclassificationCustomerImpact>()));

        // Act
        var result = await _controller.GetCustomerReclassificationStatus(customerId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<CustomerReclassificationStatusDto>().Subject;
        response.CustomerId.Should().Be(customerId);
        response.IsBlocked.Should().BeFalse();
        response.BlockingImpacts.Should().BeEmpty();
    }

    #endregion

    #region GET /api/v1/substances/{substanceCode}/classification Tests

    [Fact]
    public async Task GetEffectiveClassification_ReturnsOk_WithCurrentDate()
    {
        // Arrange
        var substanceCode = "Morphine";
        var classification = new SubstanceClassification
        {
            SubstanceCode = substanceCode,
            AsOfDate = DateOnly.FromDateTime(DateTime.UtcNow),
            OpiumActList = SubstanceCategories.OpiumActList.ListI,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None
        };

        _mockReclassificationService
            .Setup(s => s.GetEffectiveClassificationAsync(substanceCode, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(classification);

        // Act
        var result = await _controller.GetEffectiveClassification(substanceCode);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<SubstanceClassification>().Subject;
        response.SubstanceCode.Should().Be(substanceCode);
        response.OpiumActList.Should().Be(SubstanceCategories.OpiumActList.ListI);
    }

    [Fact]
    public async Task GetEffectiveClassification_ReturnsOk_WithSpecificDate()
    {
        // Arrange
        var substanceCode = "Morphine";
        var asOfDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1));
        var classification = new SubstanceClassification
        {
            SubstanceCode = substanceCode,
            AsOfDate = asOfDate,
            OpiumActList = SubstanceCategories.OpiumActList.ListII,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None
        };

        _mockReclassificationService
            .Setup(s => s.GetEffectiveClassificationAsync(substanceCode, asOfDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(classification);

        // Act
        var result = await _controller.GetEffectiveClassification(substanceCode, asOfDate);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<SubstanceClassification>().Subject;
        response.AsOfDate.Should().Be(asOfDate);
        response.OpiumActList.Should().Be(SubstanceCategories.OpiumActList.ListII);
    }

    [Fact]
    public async Task GetEffectiveClassification_ReturnsNotFound_WhenSubstanceDoesNotExist()
    {
        // Arrange
        var substanceCode = "NONEXISTENT";

        _mockReclassificationService
            .Setup(s => s.GetEffectiveClassificationAsync(substanceCode, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Substance {substanceCode} not found"));

        // Act
        var result = await _controller.GetEffectiveClassification(substanceCode);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.NOT_FOUND);
    }

    #endregion

    #region Helper Methods

    private static ControlledSubstance CreateTestSubstance(string substanceCode)
    {
        return new ControlledSubstance
        {
            SubstanceCode = substanceCode,
            SubstanceName = "Test Substance",
            OpiumActList = SubstanceCategories.OpiumActList.ListII,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
            IsActive = true
        };
    }

    private static SubstanceReclassification CreateTestReclassification(Guid reclassificationId, string substanceCode)
    {
        return new SubstanceReclassification
        {
            ReclassificationId = reclassificationId,
            SubstanceCode = substanceCode,
            PreviousOpiumActList = SubstanceCategories.OpiumActList.ListII,
            NewOpiumActList = SubstanceCategories.OpiumActList.ListI,
            PreviousPrecursorCategory = SubstanceCategories.PrecursorCategory.None,
            NewPrecursorCategory = SubstanceCategories.PrecursorCategory.None,
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            RegulatoryReference = "Staatscourant 2026/123",
            RegulatoryAuthority = "Ministry of Health",
            Status = ReclassificationStatus.Pending,
            CreatedDate = DateTime.UtcNow
        };
    }

    #endregion
}
