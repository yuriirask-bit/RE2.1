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
            s.InternalCode.ToLowerInvariant().Contains(searchTerm));

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

    #region GET /api/v1/controlledsubstances/{id} Tests

    [Fact]
    public async Task GetSubstance_ReturnsSubstance_WhenExists()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var substance = CreateTestSubstance(substanceId);
        _mockSubstanceService
            .Setup(s => s.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _controller.GetSubstance(substanceId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ControlledSubstanceResponseDto>().Subject;
        response.SubstanceId.Should().Be(substanceId);
    }

    [Fact]
    public async Task GetSubstance_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        _mockSubstanceService
            .Setup(s => s.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);

        // Act
        var result = await _controller.GetSubstance(substanceId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.SUBSTANCE_NOT_FOUND);
    }

    #endregion

    #region GET /api/v1/controlledsubstances/by-code/{code} Tests

    [Fact]
    public async Task GetSubstanceByCode_ReturnsSubstance_WhenExists()
    {
        // Arrange
        var internalCode = "MOR-001";
        var substance = CreateTestSubstance(internalCode: internalCode);

        _mockSubstanceService
            .Setup(s => s.GetByInternalCodeAsync(internalCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _controller.GetSubstanceByCode(internalCode);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ControlledSubstanceResponseDto>().Subject;
        response.InternalCode.Should().Be(internalCode);
    }

    [Fact]
    public async Task GetSubstanceByCode_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var internalCode = "NONEXISTENT-001";
        _mockSubstanceService
            .Setup(s => s.GetByInternalCodeAsync(internalCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ControlledSubstance?)null);

        // Act
        var result = await _controller.GetSubstanceByCode(internalCode);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.SUBSTANCE_NOT_FOUND);
    }

    #endregion

    #region POST /api/v1/controlledsubstances Tests

    [Fact]
    public async Task CreateSubstance_ReturnsCreated_WhenValid()
    {
        // Arrange
        var request = new CreateControlledSubstanceRequestDto
        {
            SubstanceName = "Morphine",
            InternalCode = "MOR-NEW",
            OpiumActList = SubstanceCategories.OpiumActList.ListII,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
            IsActive = true
        };

        var createdId = Guid.NewGuid();

        _mockSubstanceService
            .Setup(s => s.CreateAsync(It.IsAny<ControlledSubstance>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((createdId, ValidationResult.Success()));

        // Act
        var result = await _controller.CreateSubstance(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(ControlledSubstancesController.GetSubstance));
        var response = createdResult.Value.Should().BeOfType<ControlledSubstanceResponseDto>().Subject;
        response.SubstanceId.Should().Be(createdId);
    }

    [Fact]
    public async Task CreateSubstance_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var request = new CreateControlledSubstanceRequestDto
        {
            SubstanceName = "",
            InternalCode = "",
            OpiumActList = SubstanceCategories.OpiumActList.None,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None
        };

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "SubstanceName is required" }
        });

        _mockSubstanceService
            .Setup(s => s.CreateAsync(It.IsAny<ControlledSubstance>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, validationResult));

        // Act
        var result = await _controller.CreateSubstance(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    #endregion

    #region PUT /api/v1/controlledsubstances/{id} Tests

    [Fact]
    public async Task UpdateSubstance_ReturnsOk_WhenValid()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var request = new UpdateControlledSubstanceRequestDto
        {
            SubstanceName = "Morphine Updated",
            InternalCode = "MOR-001",
            OpiumActList = SubstanceCategories.OpiumActList.ListII,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
            IsActive = true
        };

        var updatedSubstance = CreateTestSubstance(substanceId, substanceName: request.SubstanceName);

        _mockSubstanceService
            .Setup(s => s.UpdateAsync(It.IsAny<ControlledSubstance>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockSubstanceService
            .Setup(s => s.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedSubstance);

        // Act
        var result = await _controller.UpdateSubstance(substanceId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ControlledSubstanceResponseDto>().Subject;
        response.SubstanceName.Should().Be(request.SubstanceName);
    }

    [Fact]
    public async Task UpdateSubstance_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var request = new UpdateControlledSubstanceRequestDto
        {
            SubstanceName = "Test",
            InternalCode = "TEST-001",
            OpiumActList = SubstanceCategories.OpiumActList.ListI,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
            IsActive = true
        };

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.SUBSTANCE_NOT_FOUND, Message = "Substance not found" }
        });

        _mockSubstanceService
            .Setup(s => s.UpdateAsync(It.IsAny<ControlledSubstance>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.UpdateSubstance(substanceId, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.SUBSTANCE_NOT_FOUND);
    }

    [Fact]
    public async Task UpdateSubstance_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var request = new UpdateControlledSubstanceRequestDto
        {
            SubstanceName = "",
            InternalCode = "",
            OpiumActList = SubstanceCategories.OpiumActList.None,
            PrecursorCategory = SubstanceCategories.PrecursorCategory.None,
            IsActive = true
        };

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "Invalid substance data" }
        });

        _mockSubstanceService
            .Setup(s => s.UpdateAsync(It.IsAny<ControlledSubstance>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.UpdateSubstance(substanceId, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    #endregion

    #region DELETE /api/v1/controlledsubstances/{id} Tests

    [Fact]
    public async Task DeleteSubstance_ReturnsNoContent_WhenSuccess()
    {
        // Arrange
        var substanceId = Guid.NewGuid();

        _mockSubstanceService
            .Setup(s => s.DeleteAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var result = await _controller.DeleteSubstance(substanceId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteSubstance_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var substanceId = Guid.NewGuid();

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.SUBSTANCE_NOT_FOUND, Message = "Substance not found" }
        });

        _mockSubstanceService
            .Setup(s => s.DeleteAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.DeleteSubstance(substanceId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.SUBSTANCE_NOT_FOUND);
    }

    #endregion

    #region POST /api/v1/controlledsubstances/{id}/deactivate Tests

    [Fact]
    public async Task DeactivateSubstance_ReturnsOk_WhenSuccess()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var substance = CreateTestSubstance(substanceId);
        substance.IsActive = false;

        _mockSubstanceService
            .Setup(s => s.DeactivateAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockSubstanceService
            .Setup(s => s.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _controller.DeactivateSubstance(substanceId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ControlledSubstanceResponseDto>().Subject;
        response.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateSubstance_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var substanceId = Guid.NewGuid();

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.SUBSTANCE_NOT_FOUND, Message = "Substance not found" }
        });

        _mockSubstanceService
            .Setup(s => s.DeactivateAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.DeactivateSubstance(substanceId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.SUBSTANCE_NOT_FOUND);
    }

    [Fact]
    public async Task DeactivateSubstance_ReturnsBadRequest_WhenAlreadyInactive()
    {
        // Arrange
        var substanceId = Guid.NewGuid();

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "Substance is already inactive" }
        });

        _mockSubstanceService
            .Setup(s => s.DeactivateAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.DeactivateSubstance(substanceId);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    #endregion

    #region POST /api/v1/controlledsubstances/{id}/reactivate Tests

    [Fact]
    public async Task ReactivateSubstance_ReturnsOk_WhenSuccess()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var substance = CreateTestSubstance(substanceId);
        substance.IsActive = true;

        _mockSubstanceService
            .Setup(s => s.ReactivateAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockSubstanceService
            .Setup(s => s.GetByIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(substance);

        // Act
        var result = await _controller.ReactivateSubstance(substanceId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ControlledSubstanceResponseDto>().Subject;
        response.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ReactivateSubstance_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var substanceId = Guid.NewGuid();

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.SUBSTANCE_NOT_FOUND, Message = "Substance not found" }
        });

        _mockSubstanceService
            .Setup(s => s.ReactivateAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.ReactivateSubstance(substanceId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.SUBSTANCE_NOT_FOUND);
    }

    [Fact]
    public async Task ReactivateSubstance_ReturnsBadRequest_WhenAlreadyActive()
    {
        // Arrange
        var substanceId = Guid.NewGuid();

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "Substance is already active" }
        });

        _mockSubstanceService
            .Setup(s => s.ReactivateAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.ReactivateSubstance(substanceId);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    #endregion

    #region Helper Methods

    private static ControlledSubstance CreateTestSubstance(
        Guid? substanceId = null,
        string substanceName = "Morphine",
        string internalCode = "MOR-001",
        SubstanceCategories.OpiumActList opiumActList = SubstanceCategories.OpiumActList.ListII,
        SubstanceCategories.PrecursorCategory precursorCategory = SubstanceCategories.PrecursorCategory.None)
    {
        return new ControlledSubstance
        {
            SubstanceId = substanceId ?? Guid.NewGuid(),
            SubstanceName = substanceName,
            InternalCode = internalCode,
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
                substanceId: Guid.NewGuid(),
                substanceName: "Morphine",
                internalCode: "MOR-001",
                opiumActList: SubstanceCategories.OpiumActList.ListII),
            CreateTestSubstance(
                substanceId: Guid.NewGuid(),
                substanceName: "MDMA",
                internalCode: "MDM-001",
                opiumActList: SubstanceCategories.OpiumActList.ListI),
            CreateTestSubstance(
                substanceId: Guid.NewGuid(),
                substanceName: "Ephedrine",
                internalCode: "EPH-001",
                opiumActList: SubstanceCategories.OpiumActList.None,
                precursorCategory: SubstanceCategories.PrecursorCategory.Category1)
        };
    }

    #endregion
}
