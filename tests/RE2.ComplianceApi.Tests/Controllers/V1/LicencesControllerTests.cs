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
/// T059: Integration tests for GET /api/v1/licences endpoints.
/// Tests LicencesController with mocked dependencies.
/// </summary>
public class LicencesControllerTests
{
    private readonly Mock<ILicenceService> _mockLicenceService;
    private readonly Mock<IDocumentStorage> _mockDocumentStorage;
    private readonly Mock<ILogger<LicencesController>> _mockLogger;
    private readonly LicencesController _controller;

    public LicencesControllerTests()
    {
        _mockLicenceService = new Mock<ILicenceService>();
        _mockDocumentStorage = new Mock<IDocumentStorage>();
        _mockLogger = new Mock<ILogger<LicencesController>>();

        _controller = new LicencesController(
            _mockLicenceService.Object,
            _mockDocumentStorage.Object,
            _mockLogger.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GET /api/v1/licences Tests

    [Fact]
    public async Task GetLicences_ReturnsAllLicences_WhenNoFilters()
    {
        // Arrange
        var licences = CreateTestLicences();
        _mockLicenceService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(licences);

        // Act
        var result = await _controller.GetLicences();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<LicenceResponseDto>>().Subject;
        response.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetLicences_FiltersById_WhenHolderIdAndTypeProvided()
    {
        // Arrange
        var holderId = Guid.NewGuid();
        var holderType = "Company";
        var licences = CreateTestLicences().Where(l => l.HolderType == holderType).ToList();

        _mockLicenceService
            .Setup(s => s.GetByHolderAsync(holderId, holderType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licences);

        // Act
        var result = await _controller.GetLicences(holderId, holderType);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<LicenceResponseDto>>().Subject;
        response.Should().OnlyContain(l => l.HolderType == holderType);
    }

    [Fact]
    public async Task GetLicences_FiltersbyStatus_WhenStatusProvided()
    {
        // Arrange
        var licences = CreateTestLicences();
        _mockLicenceService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(licences);

        // Act
        var result = await _controller.GetLicences(status: "Valid");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<LicenceResponseDto>>().Subject;
        response.Should().OnlyContain(l => l.Status == "Valid");
    }

    [Fact]
    public async Task GetLicences_ReturnsEmpty_WhenNoLicencesExist()
    {
        // Arrange
        _mockLicenceService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Licence>());

        // Act
        var result = await _controller.GetLicences();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<LicenceResponseDto>>().Subject;
        response.Should().BeEmpty();
    }

    #endregion

    #region GET /api/v1/licences/{id} Tests

    [Fact]
    public async Task GetLicence_ReturnsLicence_WhenExists()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var licence = CreateTestLicence(licenceId);
        _mockLicenceService
            .Setup(s => s.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);

        // Act
        var result = await _controller.GetLicence(licenceId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LicenceResponseDto>().Subject;
        response.LicenceId.Should().Be(licenceId);
    }

    [Fact]
    public async Task GetLicence_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        _mockLicenceService
            .Setup(s => s.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licence?)null);

        // Act
        var result = await _controller.GetLicence(licenceId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.LICENCE_NOT_FOUND);
    }

    #endregion

    #region GET /api/v1/licences/by-number/{licenceNumber} Tests

    [Fact]
    public async Task GetLicenceByNumber_ReturnsLicence_WhenExists()
    {
        // Arrange
        var licenceNumber = "WDA-2024-001";
        var licence = CreateTestLicence(Guid.NewGuid());
        licence.LicenceNumber = licenceNumber;

        _mockLicenceService
            .Setup(s => s.GetByLicenceNumberAsync(licenceNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);

        // Act
        var result = await _controller.GetLicenceByNumber(licenceNumber);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LicenceResponseDto>().Subject;
        response.LicenceNumber.Should().Be(licenceNumber);
    }

    [Fact]
    public async Task GetLicenceByNumber_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var licenceNumber = "NONEXISTENT-001";
        _mockLicenceService
            .Setup(s => s.GetByLicenceNumberAsync(licenceNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licence?)null);

        // Act
        var result = await _controller.GetLicenceByNumber(licenceNumber);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.LICENCE_NOT_FOUND);
    }

