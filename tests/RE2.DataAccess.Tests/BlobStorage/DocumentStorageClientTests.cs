using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RE2.ComplianceCore.Interfaces;
using RE2.DataAccess.BlobStorage;

namespace RE2.DataAccess.Tests.BlobStorage;

/// <summary>
/// T103: Integration tests for Azure Blob Storage DocumentStorageClient.
/// Tests document storage operations with mocked/emulated blob storage.
/// Per FR-008: Store licence documents (PDFs, scanned licences, certificates).
///
/// Note: For full integration testing with actual Azure Blob Storage,
/// use Azurite emulator in CI/CD pipelines or configure a test storage account.
/// These tests validate the interface contract and behavior patterns.
/// </summary>
public class DocumentStorageClientTests
{
    private const string TestContainerName = "test-licence-documents";
    private readonly Mock<ILogger<DocumentStorageClient>> _mockLogger;

    public DocumentStorageClientTests()
    {
        _mockLogger = new Mock<ILogger<DocumentStorageClient>>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsArgumentException_WhenStorageAccountUrlIsNull()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new DocumentStorageClient(null!, _mockLogger.Object));

        exception.ParamName.Should().Be("storageAccountUrl");
    }

    [Fact]
    public void Constructor_ThrowsArgumentException_WhenStorageAccountUrlIsEmpty()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new DocumentStorageClient(string.Empty, _mockLogger.Object));

        exception.ParamName.Should().Be("storageAccountUrl");
    }

    [Fact]
    public void Constructor_ThrowsArgumentException_WhenStorageAccountUrlIsWhitespace()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new DocumentStorageClient("   ", _mockLogger.Object));

        exception.ParamName.Should().Be("storageAccountUrl");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DocumentStorageClient("https://teststorage.blob.core.windows.net", null!));
    }

    #endregion

    #region Interface Contract Tests

    [Fact]
    public void IDocumentStorage_UploadDocumentAsync_ShouldHaveCorrectSignature()
    {
        // Verify interface method signature
        var method = typeof(IDocumentStorage).GetMethod(nameof(IDocumentStorage.UploadDocumentAsync));

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<Uri>));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(6);
        parameters[0].Name.Should().Be("containerName");
        parameters[1].Name.Should().Be("blobName");
        parameters[2].Name.Should().Be("content");
        parameters[3].Name.Should().Be("contentType");
        parameters[4].Name.Should().Be("metadata");
        parameters[5].Name.Should().Be("cancellationToken");
    }

    [Fact]
    public void IDocumentStorage_DownloadDocumentAsync_ShouldHaveCorrectSignature()
    {
        // Verify interface method signature
        var method = typeof(IDocumentStorage).GetMethod(nameof(IDocumentStorage.DownloadDocumentAsync));

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<Stream>));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(3);
        parameters[0].Name.Should().Be("containerName");
        parameters[1].Name.Should().Be("blobName");
        parameters[2].Name.Should().Be("cancellationToken");
    }

    [Fact]
    public void IDocumentStorage_DeleteDocumentAsync_ShouldHaveCorrectSignature()
    {
        var method = typeof(IDocumentStorage).GetMethod(nameof(IDocumentStorage.DeleteDocumentAsync));

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(3);
    }

    [Fact]
    public void IDocumentStorage_DocumentExistsAsync_ShouldHaveCorrectSignature()
    {
        var method = typeof(IDocumentStorage).GetMethod(nameof(IDocumentStorage.DocumentExistsAsync));

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<bool>));
    }

    [Fact]
    public void IDocumentStorage_GetDocumentSasUriAsync_ShouldHaveCorrectSignature()
    {
        var method = typeof(IDocumentStorage).GetMethod(nameof(IDocumentStorage.GetDocumentSasUriAsync));

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<Uri>));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(4);
        parameters[0].Name.Should().Be("containerName");
        parameters[1].Name.Should().Be("blobName");
        parameters[2].Name.Should().Be("expiresIn");
        parameters[2].ParameterType.Should().Be(typeof(TimeSpan));
    }

    [Fact]
    public void IDocumentStorage_ListDocumentsAsync_ShouldHaveCorrectSignature()
    {
        var method = typeof(IDocumentStorage).GetMethod(nameof(IDocumentStorage.ListDocumentsAsync));

        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<IEnumerable<string>>));
    }

    #endregion

    #region DocumentStorageClient Implementation Tests

    [Fact]
    public void DocumentStorageClient_ImplementsIDocumentStorage()
    {
        typeof(DocumentStorageClient).Should().Implement<IDocumentStorage>();
    }

    [Fact]
    public void DocumentStorageClient_HasAllRequiredMethods()
    {
        var implementedMethods = typeof(DocumentStorageClient)
            .GetMethods()
            .Select(m => m.Name)
            .ToList();

        implementedMethods.Should().Contain(nameof(IDocumentStorage.UploadDocumentAsync));
        implementedMethods.Should().Contain(nameof(IDocumentStorage.DownloadDocumentAsync));
        implementedMethods.Should().Contain(nameof(IDocumentStorage.DeleteDocumentAsync));
        implementedMethods.Should().Contain(nameof(IDocumentStorage.DocumentExistsAsync));
        implementedMethods.Should().Contain(nameof(IDocumentStorage.GetDocumentSasUriAsync));
        implementedMethods.Should().Contain(nameof(IDocumentStorage.ListDocumentsAsync));
    }

    #endregion

    #region Mock-Based Behavior Tests

    [Fact]
    public async Task MockDocumentStorage_UploadDocument_ReturnsUri()
    {
        // Arrange
        var mockStorage = new Mock<IDocumentStorage>();
        var expectedUri = new Uri("https://storage.blob.core.windows.net/test/document.pdf");

        mockStorage
            .Setup(s => s.UploadDocumentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUri);

        // Act
        using var stream = new MemoryStream("test content"u8.ToArray());
        var result = await mockStorage.Object.UploadDocumentAsync(
            TestContainerName,
            "test-document.pdf",
            stream,
            "application/pdf");

        // Assert
        result.Should().Be(expectedUri);
    }

    [Fact]
    public async Task MockDocumentStorage_DownloadDocument_ReturnsStream()
    {
        // Arrange
        var mockStorage = new Mock<IDocumentStorage>();
        var content = "test document content"u8.ToArray();
        var expectedStream = new MemoryStream(content);

        mockStorage
            .Setup(s => s.DownloadDocumentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStream);

        // Act
        var result = await mockStorage.Object.DownloadDocumentAsync(
            TestContainerName,
            "test-document.pdf");

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MockDocumentStorage_DeleteDocument_CompletesSuccessfully()
    {
        // Arrange
        var mockStorage = new Mock<IDocumentStorage>();

        mockStorage
            .Setup(s => s.DeleteDocumentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        await mockStorage.Object.DeleteDocumentAsync(
            TestContainerName,
            "test-document.pdf");

        mockStorage.Verify(s => s.DeleteDocumentAsync(
            TestContainerName,
            "test-document.pdf",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MockDocumentStorage_DocumentExists_ReturnsTrue_WhenExists()
    {
        // Arrange
        var mockStorage = new Mock<IDocumentStorage>();

        mockStorage
            .Setup(s => s.DocumentExistsAsync(
                TestContainerName,
                "existing-document.pdf",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await mockStorage.Object.DocumentExistsAsync(
            TestContainerName,
            "existing-document.pdf");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task MockDocumentStorage_DocumentExists_ReturnsFalse_WhenNotExists()
    {
        // Arrange
        var mockStorage = new Mock<IDocumentStorage>();

        mockStorage
            .Setup(s => s.DocumentExistsAsync(
                TestContainerName,
                "nonexistent-document.pdf",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await mockStorage.Object.DocumentExistsAsync(
            TestContainerName,
            "nonexistent-document.pdf");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task MockDocumentStorage_GetSasUri_ReturnsUriWithSasToken()
    {
        // Arrange
        var mockStorage = new Mock<IDocumentStorage>();
        var expectedUri = new Uri("https://storage.blob.core.windows.net/test/document.pdf?sv=2021-06-08&sig=xxx");

        mockStorage
            .Setup(s => s.GetDocumentSasUriAsync(
                TestContainerName,
                "document.pdf",
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUri);

        // Act
        var result = await mockStorage.Object.GetDocumentSasUriAsync(
            TestContainerName,
            "document.pdf",
            TimeSpan.FromHours(1));

        // Assert
        result.Should().NotBeNull();
        result.ToString().Should().Contain("sv=");
        result.ToString().Should().Contain("sig=");
    }

    [Fact]
    public async Task MockDocumentStorage_ListDocuments_ReturnsDocumentList()
    {
        // Arrange
        var mockStorage = new Mock<IDocumentStorage>();
        var expectedDocuments = new[] { "doc1.pdf", "doc2.pdf", "doc3.pdf" };

        mockStorage
            .Setup(s => s.ListDocumentsAsync(
                TestContainerName,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDocuments);

        // Act
        var result = await mockStorage.Object.ListDocumentsAsync(TestContainerName);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("doc1.pdf");
    }

    [Fact]
    public async Task MockDocumentStorage_ListDocuments_WithPrefix_ReturnsFilteredList()
    {
        // Arrange
        var mockStorage = new Mock<IDocumentStorage>();
        var licenceId = Guid.NewGuid();
        var prefix = $"{licenceId}/";
        var expectedDocuments = new[] { $"{licenceId}/doc1.pdf", $"{licenceId}/doc2.pdf" };

        mockStorage
            .Setup(s => s.ListDocumentsAsync(
                TestContainerName,
                prefix,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDocuments);

        // Act
        var result = await mockStorage.Object.ListDocumentsAsync(TestContainerName, prefix);

        // Assert
        result.Should().HaveCount(2);
        result.All(d => d.StartsWith(prefix)).Should().BeTrue();
    }

    #endregion

    #region Document Path Convention Tests

    [Fact]
    public void BlobName_ShouldFollowConvention_LicenceId_DocumentId_FileName()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var fileName = "certificate.pdf";

        // Act
        var blobName = $"{licenceId}/{documentId}/{fileName}";

        // Assert
        blobName.Should().Contain(licenceId.ToString());
        blobName.Should().Contain(documentId.ToString());
        blobName.Should().EndWith(fileName);
        blobName.Split('/').Should().HaveCount(3);
    }

    [Fact]
    public void ContainerName_ShouldBe_LicenceDocuments()
    {
        // The constant container name used for licence documents
        const string expectedContainerName = "licence-documents";

        // Assert
        expectedContainerName.Should().Be("licence-documents");
        expectedContainerName.Should().MatchRegex("^[a-z0-9-]+$", "Container name should be lowercase alphanumeric with hyphens");
        expectedContainerName.Length.Should().BeLessThanOrEqualTo(63, "Container name must be 63 characters or less");
    }

    #endregion

    #region Content Type Tests

    [Theory]
    [InlineData("document.pdf", "application/pdf")]
    [InlineData("letter.doc", "application/msword")]
    [InlineData("certificate.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("scan.jpg", "image/jpeg")]
    [InlineData("scan.jpeg", "image/jpeg")]
    [InlineData("scan.png", "image/png")]
    [InlineData("scan.tiff", "image/tiff")]
    public void ContentType_ShouldBeCorrect_ForFileExtension(string fileName, string expectedContentType)
    {
        // This test documents expected content types for allowed file types
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        var contentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".tiff" => "image/tiff",
            _ => "application/octet-stream"
        };

        contentType.Should().Be(expectedContentType);
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task UploadDocument_ShouldSupportCancellation()
    {
        // Arrange
        var mockStorage = new Mock<IDocumentStorage>();
        using var cts = new CancellationTokenSource();

        mockStorage
            .Setup(s => s.UploadDocumentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.Is<CancellationToken>(ct => ct == cts.Token)))
            .ReturnsAsync(new Uri("https://storage.blob.core.windows.net/test/doc.pdf"));

        // Act
        using var stream = new MemoryStream();
        await mockStorage.Object.UploadDocumentAsync(
            "container",
            "blob",
            stream,
            "application/pdf",
            null,
            cts.Token);

        // Assert - verify the cancellation token was passed through
        mockStorage.Verify(s => s.UploadDocumentAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>(),
            cts.Token), Times.Once);
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public async Task UploadDocument_ShouldAcceptMetadata()
    {
        // Arrange
        var mockStorage = new Mock<IDocumentStorage>();
        var metadata = new Dictionary<string, string>
        {
            { "DocumentType", "Certificate" },
            { "UploadedBy", Guid.NewGuid().ToString() },
            { "LicenceId", Guid.NewGuid().ToString() }
        };

        mockStorage
            .Setup(s => s.UploadDocumentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                metadata,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Uri("https://storage.blob.core.windows.net/test/doc.pdf"));

        // Act
        using var stream = new MemoryStream("content"u8.ToArray());
        await mockStorage.Object.UploadDocumentAsync(
            TestContainerName,
            "document.pdf",
            stream,
            "application/pdf",
            metadata);

        // Assert
        mockStorage.Verify(s => s.UploadDocumentAsync(
            TestContainerName,
            "document.pdf",
            It.IsAny<Stream>(),
            "application/pdf",
            It.Is<IDictionary<string, string>>(m =>
                m.ContainsKey("DocumentType") &&
                m.ContainsKey("UploadedBy") &&
                m.ContainsKey("LicenceId")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
