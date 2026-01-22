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
/// T071d: Integration tests for LicenceTypesController API endpoints.
/// Tests all CRUD operations with mocked repository dependencies.
/// </summary>
public class LicenceTypesControllerTests
{
    private readonly Mock<ILicenceTypeRepository> _mockRepository;
    private readonly Mock<ILogger<LicenceTypesController>> _mockLogger;
    private readonly LicenceTypesController _controller;

    public LicenceTypesControllerTests()
    {
        _mockRepository = new Mock<ILicenceTypeRepository>();
        _mockLogger = new Mock<ILogger<LicenceTypesController>>();

        _controller = new LicenceTypesController(_mockRepository.Object, _mockLogger.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GET /api/v1/licencetypes Tests

    [Fact]
    public async Task GetLicenceTypes_ReturnsAllLicenceTypes_WhenActiveOnlyIsFalse()
    {
        // Arrange
        var licenceTypes = CreateTestLicenceTypes();
        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(licenceTypes);

        // Act
        var result = await _controller.GetLicenceTypes(activeOnly: false);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<LicenceTypeResponseDto>>().Subject;
        response.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetLicenceTypes_ReturnsOnlyActiveLicenceTypes_WhenActiveOnlyIsTrue()
    {
        // Arrange
        var activeLicenceTypes = CreateTestLicenceTypes().Where(lt => lt.IsActive).ToList();
        _mockRepository
            .Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeLicenceTypes);

        // Act
        var result = await _controller.GetLicenceTypes(activeOnly: true);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<LicenceTypeResponseDto>>().Subject;
        response.Should().OnlyContain(lt => lt.IsActive);
    }

    [Fact]
    public async Task GetLicenceTypes_ReturnsEmptyList_WhenNoLicenceTypesExist()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<LicenceType>());

        // Act
        var result = await _controller.GetLicenceTypes();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<LicenceTypeResponseDto>>().Subject;
        response.Should().BeEmpty();
    }

    #endregion

    #region GET /api/v1/licencetypes/{id} Tests

    [Fact]
    public async Task GetLicenceType_ReturnsLicenceType_WhenExists()
    {
        // Arrange
        var licenceType = CreateTestLicenceType();
        _mockRepository
            .Setup(r => r.GetByIdAsync(licenceType.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licenceType);

        // Act
        var result = await _controller.GetLicenceType(licenceType.LicenceTypeId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LicenceTypeResponseDto>().Subject;
        response.LicenceTypeId.Should().Be(licenceType.LicenceTypeId);
        response.Name.Should().Be(licenceType.Name);
    }

    [Fact]
    public async Task GetLicenceType_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _mockRepository
            .Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LicenceType?)null);

