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
using System.Security.Claims;
using System.Text;

namespace RE2.ComplianceApi.Tests.Controllers.V1;

/// <summary>
/// T102: Integration tests for document upload endpoints.
/// Tests LicencesController document operations with mocked dependencies.
/// Per FR-008: Upload licence documents (PDFs, letters).
/// </summary>
public class LicencesControllerDocumentTests
{
    private readonly Mock<ILicenceService> _mockLicenceService;
    private readonly Mock<IDocumentStorage> _mockDocumentStorage;
    private readonly Mock<ILogger<LicencesController>> _mockLogger;
    private readonly LicencesController _controller;

    public LicencesControllerDocumentTests()
    {
        _mockLicenceService = new Mock<ILicenceService>();
        _mockDocumentStorage = new Mock<IDocumentStorage>();
        _mockLogger = new Mock<ILogger<LicencesController>>();

        _controller = new LicencesController(
            _mockLicenceService.Object,
            _mockDocumentStorage.Object,
            _mockLogger.Object);

        // Setup HttpContext with user claims
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "ComplianceManager")
        }, "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region GET /api/v1/licences/{id}/documents Tests

    [Fact]
    public async Task GetDocuments_ReturnsDocuments_WhenLicenceExists()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var licence = CreateTestLicence(licenceId);
        var documents = CreateTestDocuments(licenceId);

        _mockLicenceService
            .Setup(s => s.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);

        _mockLicenceService
            .Setup(s => s.GetDocumentsAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _controller.GetDocuments(licenceId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<LicenceDocumentResponseDto>>().Subject;
        response.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDocuments_ReturnsNotFound_WhenLicenceDoesNotExist()
    {
        // Arrange
        var licenceId = Guid.NewGuid();

        _mockLicenceService
            .Setup(s => s.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licence?)null);

        // Act
        var result = await _controller.GetDocuments(licenceId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.LICENCE_NOT_FOUND);
    }

    [Fact]
    public async Task GetDocuments_ReturnsEmptyList_WhenNoDocumentsExist()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var licence = CreateTestLicence(licenceId);

        _mockLicenceService
            .Setup(s => s.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);

        _mockLicenceService
            .Setup(s => s.GetDocumentsAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<LicenceDocument>());

        // Act
        var result = await _controller.GetDocuments(licenceId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<LicenceDocumentResponseDto>>().Subject;
        response.Should().BeEmpty();
    }

    #endregion

    #region POST /api/v1/licences/{id}/documents Tests

    [Fact]
    public async Task UploadDocument_ReturnsCreated_WhenValid()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var fileContent = "Test document content"u8.ToArray();
        var fileName = "test-certificate.pdf";

        var mockFile = CreateMockFormFile(fileContent, fileName, "application/pdf");

        var createdDocument = new LicenceDocument
        {
            DocumentId = documentId,
            LicenceId = licenceId,
            DocumentType = DocumentType.Certificate,
            FileName = fileName,
            UploadedDate = DateTime.UtcNow,
            UploadedBy = Guid.NewGuid(),
            ContentType = "application/pdf",
            FileSizeBytes = fileContent.Length,
            BlobStorageUrl = $"https://storage.blob.core.windows.net/licence-documents/{licenceId}/{documentId}/{fileName}"
        };

        _mockLicenceService
            .Setup(s => s.UploadDocumentAsync(
                licenceId,
                It.IsAny<LicenceDocument>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((documentId, ValidationResult.Success()));

        _mockLicenceService
            .Setup(s => s.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDocument);

        // Act
        var result = await _controller.UploadDocument(licenceId, mockFile.Object, (int)DocumentType.Certificate);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(LicencesController.GetDocuments));
        var response = createdResult.Value.Should().BeOfType<LicenceDocumentResponseDto>().Subject;
        response.DocumentId.Should().Be(documentId);
        response.FileName.Should().Be(fileName);
    }

    [Fact]
    public async Task UploadDocument_ReturnsBadRequest_WhenFileIsNull()
    {
        // Arrange
        var licenceId = Guid.NewGuid();

        // Act
        var result = await _controller.UploadDocument(licenceId, null!, (int)DocumentType.Certificate);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
        errorResponse.Message.Should().Contain("File is required");
    }

    [Fact]
    public async Task UploadDocument_ReturnsBadRequest_WhenFileIsEmpty()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var mockFile = CreateMockFormFile(Array.Empty<byte>(), "empty.pdf", "application/pdf");

        // Act
        var result = await _controller.UploadDocument(licenceId, mockFile.Object, (int)DocumentType.Certificate);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    [Fact]
    public async Task UploadDocument_ReturnsNotFound_WhenLicenceDoesNotExist()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var fileContent = "Test content"u8.ToArray();
        var mockFile = CreateMockFormFile(fileContent, "test.pdf", "application/pdf");

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.LICENCE_NOT_FOUND, Message = "Licence not found" }
        });

        _mockLicenceService
            .Setup(s => s.UploadDocumentAsync(
                licenceId,
                It.IsAny<LicenceDocument>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, validationResult));

        // Act
        var result = await _controller.UploadDocument(licenceId, mockFile.Object, (int)DocumentType.Certificate);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.LICENCE_NOT_FOUND);
    }

    [Fact]
    public async Task UploadDocument_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var fileContent = "Test content"u8.ToArray();
        var mockFile = CreateMockFormFile(fileContent, "test.exe", "application/octet-stream");

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.VALIDATION_ERROR, Message = "Invalid file type" }
        });

        _mockLicenceService
            .Setup(s => s.UploadDocumentAsync(
                licenceId,
                It.IsAny<LicenceDocument>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((null, validationResult));

        // Act
        var result = await _controller.UploadDocument(licenceId, mockFile.Object, (int)DocumentType.Other);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.VALIDATION_ERROR);
    }

    #endregion

    #region GET /api/v1/licences/{id}/documents/{documentId}/download Tests

    [Fact]
    public async Task DownloadDocument_ReturnsRedirect_WhenDocumentExists()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var document = CreateTestDocument(licenceId, documentId);
        var sasUri = new Uri($"https://storage.blob.core.windows.net/licence-documents/{licenceId}/{documentId}/test.pdf?sv=2021-06-08&sig=xxx");

        _mockLicenceService
            .Setup(s => s.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockDocumentStorage
            .Setup(s => s.GetDocumentSasUriAsync(
                "licence-documents",
                $"{licenceId}/{documentId}/{document.FileName}",
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sasUri);

        // Act
        var result = await _controller.DownloadDocument(licenceId, documentId);

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectResult>().Subject;
        redirectResult.Url.Should().Contain(documentId.ToString());
    }

    [Fact]
    public async Task DownloadDocument_ReturnsNotFound_WhenDocumentDoesNotExist()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        _mockLicenceService
            .Setup(s => s.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LicenceDocument?)null);

        // Act
        var result = await _controller.DownloadDocument(licenceId, documentId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.NOT_FOUND);
    }

    [Fact]
    public async Task DownloadDocument_ReturnsNotFound_WhenDocumentBelongsToDifferentLicence()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var differentLicenceId = Guid.NewGuid();
        var document = CreateTestDocument(differentLicenceId, documentId);

        _mockLicenceService
            .Setup(s => s.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        var result = await _controller.DownloadDocument(licenceId, documentId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.NOT_FOUND);
    }

    #endregion

    #region DELETE /api/v1/licences/{id}/documents/{documentId} Tests

    [Fact]
    public async Task DeleteDocument_ReturnsNoContent_WhenSuccess()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var document = CreateTestDocument(licenceId, documentId);

        _mockLicenceService
            .Setup(s => s.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _mockLicenceService
            .Setup(s => s.DeleteDocumentAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var result = await _controller.DeleteDocument(licenceId, documentId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteDocument_ReturnsNotFound_WhenDocumentDoesNotExist()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        _mockLicenceService
            .Setup(s => s.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LicenceDocument?)null);

        // Act
        var result = await _controller.DeleteDocument(licenceId, documentId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.NOT_FOUND);
    }

    [Fact]
    public async Task DeleteDocument_ReturnsBadRequest_WhenDeleteFails()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var document = CreateTestDocument(licenceId, documentId);

        _mockLicenceService
            .Setup(s => s.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        var validationResult = ValidationResult.Failure(new[]
        {
            new ValidationViolation { ErrorCode = ErrorCodes.INTERNAL_ERROR, Message = "Failed to delete from storage" }
        });

        _mockLicenceService
            .Setup(s => s.DeleteDocumentAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.DeleteDocument(licenceId, documentId);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponseDto>().Subject;
        errorResponse.ErrorCode.Should().Be(ErrorCodes.INTERNAL_ERROR);
    }

    #endregion

    #region Verification Endpoint Tests

    [Fact]
    public async Task GetVerifications_ReturnsVerifications_WhenLicenceExists()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var licence = CreateTestLicence(licenceId);
        var verifications = CreateTestVerifications(licenceId);

        _mockLicenceService
            .Setup(s => s.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);

        _mockLicenceService
            .Setup(s => s.GetVerificationHistoryAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(verifications);

        // Act
        var result = await _controller.GetVerifications(licenceId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<LicenceVerificationResponseDto>>().Subject;
        response.Should().HaveCount(2);
    }

    [Fact]
    public async Task RecordVerification_ReturnsCreated_WhenValid()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var verificationId = Guid.NewGuid();
        var request = new RecordVerificationRequestDto
        {
            VerificationMethod = (int)VerificationMethod.AuthorityWebsite,
            VerificationDate = DateOnly.FromDateTime(DateTime.Today),
            VerifiedBy = Guid.NewGuid(),
            VerifierName = "Test Verifier",
            Outcome = (int)VerificationOutcome.Valid,
            Notes = "Verified via IGJ website"
        };

        var createdVerification = new LicenceVerification
        {
            VerificationId = verificationId,
            LicenceId = licenceId,
            VerificationMethod = VerificationMethod.AuthorityWebsite,
            VerificationDate = request.VerificationDate,
            VerifiedBy = request.VerifiedBy,
            VerifierName = request.VerifierName,
            Outcome = VerificationOutcome.Valid,
            Notes = request.Notes,
            CreatedDate = DateTime.UtcNow
        };

        _mockLicenceService
            .Setup(s => s.RecordVerificationAsync(It.IsAny<LicenceVerification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((verificationId, ValidationResult.Success()));

        _mockLicenceService
            .Setup(s => s.GetLatestVerificationAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdVerification);

        // Act
        var result = await _controller.RecordVerification(licenceId, request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<LicenceVerificationResponseDto>().Subject;
        response.VerificationId.Should().Be(verificationId);
    }

    #endregion

    #region Scope Change Endpoint Tests

    [Fact]
    public async Task GetScopeChanges_ReturnsScopeChanges_WhenLicenceExists()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var licence = CreateTestLicence(licenceId);
        var scopeChanges = CreateTestScopeChanges(licenceId);

        _mockLicenceService
            .Setup(s => s.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);

        _mockLicenceService
            .Setup(s => s.GetScopeChangesAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scopeChanges);

        // Act
        var result = await _controller.GetScopeChanges(licenceId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<IEnumerable<LicenceScopeChangeResponseDto>>().Subject;
        response.Should().HaveCount(2);
    }

    [Fact]
    public async Task RecordScopeChange_ReturnsCreated_WhenValid()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var changeId = Guid.NewGuid();
        var request = new RecordScopeChangeRequestDto
        {
            EffectiveDate = DateOnly.FromDateTime(DateTime.Today),
            ChangeDescription = "Added import activity",
            ChangeType = (int)ScopeChangeType.ActivitiesAdded,
            RecordedBy = Guid.NewGuid(),
            RecorderName = "Test Recorder",
            ActivitiesAdded = "Import"
        };

        var createdScopeChange = new LicenceScopeChange
        {
            ChangeId = changeId,
            LicenceId = licenceId,
            EffectiveDate = request.EffectiveDate,
            ChangeDescription = request.ChangeDescription,
            ChangeType = ScopeChangeType.ActivitiesAdded,
            RecordedBy = request.RecordedBy,
            RecorderName = request.RecorderName,
            RecordedDate = DateTime.UtcNow,
            ActivitiesAdded = request.ActivitiesAdded
        };

        _mockLicenceService
            .Setup(s => s.RecordScopeChangeAsync(It.IsAny<LicenceScopeChange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((changeId, ValidationResult.Success()));

        _mockLicenceService
            .Setup(s => s.GetScopeChangesAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { createdScopeChange });

        // Act
        var result = await _controller.RecordScopeChange(licenceId, request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<LicenceScopeChangeResponseDto>().Subject;
        response.ChangeId.Should().Be(changeId);
    }

    #endregion

    #region Helper Methods

    private static Mock<IFormFile> CreateMockFormFile(byte[] content, string fileName, string contentType)
    {
        var mockFile = new Mock<IFormFile>();
        var stream = new MemoryStream(content);

        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(content.Length);
        mockFile.Setup(f => f.ContentType).Returns(contentType);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);

        return mockFile;
    }

    private static Licence CreateTestLicence(Guid licenceId)
    {
        return new Licence
        {
            LicenceId = licenceId,
            LicenceNumber = $"WDA-2024-{licenceId.ToString()[..4]}",
            LicenceTypeId = Guid.NewGuid(),
            HolderType = "Company",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-1)),
            ExpiryDate = DateOnly.FromDateTime(DateTime.Now.AddYears(4)),
            Status = "Valid"
        };
    }

    private static LicenceDocument CreateTestDocument(Guid licenceId, Guid documentId)
    {
        return new LicenceDocument
        {
            DocumentId = documentId,
            LicenceId = licenceId,
            DocumentType = DocumentType.Certificate,
            FileName = "test-certificate.pdf",
            BlobStorageUrl = $"https://storage.blob.core.windows.net/licence-documents/{licenceId}/{documentId}/test-certificate.pdf",
            UploadedDate = DateTime.UtcNow,
            UploadedBy = Guid.NewGuid(),
            ContentType = "application/pdf",
            FileSizeBytes = 1024
        };
    }

    private static List<LicenceDocument> CreateTestDocuments(Guid licenceId)
    {
        return new List<LicenceDocument>
        {
            CreateTestDocument(licenceId, Guid.NewGuid()),
            new LicenceDocument
            {
                DocumentId = Guid.NewGuid(),
                LicenceId = licenceId,
                DocumentType = DocumentType.Letter,
                FileName = "authority-letter.pdf",
                BlobStorageUrl = "https://storage.blob.core.windows.net/licence-documents/letter.pdf",
                UploadedDate = DateTime.UtcNow.AddDays(-30),
                UploadedBy = Guid.NewGuid(),
                ContentType = "application/pdf",
                FileSizeBytes = 512
            }
        };
    }

    private static List<LicenceVerification> CreateTestVerifications(Guid licenceId)
    {
        return new List<LicenceVerification>
        {
            new LicenceVerification
            {
                VerificationId = Guid.NewGuid(),
                LicenceId = licenceId,
                VerificationMethod = VerificationMethod.AuthorityWebsite,
                VerificationDate = DateOnly.FromDateTime(DateTime.Today),
                VerifiedBy = Guid.NewGuid(),
                VerifierName = "John Doe",
                Outcome = VerificationOutcome.Valid,
                CreatedDate = DateTime.UtcNow
            },
            new LicenceVerification
            {
                VerificationId = Guid.NewGuid(),
                LicenceId = licenceId,
                VerificationMethod = VerificationMethod.EmailConfirmation,
                VerificationDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-6)),
                VerifiedBy = Guid.NewGuid(),
                VerifierName = "Jane Smith",
                Outcome = VerificationOutcome.Valid,
                CreatedDate = DateTime.UtcNow.AddMonths(-6)
            }
        };
    }

    private static List<LicenceScopeChange> CreateTestScopeChanges(Guid licenceId)
    {
        return new List<LicenceScopeChange>
        {
            new LicenceScopeChange
            {
                ChangeId = Guid.NewGuid(),
                LicenceId = licenceId,
                EffectiveDate = DateOnly.FromDateTime(DateTime.Today),
                ChangeDescription = "Added import activity",
                ChangeType = ScopeChangeType.ActivitiesAdded,
                RecordedBy = Guid.NewGuid(),
                RecorderName = "Admin User",
                RecordedDate = DateTime.UtcNow,
                ActivitiesAdded = "Import"
            },
            new LicenceScopeChange
            {
                ChangeId = Guid.NewGuid(),
                LicenceId = licenceId,
                EffectiveDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-3)),
                ChangeDescription = "Extended substance coverage",
                ChangeType = ScopeChangeType.SubstancesAdded,
                RecordedBy = Guid.NewGuid(),
                RecorderName = "Compliance Manager",
                RecordedDate = DateTime.UtcNow.AddMonths(-3),
                SubstancesAdded = "Morphine, Codeine"
            }
        };
    }

    #endregion
}
