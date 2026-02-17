using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using RE2.ComplianceApi.Controllers.V1;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using Xunit;

namespace RE2.ComplianceApi.Tests.Controllers.V1;

/// <summary>
/// Tests for GDP document API endpoints on GdpProvidersController.
/// T242: Tests document CRUD endpoints per FR-044.
/// </summary>
public class GdpProvidersControllerDocumentTests
{
    private readonly Mock<IGdpComplianceService> _mockGdpService;
    private readonly Mock<ILogger<GdpProvidersController>> _mockLogger;
    private readonly GdpProvidersController _controller;

    public GdpProvidersControllerDocumentTests()
    {
        _mockGdpService = new Mock<IGdpComplianceService>();
        _mockLogger = new Mock<ILogger<GdpProvidersController>>();
        _controller = new GdpProvidersController(_mockGdpService.Object, _mockLogger.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    #region GetCredentialDocuments

    [Fact]
    public async Task GetCredentialDocuments_ShouldReturnDocumentList()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var documents = new List<GdpDocument>
        {
            CreateTestDocument(credentialId),
            CreateTestDocument(credentialId)
        };

        _mockGdpService
            .Setup(s => s.GetDocumentsByEntityAsync(GdpDocumentEntityType.Credential, credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);

        // Act
        var result = await _controller.GetCredentialDocuments(credentialId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var responseDtos = okResult.Value.Should().BeAssignableTo<IEnumerable<GdpDocumentResponseDto>>().Subject;
        responseDtos.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCredentialDocuments_WithNoDocuments_ShouldReturnEmptyList()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        _mockGdpService
            .Setup(s => s.GetDocumentsByEntityAsync(GdpDocumentEntityType.Credential, credentialId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<GdpDocument>());

        // Act
        var result = await _controller.GetCredentialDocuments(credentialId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var responseDtos = okResult.Value.Should().BeAssignableTo<IEnumerable<GdpDocumentResponseDto>>().Subject;
        responseDtos.Should().BeEmpty();
    }

    #endregion

    #region GetDocument

    [Fact]
    public async Task GetDocument_WithValidId_ShouldReturnDocument()
    {
        // Arrange
        var document = CreateTestDocument(Guid.NewGuid());
        _mockGdpService
            .Setup(s => s.GetDocumentAsync(document.DocumentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        var result = await _controller.GetDocument(document.DocumentId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<GdpDocumentResponseDto>().Subject;
        dto.DocumentId.Should().Be(document.DocumentId);
        dto.FileName.Should().Be(document.FileName);
    }

    [Fact]
    public async Task GetDocument_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _mockGdpService
            .Setup(s => s.GetDocumentAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GdpDocument?)null);

        // Act
        var result = await _controller.GetDocument(documentId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region DownloadDocument

    [Fact]
    public async Task DownloadDocument_WithValidId_ShouldReturnDownloadUrl()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var sasUri = new Uri("https://storage.blob.core.windows.net/gdp-documents/test.pdf?sv=2023&sig=abc");
        _mockGdpService
            .Setup(s => s.GetDocumentDownloadUrlAsync(documentId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((sasUri, ValidationResult.Success()));

        // Act
        var result = await _controller.DownloadDocument(documentId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<DocumentDownloadResponseDto>().Subject;
        dto.DocumentId.Should().Be(documentId);
        dto.DownloadUrl.Should().Contain("storage.blob.core.windows.net");
    }

    [Fact]
    public async Task DownloadDocument_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _mockGdpService
            .Setup(s => s.GetDocumentDownloadUrlAsync(documentId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Uri?)null, ValidationResult.Failure(new[]
            {
                new ValidationViolation { ErrorCode = ErrorCodes.NOT_FOUND, Message = "Not found" }
            })));

        // Act
        var result = await _controller.DownloadDocument(documentId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region DeleteDocument

    [Fact]
    public async Task DeleteDocument_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _mockGdpService
            .Setup(s => s.DeleteDocumentAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Act
        var result = await _controller.DeleteDocument(documentId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteDocument_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _mockGdpService
            .Setup(s => s.DeleteDocumentAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure(new[]
            {
                new ValidationViolation { ErrorCode = ErrorCodes.NOT_FOUND, Message = "Not found" }
            }));

        // Act
        var result = await _controller.DeleteDocument(documentId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GdpDocumentResponseDto

    [Fact]
    public void GdpDocumentResponseDto_FromDomain_ShouldMapCorrectly()
    {
        // Arrange
        var document = CreateTestDocument(Guid.NewGuid());

        // Act
        var dto = GdpDocumentResponseDto.FromDomain(document);

        // Assert
        dto.DocumentId.Should().Be(document.DocumentId);
        dto.OwnerEntityType.Should().Be("Credential");
        dto.OwnerEntityId.Should().Be(document.OwnerEntityId);
        dto.DocumentType.Should().Be("Certificate");
        dto.FileName.Should().Be(document.FileName);
        dto.UploadedBy.Should().Be(document.UploadedBy);
        dto.ContentType.Should().Be(document.ContentType);
        dto.FileSizeBytes.Should().Be(document.FileSizeBytes);
        dto.Description.Should().Be(document.Description);
    }

    #endregion

    private static GdpDocument CreateTestDocument(Guid credentialId) => new()
    {
        DocumentId = Guid.NewGuid(),
        OwnerEntityType = GdpDocumentEntityType.Credential,
        OwnerEntityId = credentialId,
        DocumentType = DocumentType.Certificate,
        FileName = "test-certificate.pdf",
        BlobStorageUrl = "https://storage.blob.core.windows.net/gdp-documents/test.pdf",
        UploadedDate = DateTime.UtcNow,
        UploadedBy = "Test User",
        ContentType = "application/pdf",
        FileSizeBytes = 100_000,
        Description = "Test GDP certificate"
    };
}