        // Act
        var result = await _controller.GetLicenceType(nonExistentId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.LICENCE_TYPE_NOT_FOUND);
        error.Message.Should().Contain(nonExistentId.ToString());
    }

    #endregion

    #region GET /api/v1/licencetypes/by-name/{name} Tests

    [Fact]
    public async Task GetLicenceTypeByName_ReturnsLicenceType_WhenExists()
    {
        // Arrange
        var licenceType = CreateTestLicenceType();
        _mockRepository
            .Setup(r => r.GetByNameAsync(licenceType.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licenceType);

        // Act
        var result = await _controller.GetLicenceTypeByName(licenceType.Name);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LicenceTypeResponseDto>().Subject;
        response.Name.Should().Be(licenceType.Name);
    }

    [Fact]
    public async Task GetLicenceTypeByName_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var nonExistentName = "NonExistentType";
        _mockRepository
            .Setup(r => r.GetByNameAsync(nonExistentName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LicenceType?)null);

        // Act
        var result = await _controller.GetLicenceTypeByName(nonExistentName);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.LICENCE_TYPE_NOT_FOUND);
        error.Message.Should().Contain(nonExistentName);
    }

    #endregion

    #region POST /api/v1/licencetypes Tests

    [Fact]
    public async Task CreateLicenceType_ReturnsCreated_WhenValid()
    {
        // Arrange
        var request = new CreateLicenceTypeRequestDto
        {
            Name = "Opium Act Exemption",
            IssuingAuthority = "Farmatec",
            TypicalValidityMonths = 12,
            PermittedActivities = (int)(LicenceTypes.PermittedActivity.Import | LicenceTypes.PermittedActivity.Export),
            IsActive = true
        };
        var newId = Guid.NewGuid();

        _mockRepository
            .Setup(r => r.GetByNameAsync(request.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LicenceType?)null);
        _mockRepository
            .Setup(r => r.CreateAsync(It.IsAny<LicenceType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newId);

        // Act
        var result = await _controller.CreateLicenceType(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        var response = createdResult.Value.Should().BeOfType<LicenceTypeResponseDto>().Subject;
        response.Name.Should().Be(request.Name);
        response.IssuingAuthority.Should().Be(request.IssuingAuthority);
    }

    [Fact]
    public async Task CreateLicenceType_ReturnsBadRequest_WhenNameIsEmpty()
    {
        // Arrange
        var request = new CreateLicenceTypeRequestDto
        {
            Name = "",
            IssuingAuthority = "Farmatec",
            PermittedActivities = (int)LicenceTypes.PermittedActivity.Import
        };

        // Act
        var result = await _controller.CreateLicenceType(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
        error.Details.Should().Contain("Name is required");
    }

    [Fact]
    public async Task CreateLicenceType_ReturnsBadRequest_WhenIssuingAuthorityIsEmpty()
    {
        // Arrange
        var request = new CreateLicenceTypeRequestDto
        {
            Name = "Test Licence",
            IssuingAuthority = "",
            PermittedActivities = (int)LicenceTypes.PermittedActivity.Import
        };

        // Act
        var result = await _controller.CreateLicenceType(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
        error.Details.Should().Contain("IssuingAuthority is required");
    }

    [Fact]
    public async Task CreateLicenceType_ReturnsBadRequest_WhenNoPermittedActivities()
    {
        // Arrange
        var request = new CreateLicenceTypeRequestDto
        {
            Name = "Test Licence",
            IssuingAuthority = "IGJ",
            PermittedActivities = 0 // None
        };

        // Act
        var result = await _controller.CreateLicenceType(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
        error.Details.Should().Contain("PermittedActivities");
    }

    [Fact]
    public async Task CreateLicenceType_ReturnsBadRequest_WhenNameAlreadyExists()
    {
        // Arrange
        var existingType = CreateTestLicenceType();
        var request = new CreateLicenceTypeRequestDto
        {
            Name = existingType.Name,
            IssuingAuthority = "Farmatec",
            PermittedActivities = (int)LicenceTypes.PermittedActivity.Import
        };

        _mockRepository
            .Setup(r => r.GetByNameAsync(existingType.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingType);

        // Act
        var result = await _controller.CreateLicenceType(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
        error.Message.Should().Contain("already exists");
    }

    #endregion

    #region PUT /api/v1/licencetypes/{id} Tests

    [Fact]
    public async Task UpdateLicenceType_ReturnsOk_WhenValid()
    {
        // Arrange
        var existingType = CreateTestLicenceType();
        var request = new UpdateLicenceTypeRequestDto
        {
            Name = "Updated Name",
            IssuingAuthority = "Updated Authority",
            TypicalValidityMonths = 24,
            PermittedActivities = (int)(LicenceTypes.PermittedActivity.Import | LicenceTypes.PermittedActivity.Manufacture),
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(existingType.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingType);
        _mockRepository
            .Setup(r => r.GetByNameAsync(request.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LicenceType?)null);
        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<LicenceType>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup to return updated entity after update
        var updatedType = new LicenceType
        {
            LicenceTypeId = existingType.LicenceTypeId,
            Name = request.Name,
            IssuingAuthority = request.IssuingAuthority,
            TypicalValidityMonths = request.TypicalValidityMonths,
            PermittedActivities = (LicenceTypes.PermittedActivity)request.PermittedActivities,
            IsActive = request.IsActive
        };
        _mockRepository
            .SetupSequence(r => r.GetByIdAsync(existingType.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingType) // First call for existence check
            .ReturnsAsync(updatedType);  // Second call after update

        // Act
        var result = await _controller.UpdateLicenceType(existingType.LicenceTypeId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LicenceTypeResponseDto>().Subject;
        response.Name.Should().Be(request.Name);
        response.IssuingAuthority.Should().Be(request.IssuingAuthority);
    }

    [Fact]
    public async Task UpdateLicenceType_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateLicenceTypeRequestDto
        {
            Name = "Updated Name",
            IssuingAuthority = "IGJ",
            PermittedActivities = (int)LicenceTypes.PermittedActivity.Import
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LicenceType?)null);

        // Act
        var result = await _controller.UpdateLicenceType(nonExistentId, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.LICENCE_TYPE_NOT_FOUND);
    }

    [Fact]
    public async Task UpdateLicenceType_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var existingType = CreateTestLicenceType();
        var request = new UpdateLicenceTypeRequestDto
        {
            Name = "",
            IssuingAuthority = "IGJ",
            PermittedActivities = (int)LicenceTypes.PermittedActivity.Import
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(existingType.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingType);

        // Act
        var result = await _controller.UpdateLicenceType(existingType.LicenceTypeId, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    [Fact]
    public async Task UpdateLicenceType_ReturnsBadRequest_WhenDuplicateName()
    {
        // Arrange
        var existingType = CreateTestLicenceType();
        var anotherType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Another Type",
            IssuingAuthority = "IGJ",
            PermittedActivities = LicenceTypes.PermittedActivity.Store
        };
        var request = new UpdateLicenceTypeRequestDto
        {
            Name = anotherType.Name, // Try to rename to existing name
            IssuingAuthority = "IGJ",
            PermittedActivities = (int)LicenceTypes.PermittedActivity.Import
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(existingType.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingType);
        _mockRepository
            .Setup(r => r.GetByNameAsync(anotherType.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(anotherType);

        // Act
        var result = await _controller.UpdateLicenceType(existingType.LicenceTypeId, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
        error.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task UpdateLicenceType_AllowsSameName_WhenNameNotChanged()
    {
        // Arrange
        var existingType = CreateTestLicenceType();
        var request = new UpdateLicenceTypeRequestDto
        {
            Name = existingType.Name, // Keep same name
            IssuingAuthority = "Updated Authority",
            PermittedActivities = (int)LicenceTypes.PermittedActivity.Import,
            IsActive = true
        };

        var updatedType = new LicenceType
        {
            LicenceTypeId = existingType.LicenceTypeId,
            Name = request.Name,
            IssuingAuthority = request.IssuingAuthority,
            PermittedActivities = (LicenceTypes.PermittedActivity)request.PermittedActivities,
            IsActive = request.IsActive
        };

        _mockRepository
            .SetupSequence(r => r.GetByIdAsync(existingType.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingType)
            .ReturnsAsync(updatedType);
        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<LicenceType>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateLicenceType(existingType.LicenceTypeId, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region DELETE /api/v1/licencetypes/{id} Tests

    [Fact]
    public async Task DeleteLicenceType_ReturnsNoContent_WhenExists()
    {
        // Arrange
        var existingType = CreateTestLicenceType();
        _mockRepository
            .Setup(r => r.GetByIdAsync(existingType.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingType);
        _mockRepository
            .Setup(r => r.DeleteAsync(existingType.LicenceTypeId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteLicenceType(existingType.LicenceTypeId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockRepository.Verify(r => r.DeleteAsync(existingType.LicenceTypeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteLicenceType_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _mockRepository
            .Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LicenceType?)null);

        // Act
        var result = await _controller.DeleteLicenceType(nonExistentId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.LICENCE_TYPE_NOT_FOUND);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Response Mapping Tests

    [Fact]
    public async Task GetLicenceType_MapsAllFieldsCorrectly()
    {
        // Arrange
        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Wholesale Licence (WDA)",
            IssuingAuthority = "IGJ",
            TypicalValidityMonths = 60,
            PermittedActivities = LicenceTypes.PermittedActivity.Distribute | LicenceTypes.PermittedActivity.Store,
            IsActive = true
        };
        _mockRepository
            .Setup(r => r.GetByIdAsync(licenceType.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licenceType);

        // Act
        var result = await _controller.GetLicenceType(licenceType.LicenceTypeId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LicenceTypeResponseDto>().Subject;

        response.LicenceTypeId.Should().Be(licenceType.LicenceTypeId);
        response.Name.Should().Be(licenceType.Name);
        response.IssuingAuthority.Should().Be(licenceType.IssuingAuthority);
        response.TypicalValidityMonths.Should().Be(licenceType.TypicalValidityMonths);
        response.PermittedActivities.Should().Be((int)licenceType.PermittedActivities);
        response.IsActive.Should().Be(licenceType.IsActive);
    }

    [Fact]
    public async Task GetLicenceType_MapsNullTypicalValidityMonthsCorrectly()
    {
        // Arrange - permanent licence with no validity period
        var licenceType = new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Permanent Licence",
            IssuingAuthority = "IGJ",
            TypicalValidityMonths = null,
            PermittedActivities = LicenceTypes.PermittedActivity.Import,
            IsActive = true
        };
        _mockRepository
            .Setup(r => r.GetByIdAsync(licenceType.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licenceType);

        // Act
        var result = await _controller.GetLicenceType(licenceType.LicenceTypeId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LicenceTypeResponseDto>().Subject;
        response.TypicalValidityMonths.Should().BeNull();
    }

    #endregion

    #region Test Helpers

    private static LicenceType CreateTestLicenceType()
    {
        return new LicenceType
        {
            LicenceTypeId = Guid.NewGuid(),
            Name = "Wholesale Licence (WDA)",
            IssuingAuthority = "IGJ",
            TypicalValidityMonths = 60,
            PermittedActivities = LicenceTypes.PermittedActivity.Distribute | LicenceTypes.PermittedActivity.Store,
            IsActive = true
        };
    }

    private static List<LicenceType> CreateTestLicenceTypes()
    {
        return new List<LicenceType>
        {
            new LicenceType
            {
                LicenceTypeId = Guid.NewGuid(),
                Name = "Wholesale Licence (WDA)",
                IssuingAuthority = "IGJ",
                TypicalValidityMonths = 60,
                PermittedActivities = LicenceTypes.PermittedActivity.Distribute | LicenceTypes.PermittedActivity.Store,
                IsActive = true
            },
            new LicenceType
            {
                LicenceTypeId = Guid.NewGuid(),
                Name = "Opium Act Exemption",
                IssuingAuthority = "Farmatec",
                TypicalValidityMonths = 12,
                PermittedActivities = LicenceTypes.PermittedActivity.Import | LicenceTypes.PermittedActivity.Export,
                IsActive = true
            },
            new LicenceType
            {
                LicenceTypeId = Guid.NewGuid(),
                Name = "Legacy Licence",
                IssuingAuthority = "IGJ",
                TypicalValidityMonths = 24,
                PermittedActivities = LicenceTypes.PermittedActivity.Distribute,
                IsActive = false // Inactive licence type
            }
        };
    }

    #endregion
}
