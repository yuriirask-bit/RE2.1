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
/// T198: Integration tests for GDP Providers API endpoints.
/// Tests GdpProvidersController with mocked dependencies.
/// </summary>
public class GdpProvidersControllerTests
{
    private readonly Mock<IGdpComplianceService> _mockGdpService;
    private readonly Mock<ILogger<GdpProvidersController>> _mockLogger;
    private readonly GdpProvidersController _controller;

    public GdpProvidersControllerTests()
    {
        _mockGdpService = new Mock<IGdpComplianceService>();
        _mockLogger = new Mock<ILogger<GdpProvidersController>>();

        _controller = new GdpProvidersController(_mockGdpService.Object, _mockLogger.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GET /api/v1/gdp-providers Tests

    [Fact]
    public async Task GetProviders_ReturnsOk_WithProviderList()
    {
        // Arrange
        var providers = new List<GdpServiceProvider>
        {
            CreateTestProvider(Guid.NewGuid(), "MedLogistics NL B.V."),
            CreateTestProvider(Guid.NewGuid(), "PharmaTransport EU GmbH")
        };

        _mockGdpService
            .Setup(s => s.GetAllProvidersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        // Act
        var result = await _controller.GetProviders();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<GdpProviderResponseDto>>().Subject;
        response.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetProvider_ReturnsOk_WhenProviderExists()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId, "MedLogistics NL B.V.");
        _mockGdpService
            .Setup(s => s.GetProviderAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        // Act
        var result = await _controller.GetProvider(providerId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GdpProviderResponseDto>().Subject;
        response.ProviderId.Should().Be(providerId);
        response.ProviderName.Should().Be("MedLogistics NL B.V.");
    }

    [Fact]
    public async Task GetProvider_ReturnsNotFound_WhenProviderDoesNotExist()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        _mockGdpService
            .Setup(s => s.GetProviderAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpServiceProvider?)null);

        // Act
        var result = await _controller.GetProvider(providerId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var error = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.NOT_FOUND);
    }

    #endregion

    #region POST /api/v1/gdp-providers Tests

    [Fact]
    public async Task CreateProvider_ReturnsCreated_WithValidData()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var request = new CreateGdpProviderRequestDto
        {
            ProviderName = "MedLogistics NL B.V.",
            ServiceType = "ThirdPartyLogistics",
            TemperatureControlledCapability = true,
            ReviewFrequencyMonths = 24
        };

        _mockGdpService
            .Setup(s => s.CreateProviderAsync(It.IsAny<GdpServiceProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((providerId, ValidationResult.Success()));

        _mockGdpService
            .Setup(s => s.GetProviderAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestProvider(providerId, "MedLogistics NL B.V."));

        // Act
        var result = await _controller.CreateProvider(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        var response = createdResult.Value.Should().BeOfType<GdpProviderResponseDto>().Subject;
        response.ProviderId.Should().Be(providerId);
    }

    [Fact]
    public async Task CreateProvider_ReturnsBadRequest_WithInvalidData()
    {
        // Arrange
        var request = new CreateGdpProviderRequestDto
        {
            ProviderName = "",
            ServiceType = "ThirdPartyLogistics",
            ReviewFrequencyMonths = 0
        };

        var violations = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "ProviderName is required" }
        });

        _mockGdpService
            .Setup(s => s.CreateProviderAsync(It.IsAny<GdpServiceProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Guid?)null, violations));

        // Act
        var result = await _controller.CreateProvider(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        error.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    #endregion

    #region PUT /api/v1/gdp-providers/{providerId} Tests

    [Fact]
    public async Task UpdateProvider_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var request = new CreateGdpProviderRequestDto
        {
            ProviderName = "Updated Name",
            ServiceType = "Transporter",
            ReviewFrequencyMonths = 36
        };

        _mockGdpService
            .Setup(s => s.UpdateProviderAsync(It.IsAny<GdpServiceProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockGdpService
            .Setup(s => s.GetProviderAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestProvider(providerId, "Updated Name"));

        // Act
        var result = await _controller.UpdateProvider(providerId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GdpProviderResponseDto>().Subject;
        response.ProviderName.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateProvider_ReturnsNotFound_WhenProviderDoesNotExist()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var request = new CreateGdpProviderRequestDto
        {
            ProviderName = "Test",
            ServiceType = "Transporter",
            ReviewFrequencyMonths = 24
        };

        var violations = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.NOT_FOUND, Message = "Not found" }
        });

        _mockGdpService
            .Setup(s => s.UpdateProviderAsync(It.IsAny<GdpServiceProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(violations);

        // Act
        var result = await _controller.UpdateProvider(providerId, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region DELETE /api/v1/gdp-providers/{providerId} Tests

    [Fact]
    public async Task DeleteProvider_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        _mockGdpService
            .Setup(s => s.DeleteProviderAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var result = await _controller.DeleteProvider(providerId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteProvider_ReturnsNotFound_WhenProviderDoesNotExist()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        _mockGdpService
            .Setup(s => s.DeleteProviderAsync(providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(new[]
            {
                new ValidationViolation { ErrorCode = ErrorCodes.NOT_FOUND, Message = "Not found" }
            }));

        // Act
        var result = await _controller.DeleteProvider(providerId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GET /api/v1/gdp-providers/{providerId}/credentials Tests

    [Fact]
    public async Task GetProviderCredentials_ReturnsOk_WithCredentialList()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var credentials = new List<GdpCredential>
        {
            CreateTestCredential(Guid.NewGuid(), GdpCredentialEntityType.ServiceProvider, providerId)
        };

        _mockGdpService
            .Setup(s => s.GetCredentialsByEntityAsync(GdpCredentialEntityType.ServiceProvider, providerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credentials);

        // Act
        var result = await _controller.GetProviderCredentials(providerId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<GdpCredentialResponseDto>>().Subject;
        response.Should().HaveCount(1);
    }

    #endregion

    #region POST /api/v1/gdp-providers/credentials Tests

    [Fact]
    public async Task CreateCredential_ReturnsCreated_WithValidData()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var request = new CreateGdpCredentialRequestDto
        {
            EntityType = "ServiceProvider",
            EntityId = entityId,
            WdaNumber = "WDA-TEST-001",
            ValidityStartDate = "2025-01-01",
            ValidityEndDate = "2030-01-01"
        };

        _mockGdpService
            .Setup(s => s.CreateCredentialAsync(It.IsAny<GdpCredential>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((credentialId, ValidationResult.Success()));

        _mockGdpService
            .Setup(s => s.GetCredentialAsync(credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestCredential(credentialId, GdpCredentialEntityType.ServiceProvider, entityId));

        // Act
        var result = await _controller.CreateCredential(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task CreateCredential_ReturnsBadRequest_WithInvalidData()
    {
        // Arrange
        var request = new CreateGdpCredentialRequestDto
        {
            EntityType = "ServiceProvider",
            EntityId = Guid.Empty
        };

        _mockGdpService
            .Setup(s => s.CreateCredentialAsync(It.IsAny<GdpCredential>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Guid?)null, ValidationResult.Failure(new[]
            {
                new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "EntityId is required" }
            })));

        // Act
        var result = await _controller.CreateCredential(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region POST /api/v1/gdp-providers/{providerId}/reviews Tests

    [Fact]
    public async Task RecordReview_ReturnsCreated_WithValidData()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var reviewId = Guid.NewGuid();
        var request = new CreateReviewRequestDto
        {
            ReviewDate = "2025-06-15",
            ReviewMethod = "OnSiteAudit",
            ReviewOutcome = "Approved",
            ReviewerName = "Jan de Vries",
            NextReviewMonths = 24
        };

        _mockGdpService
            .Setup(s => s.RecordReviewAsync(It.IsAny<QualificationReview>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((reviewId, ValidationResult.Success()));

        // Act
        var result = await _controller.RecordReview(providerId, request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task RecordReview_ReturnsNotFound_WhenProviderDoesNotExist()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var request = new CreateReviewRequestDto
        {
            ReviewDate = "2025-06-15",
            ReviewMethod = "OnSiteAudit",
            ReviewOutcome = "Approved",
            ReviewerName = "Jan de Vries"
        };

        _mockGdpService
            .Setup(s => s.RecordReviewAsync(It.IsAny<QualificationReview>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Guid?)null, ValidationResult.Failure(new[]
            {
                new ValidationViolation { ErrorCode = ErrorCodes.NOT_FOUND, Message = "Not found" }
            })));

        // Act
        var result = await _controller.RecordReview(providerId, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region POST /api/v1/gdp-providers/credentials/{credentialId}/verifications Tests

    [Fact]
    public async Task RecordVerification_ReturnsCreated_WithValidData()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var verificationId = Guid.NewGuid();
        var request = new CreateVerificationRequestDto
        {
            VerificationDate = "2025-06-15",
            VerificationMethod = "EudraGMDP",
            VerifiedBy = "Jan de Vries",
            Outcome = "Valid"
        };

        _mockGdpService
            .Setup(s => s.RecordVerificationAsync(It.IsAny<GdpCredentialVerification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((verificationId, ValidationResult.Success()));

        // Act
        var result = await _controller.RecordVerification(credentialId, request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task RecordVerification_ReturnsNotFound_WhenCredentialDoesNotExist()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var request = new CreateVerificationRequestDto
        {
            VerificationDate = "2025-06-15",
            VerificationMethod = "EudraGMDP",
            VerifiedBy = "Jan de Vries",
            Outcome = "Valid"
        };

        _mockGdpService
            .Setup(s => s.RecordVerificationAsync(It.IsAny<GdpCredentialVerification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Guid?)null, ValidationResult.Failure(new[]
            {
                new ValidationViolation { ErrorCode = ErrorCodes.NOT_FOUND, Message = "Credential not found" }
            })));

        // Act
        var result = await _controller.RecordVerification(credentialId, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GET /api/v1/gdp-providers/check-qualification Tests

    [Fact]
    public async Task CheckPartnerQualification_ReturnsOk_WithQualifiedStatus()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        _mockGdpService
            .Setup(s => s.IsPartnerQualifiedAsync(GdpCredentialEntityType.ServiceProvider, entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CheckPartnerQualification("ServiceProvider", entityId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PartnerQualificationCheckDto>().Subject;
        response.IsQualified.Should().BeTrue();
        response.EntityId.Should().Be(entityId);
    }

    [Fact]
    public async Task CheckPartnerQualification_ReturnsOk_WithNotQualifiedStatus()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        _mockGdpService
            .Setup(s => s.IsPartnerQualifiedAsync(GdpCredentialEntityType.Supplier, entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CheckPartnerQualification("Supplier", entityId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PartnerQualificationCheckDto>().Subject;
        response.IsQualified.Should().BeFalse();
    }

    #endregion

    #region GET /api/v1/gdp-providers/requiring-review Tests

    [Fact]
    public async Task GetProvidersRequiringReview_ReturnsOk_WithProviderList()
    {
        // Arrange
        var providers = new List<GdpServiceProvider>
        {
            CreateTestProvider(Guid.NewGuid(), "Overdue Provider")
        };

        _mockGdpService
            .Setup(s => s.GetProvidersRequiringReviewAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        // Act
        var result = await _controller.GetProvidersRequiringReview();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<GdpProviderResponseDto>>().Subject;
        response.Should().HaveCount(1);
    }

    #endregion

    #region Test Helpers

    private static GdpServiceProvider CreateTestProvider(Guid id, string name) => new()
    {
        ProviderId = id,
        ProviderName = name,
        ServiceType = GdpServiceType.ThirdPartyLogistics,
        TemperatureControlledCapability = true,
        QualificationStatus = GdpQualificationStatus.Approved,
        ReviewFrequencyMonths = 24,
        IsActive = true
    };

    private static GdpCredential CreateTestCredential(Guid id, GdpCredentialEntityType entityType, Guid entityId) => new()
    {
        CredentialId = id,
        EntityType = entityType,
        EntityId = entityId,
        WdaNumber = "WDA-TEST-001",
        GdpCertificateNumber = "GDP-TEST-001",
        ValidityStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
        ValidityEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
        QualificationStatus = GdpQualificationStatus.Approved
    };

    #endregion
}