    #endregion

    #region GET /api/v1/licences/expiring Tests

    [Fact]
    public async Task GetExpiringLicences_ReturnsLicences_WithDefaultDays()
    {
        // Arrange
        var expiringLicences = CreateTestLicences()
            .Where(l => l.ExpiryDate.HasValue)
            .Take(1)
            .ToList();

        _mockLicenceService
            .Setup(s => s.GetExpiringLicencesAsync(90, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiringLicences);

        // Act
        var result = await _controller.GetExpiringLicences();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<LicenceResponseDto>>().Subject;
        response.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetExpiringLicences_ReturnsLicences_WithCustomDays()
    {
        // Arrange
        var daysAhead = 30;
        var expiringLicences = CreateTestLicences().Take(1).ToList();

        _mockLicenceService
            .Setup(s => s.GetExpiringLicencesAsync(daysAhead, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiringLicences);

        // Act
        var result = await _controller.GetExpiringLicences(daysAhead);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<IEnumerable<LicenceResponseDto>>();
        _mockLicenceService.Verify(s => s.GetExpiringLicencesAsync(daysAhead, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetExpiringLicences_ReturnsEmpty_WhenNoExpiringLicences()
    {
        // Arrange
        _mockLicenceService
            .Setup(s => s.GetExpiringLicencesAsync(90, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Licence>());

        // Act
        var result = await _controller.GetExpiringLicences();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<LicenceResponseDto>>().Subject;
        response.Should().BeEmpty();
    }

    #endregion

    #region POST /api/v1/licences Tests

    [Fact]
    public async Task CreateLicence_ReturnsCreated_WhenValid()
    {
        // Arrange
        var request = new CreateLicenceRequestDto
        {
            LicenceNumber = "WDA-2024-NEW",
            LicenceTypeId = Guid.NewGuid(),
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = DateOnly.FromDateTime(DateTime.Now),
            ExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddYears(5)),
            Status = "Valid",
            PermittedActivities = 7 // Possess | Store | Distribute
        };

        var createdId = Guid.NewGuid();
        var createdLicence = CreateTestLicence(createdId);
        createdLicence.LicenceNumber = request.LicenceNumber;

        _mockLicenceService
            .Setup(s => s.CreateAsync(It.IsAny<Licence>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((createdId, ValidationResult.Success()));

        _mockLicenceService
            .Setup(s => s.GetByIdAsync(createdId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdLicence);

        // Act
        var result = await _controller.CreateLicence(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(LicencesController.GetLicence));
        var response = createdResult.Value.Should().BeOfType<LicenceResponseDto>().Subject;
        response.LicenceId.Should().Be(createdId);
    }

    [Fact]
    public async Task CreateLicence_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var request = new CreateLicenceRequestDto
        {
            LicenceNumber = "",
            LicenceTypeId = Guid.Empty,
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = DateOnly.FromDateTime(DateTime.Now),
            Status = "Valid",
            PermittedActivities = 0
        };

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "LicenceNumber is required" }
        });

        _mockLicenceService
            .Setup(s => s.CreateAsync(It.IsAny<Licence>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, validationResult));

        // Act
        var result = await _controller.CreateLicence(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    #endregion

    #region PUT /api/v1/licences/{id} Tests

    [Fact]
    public async Task UpdateLicence_ReturnsOk_WhenValid()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var request = new UpdateLicenceRequestDto
        {
            LicenceNumber = "WDA-2024-UPDATED",
            LicenceTypeId = Guid.NewGuid(),
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = DateOnly.FromDateTime(DateTime.Now),
            ExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddYears(5)),
            Status = "Valid",
            PermittedActivities = 7
        };

        var updatedLicence = CreateTestLicence(licenceId);
        updatedLicence.LicenceNumber = request.LicenceNumber;

        _mockLicenceService
            .Setup(s => s.UpdateAsync(It.IsAny<Licence>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockLicenceService
            .Setup(s => s.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedLicence);

        // Act
        var result = await _controller.UpdateLicence(licenceId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LicenceResponseDto>().Subject;
        response.LicenceNumber.Should().Be(request.LicenceNumber);
    }

    [Fact]
    public async Task UpdateLicence_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var request = new UpdateLicenceRequestDto
        {
            LicenceNumber = "WDA-2024-001",
            LicenceTypeId = Guid.NewGuid(),
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = DateOnly.FromDateTime(DateTime.Now),
            Status = "Valid",
            PermittedActivities = 1
        };

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.LICENCE_NOT_FOUND, Message = "Licence not found" }
        });

        _mockLicenceService
            .Setup(s => s.UpdateAsync(It.IsAny<Licence>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.UpdateLicence(licenceId, request);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.LICENCE_NOT_FOUND);
    }

    [Fact]
    public async Task UpdateLicence_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var request = new UpdateLicenceRequestDto
        {
            LicenceNumber = "",
            LicenceTypeId = Guid.Empty,
            HolderType = "InvalidType",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = DateOnly.FromDateTime(DateTime.Now),
            Status = "Valid",
            PermittedActivities = 0
        };

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "Invalid holder type" }
        });

        _mockLicenceService
            .Setup(s => s.UpdateAsync(It.IsAny<Licence>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.UpdateLicence(licenceId, request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    #endregion

    #region DELETE /api/v1/licences/{id} Tests

    [Fact]
    public async Task DeleteLicence_ReturnsNoContent_WhenSuccess()
    {
        // Arrange
        var licenceId = Guid.NewGuid();

        _mockLicenceService
            .Setup(s => s.DeleteAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var result = await _controller.DeleteLicence(licenceId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteLicence_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var licenceId = Guid.NewGuid();

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.LICENCE_NOT_FOUND, Message = "Licence not found" }
        });

        _mockLicenceService
            .Setup(s => s.DeleteAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.DeleteLicence(licenceId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.LICENCE_NOT_FOUND);
    }

    #endregion

    #region Helper Methods

    private static Licence CreateTestLicence(Guid licenceId)
    {
        var licenceTypeId = Guid.NewGuid();
        return new Licence
        {
            LicenceId = licenceId,
            LicenceNumber = $"WDA-{DateTime.Now.Year}-{licenceId.ToString()[..4]}",
            LicenceTypeId = licenceTypeId,
            LicenceType = new LicenceType
            {
                LicenceTypeId = licenceTypeId,
                Name = "Wholesale Distribution Authorisation (WDA)",
                IssuingAuthority = "IGJ",
                TypicalValidityMonths = 60,
                PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                     LicenceTypes.PermittedActivity.Store |
                                     LicenceTypes.PermittedActivity.Distribute,
                IsActive = true
            },
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-1)),
            ExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddYears(4)),
            Status = "Valid",
            Scope = "Pharmaceutical distribution within EU",
            PermittedActivities = LicenceTypes.PermittedActivity.Possess |
                                 LicenceTypes.PermittedActivity.Store |
                                 LicenceTypes.PermittedActivity.Distribute,
            CreatedDate = DateTime.UtcNow.AddYears(-1),
            ModifiedDate = DateTime.UtcNow
        };
    }

    private static List<Licence> CreateTestLicences()
    {
        var licences = new List<Licence>
        {
            CreateTestLicence(Guid.NewGuid()),
            CreateTestLicence(Guid.NewGuid()),
            CreateTestLicence(Guid.NewGuid())
        };

        // Modify second licence to be expired
        licences[1].Status = "Expired";
        licences[1].ExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));

        // Modify third licence to be for a Customer instead of Company
        licences[2].HolderType = "Customer";

        return licences;
    }

    #endregion
}
