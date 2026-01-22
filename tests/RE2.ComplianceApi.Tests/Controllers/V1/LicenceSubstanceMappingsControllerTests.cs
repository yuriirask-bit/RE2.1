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
/// T079g: Integration tests for LicenceSubstanceMappingsController API endpoints.
/// Tests all CRUD operations with mocked service dependencies.
/// </summary>
public class LicenceSubstanceMappingsControllerTests
{
    private readonly Mock<ILicenceSubstanceMappingService> _mockService;
    private readonly Mock<ILogger<LicenceSubstanceMappingsController>> _mockLogger;
    private readonly LicenceSubstanceMappingsController _controller;

    public LicenceSubstanceMappingsControllerTests()
    {
        _mockService = new Mock<ILicenceSubstanceMappingService>();
        _mockLogger = new Mock<ILogger<LicenceSubstanceMappingsController>>();

        _controller = new LicenceSubstanceMappingsController(_mockService.Object, _mockLogger.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GET /api/v1/licencesubstancemappings Tests

    [Fact]
    public async Task GetMappings_ReturnsAllMappings_WhenNoFilters()
    {
        // Arrange
        var mappings = CreateTestMappings();
        _mockService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mappings);

        // Act
        var result = await _controller.GetMappings();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<LicenceSubstanceMappingResponseDto>>().Subject;
        response.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetMappings_ReturnsMappingsForLicence_WhenLicenceIdProvided()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var mappings = CreateTestMappings().Where(m => m.LicenceId == licenceId).ToList();
        _mockService
            .Setup(s => s.GetByLicenceIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mappings);

        // Act
        var result = await _controller.GetMappings(licenceId: licenceId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<LicenceSubstanceMappingResponseDto>>().Subject;
        _mockService.Verify(s => s.GetByLicenceIdAsync(licenceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMappings_ReturnsActiveMappings_WhenLicenceIdAndActiveOnlyProvided()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var activeMappings = CreateTestMappings().Take(1).ToList();
        _mockService
            .Setup(s => s.GetActiveMappingsByLicenceIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeMappings);

        // Act
        var result = await _controller.GetMappings(licenceId: licenceId, activeOnly: true);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        _mockService.Verify(s => s.GetActiveMappingsByLicenceIdAsync(licenceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMappings_ReturnsMappingsForSubstance_WhenSubstanceIdProvided()
    {
        // Arrange
        var substanceId = Guid.NewGuid();
        var mappings = CreateTestMappings().Take(2).ToList();
        _mockService
            .Setup(s => s.GetBySubstanceIdAsync(substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mappings);

        // Act
        var result = await _controller.GetMappings(substanceId: substanceId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        _mockService.Verify(s => s.GetBySubstanceIdAsync(substanceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMappings_ReturnsEmptyList_WhenNoMappingsExist()
    {
        // Arrange
        _mockService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<LicenceSubstanceMapping>());

        // Act
        var result = await _controller.GetMappings();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<LicenceSubstanceMappingResponseDto>>().Subject;
        response.Should().BeEmpty();
    }

    #endregion

    #region GET /api/v1/licencesubstancemappings/{id} Tests

    [Fact]
    public async Task GetMapping_ReturnsMapping_WhenExists()
    {
        // Arrange
        var mapping = CreateTestMapping();
        _mockService
            .Setup(s => s.GetByIdAsync(mapping.MappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapping);

        // Act
        var result = await _controller.GetMapping(mapping.MappingId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LicenceSubstanceMappingResponseDto>().Subject;
        response.MappingId.Should().Be(mapping.MappingId);
        response.LicenceId.Should().Be(mapping.LicenceId);
        response.SubstanceId.Should().Be(mapping.SubstanceId);
    }

    [Fact]
    public async Task GetMapping_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _mockService
            .Setup(s => s.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LicenceSubstanceMapping?)null);

        // Act
        var result = await _controller.GetMapping(nonExistentId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.MAPPING_NOT_FOUND);
        error.Message.Should().Contain(nonExistentId.ToString());
    }

    #endregion

    #region GET /api/v1/licencesubstancemappings/check-authorization Tests

    [Fact]
    public async Task CheckSubstanceAuthorization_ReturnsTrue_WhenAuthorized()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var substanceId = Guid.NewGuid();
        _mockService
            .Setup(s => s.IsSubstanceAuthorizedByLicenceAsync(licenceId, substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CheckSubstanceAuthorization(licenceId, substanceId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<SubstanceAuthorizationCheckDto>().Subject;
        response.LicenceId.Should().Be(licenceId);
        response.SubstanceId.Should().Be(substanceId);
        response.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task CheckSubstanceAuthorization_ReturnsFalse_WhenNotAuthorized()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var substanceId = Guid.NewGuid();
        _mockService
            .Setup(s => s.IsSubstanceAuthorizedByLicenceAsync(licenceId, substanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CheckSubstanceAuthorization(licenceId, substanceId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<SubstanceAuthorizationCheckDto>().Subject;
        response.IsAuthorized.Should().BeFalse();
    }

    #endregion

    #region POST /api/v1/licencesubstancemappings Tests

    [Fact]
    public async Task CreateMapping_ReturnsCreated_WhenValid()
    {
        // Arrange
        var request = new CreateLicenceSubstanceMappingRequestDto
        {
            LicenceId = Guid.NewGuid(),
            SubstanceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.Today),
            MaxQuantityPerTransaction = 100,
            PeriodType = "Monthly"
        };
        var newId = Guid.NewGuid();
        var createdMapping = new LicenceSubstanceMapping
        {
            MappingId = newId,
            LicenceId = request.LicenceId,
            SubstanceId = request.SubstanceId,
            EffectiveDate = request.EffectiveDate,
            MaxQuantityPerTransaction = request.MaxQuantityPerTransaction,
            PeriodType = request.PeriodType
        };

        _mockService
            .Setup(s => s.CreateAsync(It.IsAny<LicenceSubstanceMapping>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((newId, ValidationResult.Success()));
        _mockService
            .Setup(s => s.GetByIdAsync(newId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdMapping);

        // Act
        var result = await _controller.CreateMapping(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        var response = createdResult.Value.Should().BeOfType<LicenceSubstanceMappingResponseDto>().Subject;
        response.LicenceId.Should().Be(request.LicenceId);
        response.SubstanceId.Should().Be(request.SubstanceId);
    }

    [Fact]
    public async Task CreateMapping_ReturnsBadRequest_WhenLicenceNotFound()
    {
        // Arrange
        var request = new CreateLicenceSubstanceMappingRequestDto
        {
            LicenceId = Guid.NewGuid(),
            SubstanceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.Today)
        };

        _mockService
            .Setup(s => s.CreateAsync(It.IsAny<LicenceSubstanceMapping>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, ValidationResult.Failure(new[]
            {
                new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "Licence not found" }
            })));

        // Act
        var result = await _controller.CreateMapping(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
        error.Details.Should().Contain("Licence not found");
    }

    [Fact]
    public async Task CreateMapping_ReturnsBadRequest_WhenSubstanceNotActive()
    {
        // Arrange
        var request = new CreateLicenceSubstanceMappingRequestDto
        {
            LicenceId = Guid.NewGuid(),
            SubstanceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.Today)
        };

        _mockService
            .Setup(s => s.CreateAsync(It.IsAny<LicenceSubstanceMapping>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, ValidationResult.Failure(new[]
            {
                new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "Substance is not active" }
            })));

        // Act
        var result = await _controller.CreateMapping(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.Details.Should().Contain("Substance is not active");
    }

    [Fact]
    public async Task CreateMapping_ReturnsBadRequest_WhenDuplicateMapping()
    {
        // Arrange
        var request = new CreateLicenceSubstanceMappingRequestDto
        {
            LicenceId = Guid.NewGuid(),
            SubstanceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.Today)
        };

        _mockService
            .Setup(s => s.CreateAsync(It.IsAny<LicenceSubstanceMapping>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, ValidationResult.Failure(new[]
            {
                new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "A mapping already exists" }
            })));

        // Act
        var result = await _controller.CreateMapping(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.Details.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateMapping_ReturnsBadRequest_WhenExpiryExceedsLicenceExpiry()
    {
        // Arrange
        var request = new CreateLicenceSubstanceMappingRequestDto
        {
            LicenceId = Guid.NewGuid(),
            SubstanceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.Today),
            ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddYears(10))
        };

        _mockService
            .Setup(s => s.CreateAsync(It.IsAny<LicenceSubstanceMapping>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, ValidationResult.Failure(new[]
            {
                new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "ExpiryDate cannot exceed licence ExpiryDate" }
            })));

        // Act
        var result = await _controller.CreateMapping(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.Details.Should().Contain("ExpiryDate cannot exceed");
    }

    #endregion

    #region PUT /api/v1/licencesubstancemappings/{id} Tests

    [Fact]
    public async Task UpdateMapping_ReturnsOk_WhenValid()
    {
        // Arrange
        var mappingId = Guid.NewGuid();
        var request = new UpdateLicenceSubstanceMappingRequestDto
        {
            LicenceId = Guid.NewGuid(),
            SubstanceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.Today),
            MaxQuantityPerTransaction = 200,
            PeriodType = "Annual"
        };
        var updatedMapping = new LicenceSubstanceMapping
        {
            MappingId = mappingId,
            LicenceId = request.LicenceId,
            SubstanceId = request.SubstanceId,
            EffectiveDate = request.EffectiveDate,
            MaxQuantityPerTransaction = request.MaxQuantityPerTransaction,
            PeriodType = request.PeriodType
        };

        _mockService
            .Setup(s => s.UpdateAsync(It.IsAny<LicenceSubstanceMapping>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());
        _mockService
            .Setup(s => s.GetByIdAsync(mappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedMapping);

        // Act
        var result = await _controller.UpdateMapping(mappingId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LicenceSubstanceMappingResponseDto>().Subject;
        response.MappingId.Should().Be(mappingId);
        response.MaxQuantityPerTransaction.Should().Be(200);
    }

    [Fact]
    public async Task UpdateMapping_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateLicenceSubstanceMappingRequestDto
        {
            LicenceId = Guid.NewGuid(),
            SubstanceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.Today)
        };

        _mockService
            .Setup(s => s.UpdateAsync(It.IsAny<LicenceSubstanceMapping>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(new[]
            {
                new ValidationViolation { ErrorCode = ErrorCodes.NOT_FOUND, Message = "Mapping not found" }
            }));

        // Act
        var result = await _controller.UpdateMapping(nonExistentId, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.MAPPING_NOT_FOUND);
    }

    [Fact]
    public async Task UpdateMapping_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var mappingId = Guid.NewGuid();
        var request = new UpdateLicenceSubstanceMappingRequestDto
        {
            LicenceId = Guid.NewGuid(),
            SubstanceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
            ExpiryDate = DateOnly.FromDateTime(DateTime.Today) // Expiry before effective
        };

        _mockService
            .Setup(s => s.UpdateAsync(It.IsAny<LicenceSubstanceMapping>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(new[]
            {
                new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "ExpiryDate must be after EffectiveDate" }
            }));

        // Act
        var result = await _controller.UpdateMapping(mappingId, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    #endregion

    #region DELETE /api/v1/licencesubstancemappings/{id} Tests

    [Fact]
    public async Task DeleteMapping_ReturnsNoContent_WhenExists()
    {
        // Arrange
        var mappingId = Guid.NewGuid();
        _mockService
            .Setup(s => s.DeleteAsync(mappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var result = await _controller.DeleteMapping(mappingId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockService.Verify(s => s.DeleteAsync(mappingId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteMapping_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _mockService
            .Setup(s => s.DeleteAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(new[]
            {
                new ValidationViolation { ErrorCode = ErrorCodes.NOT_FOUND, Message = "Mapping not found" }
            }));

        // Act
        var result = await _controller.DeleteMapping(nonExistentId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.MAPPING_NOT_FOUND);
    }

    #endregion

    #region Response Mapping Tests

    [Fact]
    public async Task GetMapping_MapsAllFieldsCorrectly()
    {
        // Arrange
        var mapping = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            SubstanceId = Guid.NewGuid(),
            MaxQuantityPerTransaction = 500,
            MaxQuantityPerPeriod = 5000,
            PeriodType = "Monthly",
            Restrictions = "Storage at controlled temperature only",
            EffectiveDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)),
            ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
            Licence = new Licence { LicenceId = Guid.NewGuid(), LicenceNumber = "LIC-001", HolderType = "Company", IssuingAuthority = "IGJ", Status = "Valid" },
            Substance = new ControlledSubstance { SubstanceId = Guid.NewGuid(), SubstanceName = "Morphine", InternalCode = "MOR-001" }
        };
        _mockService
            .Setup(s => s.GetByIdAsync(mapping.MappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapping);

        // Act
        var result = await _controller.GetMapping(mapping.MappingId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LicenceSubstanceMappingResponseDto>().Subject;

        response.MappingId.Should().Be(mapping.MappingId);
        response.LicenceId.Should().Be(mapping.LicenceId);
        response.LicenceNumber.Should().Be("LIC-001");
        response.SubstanceId.Should().Be(mapping.SubstanceId);
        response.SubstanceName.Should().Be("Morphine");
        response.SubstanceCode.Should().Be("MOR-001");
        response.MaxQuantityPerTransaction.Should().Be(500);
        response.MaxQuantityPerPeriod.Should().Be(5000);
        response.PeriodType.Should().Be("Monthly");
        response.Restrictions.Should().Be("Storage at controlled temperature only");
        response.EffectiveDate.Should().Be(mapping.EffectiveDate);
        response.ExpiryDate.Should().Be(mapping.ExpiryDate);
        response.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetMapping_SetsIsActiveToFalse_WhenExpired()
    {
        // Arrange
        var mapping = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            SubstanceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-2)),
            ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)) // Expired yesterday
        };
        _mockService
            .Setup(s => s.GetByIdAsync(mapping.MappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapping);

        // Act
        var result = await _controller.GetMapping(mapping.MappingId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LicenceSubstanceMappingResponseDto>().Subject;
        response.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetMapping_SetsIsActiveToFalse_WhenNotYetEffective()
    {
        // Arrange
        var mapping = new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            SubstanceId = Guid.NewGuid(),
            EffectiveDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)) // Starts tomorrow
        };
        _mockService
            .Setup(s => s.GetByIdAsync(mapping.MappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapping);

        // Act
        var result = await _controller.GetMapping(mapping.MappingId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LicenceSubstanceMappingResponseDto>().Subject;
        response.IsActive.Should().BeFalse();
    }

    #endregion

    #region Test Helpers

    private static LicenceSubstanceMapping CreateTestMapping()
    {
        return new LicenceSubstanceMapping
        {
            MappingId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            SubstanceId = Guid.NewGuid(),
            MaxQuantityPerTransaction = 100,
            MaxQuantityPerPeriod = 1000,
            PeriodType = "Monthly",
            EffectiveDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)),
            ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1))
        };
    }

    private static List<LicenceSubstanceMapping> CreateTestMappings()
    {
        var licenceId = Guid.NewGuid();
        return new List<LicenceSubstanceMapping>
        {
            new LicenceSubstanceMapping
            {
                MappingId = Guid.NewGuid(),
                LicenceId = licenceId,
                SubstanceId = Guid.NewGuid(),
                MaxQuantityPerTransaction = 100,
                PeriodType = "Monthly",
                EffectiveDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)),
                ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddYears(1)),
                Substance = new ControlledSubstance { SubstanceName = "Morphine", InternalCode = "MOR-001" }
            },
            new LicenceSubstanceMapping
            {
                MappingId = Guid.NewGuid(),
                LicenceId = licenceId,
                SubstanceId = Guid.NewGuid(),
                MaxQuantityPerTransaction = 50,
                PeriodType = "Quarterly",
                EffectiveDate = DateOnly.FromDateTime(DateTime.Today),
                Substance = new ControlledSubstance { SubstanceName = "Fentanyl", InternalCode = "FEN-001" }
            },
            new LicenceSubstanceMapping
            {
                MappingId = Guid.NewGuid(),
                LicenceId = Guid.NewGuid(), // Different licence
                SubstanceId = Guid.NewGuid(),
                EffectiveDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-2)),
                ExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), // Expired
                Substance = new ControlledSubstance { SubstanceName = "Codeine", InternalCode = "COD-001" }
            }
        };
    }

    #endregion
}
