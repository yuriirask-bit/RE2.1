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
/// T073f: Integration tests for ControlledSubstancesController endpoints.
/// Tests controller with mocked dependencies per FR-003.
/// </summary>
public class ControlledSubstancesControllerTests
{
    private readonly Mock<IControlledSubstanceService> _mockSubstanceService;
    private readonly Mock<ILogger<ControlledSubstancesController>> _mockLogger;
    private readonly ControlledSubstancesController _controller;

    public ControlledSubstancesControllerTests()
    {
        _mockSubstanceService = new Mock<IControlledSubstanceService>();
        _mockLogger = new Mock<ILogger<ControlledSubstancesController>>();

        _controller = new ControlledSubstancesController(_mockSubstanceService.Object, _mockLogger.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GET /api/v1/controlledsubstances Tests

    [Fact]
    public async Task GetSubstances_ReturnsAllSubstances_WhenNoFilters()
    {
        // Arrange
        var substances = CreateTestSubstances();
        _mockSubstanceService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(substances);

        // Act
        var result = await _controller.GetSubstances();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<ControlledSubstanceResponseDto>>().Subject;
        response.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetSubstances_ReturnsActiveOnly_WhenActiveOnlyFilter()
    {
        // Arrange
        var substances = CreateTestSubstances().Where(s => s.IsActive);
        _mockSubstanceService
            .Setup(s => s.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(substances);

        // Act
        var result = await _controller.GetSubstances(activeOnly: true);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<ControlledSubstanceResponseDto>>().Subject;
        response.Should().OnlyContain(s => s.IsActive);
    }

    [Fact]
    public async Task GetSubstances_FiltersByOpiumActList_WhenProvided()
    {
        // Arrange
        var listISubstances = CreateTestSubstances().Where(s => s.OpiumActList == SubstanceCategories.OpiumActList.ListI);
        _mockSubstanceService
            .Setup(s => s.GetByOpiumActListAsync(SubstanceCategories.OpiumActList.ListI, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listISubstances);

        // Act
        var result = await _controller.GetSubstances(opiumActList: SubstanceCategories.OpiumActList.ListI);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<ControlledSubstanceResponseDto>>().Subject;
        response.Should().OnlyContain(s => s.OpiumActList == SubstanceCategories.OpiumActList.ListI);
    }

    [Fact]
    public async Task GetSubstances_FiltersByPrecursorCategory_WhenProvided()
    {
        // Arrange
        var category1Substances = CreateTestSubstances()
            .Where(s => s.PrecursorCategory == SubstanceCategories.PrecursorCategory.Category1);
        _mockSubstanceService
            .Setup(s => s.GetByPrecursorCategoryAsync(SubstanceCategories.PrecursorCategory.Category1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category1Substances);

        // Act
        var result = await _controller.GetSubstances(precursorCategory: SubstanceCategories.PrecursorCategory.Category1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<ControlledSubstanceResponseDto>>().Subject;
        response.Should().OnlyContain(s => s.PrecursorCategory == SubstanceCategories.PrecursorCategory.Category1);
    }

    [Fact]
    public async Task GetSubstances_SearchesSubstances_WhenSearchProvided()
    {
        // Arrange
        var searchTerm = "morphine";
        var matchingSubstances = CreateTestSubstances().Where(s =>
            s.SubstanceName.ToLowerInvariant().Contains(searchTerm) ||
            s.SubstanceCode.ToLowerInvariant().Contains(searchTerm));

        _mockSubstanceService
            .Setup(s => s.SearchAsync(searchTerm, It.IsAny<CancellationToken>()))
            .ReturnsAsync(matchingSubstances);

        // Act
        var result = await _controller.GetSubstances(search: searchTerm);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<ControlledSubstanceResponseDto>>();
        _mockSubstanceService.Verify(s => s.SearchAsync(searchTerm, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSubstances_ReturnsEmpty_WhenNoSubstancesExist()
    {
        // Arrange
        _mockSubstanceService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<ControlledSubstance>());

        // Act
        var result = await _controller.GetSubstances();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<ControlledSubstanceResponseDto>>().Subject;
        response.Should().BeEmpty();
    }

    #endregion

    #region GET /api/v1/controlledsubstances/{substanceCode} Tests

    [Fact]
    public async Task GetSubstance_ReturnsSubstance_WhenExists()
    {
        // Arrange
        var substanceCode = "Morphine";
        var substance = CreateTestSubstance(substanceCode: substanceCode);
        _mockSubstanceService
            .Setup(s => s.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _controller.GetSubstance(substanceCode);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ControlledSubstanceResponseDto>().Subject;
        response.SubstanceCode.Should().Be(substanceCode);
    }

    [Fact]
    public async Task GetSubstance_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var substanceCode = "NONEXISTENT";
        _mockSubstanceService
            .Setup(s => s.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);

        // Act
        var result = await _controller.GetSubstance(substanceCode);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.SUBSTANCE_NOT_FOUND);
    }

    #endregion

    #region POST /api/v1/controlledsubstances/configure-compliance Tests

    [Fact]
    public async Task ConfigureCompliance_ReturnsOk_WhenValid()
    {
        // Arrange
        var substanceCode = "Morphine";
        var request = new ConfigureComplianceRequestDto
        {
            SubstanceCode = substanceCode,
            RegulatoryRestrictions = "Cold storage required",
            IsActive = true
        };

        var substance = CreateTestSubstance(substanceCode: substanceCode);

        _mockSubstanceService
            .Setup(s => s.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        _mockSubstanceService
            .Setup(s => s.ConfigureComplianceAsync(It.IsAny<ControlledSubstance>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var result = await _controller.ConfigureCompliance(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ControlledSubstanceResponseDto>().Subject;
        response.SubstanceCode.Should().Be(substanceCode);
    }

    [Fact]
    public async Task ConfigureCompliance_ReturnsNotFound_WhenSubstanceDoesNotExist()
    {
        // Arrange
        var request = new ConfigureComplianceRequestDto
        {
            SubstanceCode = "NONEXISTENT",
            IsActive = true
        };

        _mockSubstanceService
            .Setup(s => s.GetBySubstanceCodeAsync("NONEXISTENT", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);

        // Act
        var result = await _controller.ConfigureCompliance(request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.SUBSTANCE_NOT_FOUND);
    }

    [Fact]
    public async Task ConfigureCompliance_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var substanceCode = "Morphine";
        var request = new ConfigureComplianceRequestDto
        {
            SubstanceCode = substanceCode,
            IsActive = true
        };

        var substance = CreateTestSubstance(substanceCode: substanceCode);

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "Compliance configuration failed" }
        });

        _mockSubstanceService
            .Setup(s => s.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        _mockSubstanceService
            .Setup(s => s.ConfigureComplianceAsync(It.IsAny<ControlledSubstance>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.ConfigureCompliance(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    #endregion

    #region PUT /api/v1/controlledsubstances/{substanceCode}/compliance Tests

    [Fact]
    public async Task UpdateCompliance_ReturnsOk_WhenValid()
    {
        // Arrange
        var substanceCode = "Morphine";
        var request = new UpdateComplianceRequestDto
        {
            RegulatoryRestrictions = "Updated restrictions",
            IsActive = true
        };

        var substance = CreateTestSubstance(substanceCode: substanceCode, substanceName: "Morphine Updated");

        _mockSubstanceService
            .Setup(s => s.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        _mockSubstanceService
            .Setup(s => s.UpdateComplianceAsync(It.IsAny<ControlledSubstance>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var result = await _controller.UpdateCompliance(substanceCode, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ControlledSubstanceResponseDto>().Subject;
        response.SubstanceName.Should().Be("Morphine Updated");
    }

    [Fact]
    public async Task UpdateCompliance_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var substanceCode = "NONEXISTENT";
        var request = new UpdateComplianceRequestDto
        {
            IsActive = true
        };

        _mockSubstanceService
            .Setup(s => s.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);

        // Act
        var result = await _controller.UpdateCompliance(substanceCode, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.SUBSTANCE_NOT_FOUND);
    }

    [Fact]
    public async Task UpdateCompliance_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var substanceCode = "Morphine";
        var request = new UpdateComplianceRequestDto
        {
            IsActive = true
        };

        var substance = CreateTestSubstance(substanceCode: substanceCode);

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "Invalid compliance data" }
        });

        _mockSubstanceService
            .Setup(s => s.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        _mockSubstanceService
            .Setup(s => s.UpdateComplianceAsync(It.IsAny<ControlledSubstance>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.UpdateCompliance(substanceCode, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    #endregion

    #region POST /api/v1/controlledsubstances/{substanceCode}/deactivate Tests

    [Fact]
    public async Task DeactivateSubstance_ReturnsOk_WhenSuccess()
    {
        // Arrange
        var substanceCode = "Morphine";
        var substance = CreateTestSubstance(substanceCode: substanceCode);
        substance.IsActive = false;

        _mockSubstanceService
            .Setup(s => s.DeactivateAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockSubstanceService
            .Setup(s => s.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _controller.DeactivateSubstance(substanceCode);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ControlledSubstanceResponseDto>().Subject;
        response.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateSubstance_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var substanceCode = "NONEXISTENT";

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.SUBSTANCE_NOT_FOUND, Message = "Substance not found" }
        });

        _mockSubstanceService
            .Setup(s => s.DeactivateAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.DeactivateSubstance(substanceCode);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.SUBSTANCE_NOT_FOUND);
    }

    [Fact]
    public async Task DeactivateSubstance_ReturnsBadRequest_WhenAlreadyInactive()
    {
        // Arrange
        var substanceCode = "Morphine";

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "Substance is already inactive" }
        });

        _mockSubstanceService
            .Setup(s => s.DeactivateAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.DeactivateSubstance(substanceCode);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    #endregion

    #region POST /api/v1/controlledsubstances/{substanceCode}/reactivate Tests

    [Fact]
    public async Task ReactivateSubstance_ReturnsOk_WhenSuccess()
    {
        // Arrange
        var substanceCode = "Morphine";
        var substance = CreateTestSubstance(substanceCode: substanceCode);
        substance.IsActive = true;

        _mockSubstanceService
            .Setup(s => s.ReactivateAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockSubstanceService
            .Setup(s => s.GetBySubstanceCodeAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _controller.ReactivateSubstance(substanceCode);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ControlledSubstanceResponseDto>().Subject;
        response.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ReactivateSubstance_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var substanceCode = "NONEXISTENT";

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.SUBSTANCE_NOT_FOUND, Message = "Substance not found" }
        });

        _mockSubstanceService
            .Setup(s => s.ReactivateAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.ReactivateSubstance(substanceCode);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.SUBSTANCE_NOT_FOUND);
    }

    [Fact]
    public async Task ReactivateSubstance_ReturnsBadRequest_WhenAlreadyActive()
    {
        // Arrange
        var substanceCode = "Morphine";

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "Substance is already active" }
        });

        _mockSubstanceService
            .Setup(s => s.ReactivateAsync(substanceCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.ReactivateSubstance(substanceCode);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    #endregion

    #region Helper Methods

    private static ControlledSubstance CreateTestSubstance(
        string substanceCode = "Morphine",
        string substanceName = "Morphine",
        SubstanceCategories.OpiumActList opiumActList = SubstanceCategories.OpiumActList.ListII,
        SubstanceCategories.PrecursorCategory precursorCategory = SubstanceCategories.PrecursorCategory.None)
    {
        return new ControlledSubstance
        {
            SubstanceCode = substanceCode,
            SubstanceName = substanceName,
            OpiumActList = opiumActList,
            PrecursorCategory = precursorCategory,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
    }

    private static List<ControlledSubstance> CreateTestSubstances()
    {
        return new List<ControlledSubstance>
        {
            CreateTestSubstance(
                substanceCode: "Morphine",
                substanceName: "Morphine",
                opiumActList: SubstanceCategories.OpiumActList.ListII),
            CreateTestSubstance(
                substanceCode: "MDMA",
                substanceName: "MDMA",
                opiumActList: SubstanceCategories.OpiumActList.ListI),
            CreateTestSubstance(
                substanceCode: "Ephedrine",
                substanceName: "Ephedrine",
                opiumActList: SubstanceCategories.OpiumActList.None,
                precursorCategory: SubstanceCategories.PrecursorCategory.Category1)
        };
    }

    #endregion
}
