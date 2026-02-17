using FluentAssertions;
using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for GdpDocument domain model.
/// T231: Tests GdpDocument per data-model.md (US10).
/// </summary>
public class GdpDocumentTests
{
    [Fact]
    public void GdpDocument_ShouldHaveCorrectDefaultValues()
    {
        // Arrange & Act
        var document = new GdpDocument();

        // Assert
        document.DocumentId.Should().Be(Guid.Empty);
        document.OwnerEntityId.Should().Be(Guid.Empty);
        document.FileName.Should().BeEmpty();
        document.BlobStorageUrl.Should().BeEmpty();
        document.UploadedBy.Should().BeEmpty();
        document.ContentType.Should().BeNull();
        document.FileSizeBytes.Should().BeNull();
        document.Description.Should().BeNull();
    }

    [Fact]
    public void Validate_WithValidDocument_ShouldPass()
    {
        // Arrange
        var document = new GdpDocument
        {
            DocumentId = Guid.NewGuid(),
            OwnerEntityType = GdpDocumentEntityType.Credential,
            OwnerEntityId = Guid.NewGuid(),
            DocumentType = DocumentType.Certificate,
            FileName = "gdp-certificate.pdf",
            BlobStorageUrl = "https://storage.blob.core.windows.net/gdp-documents/credential/abc123.pdf",
            UploadedDate = DateTime.UtcNow,
            UploadedBy = "Jane Smith"
        };

        // Act
        var result = document.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyOwnerEntityId_ShouldFail()
    {
        // Arrange
        var document = CreateValidDocument();
        document.OwnerEntityId = Guid.Empty;

        // Act
        var result = document.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("OwnerEntityId is required"));
    }

    [Fact]
    public void Validate_WithEmptyFileName_ShouldFail()
    {
        // Arrange
        var document = CreateValidDocument();
        document.FileName = "";

        // Act
        var result = document.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("FileName is required"));
    }

    [Fact]
    public void Validate_WithEmptyBlobStorageUrl_ShouldFail()
    {
        // Arrange
        var document = CreateValidDocument();
        document.BlobStorageUrl = "";

        // Act
        var result = document.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("BlobStorageUrl is required"));
    }

    [Fact]
    public void Validate_WithEmptyUploadedBy_ShouldFail()
    {
        // Arrange
        var document = CreateValidDocument();
        document.UploadedBy = "";

        // Act
        var result = document.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("UploadedBy is required"));
    }

    [Theory]
    [InlineData("document.pdf", true)]
    [InlineData("document.doc", true)]
    [InlineData("document.docx", true)]
    [InlineData("document.jpg", true)]
    [InlineData("document.jpeg", true)]
    [InlineData("document.png", true)]
    [InlineData("document.tiff", true)]
    [InlineData("document.exe", false)]
    [InlineData("document.bat", false)]
    [InlineData("document.zip", false)]
    public void Validate_WithFileExtension_ShouldValidateCorrectly(string fileName, bool shouldBeValid)
    {
        // Arrange
        var document = CreateValidDocument();
        document.FileName = fileName;

        // Act
        var result = document.Validate();

        // Assert
        result.IsValid.Should().Be(shouldBeValid);
    }

    [Fact]
    public void Validate_WithMultipleViolations_ShouldReportAll()
    {
        // Arrange
        var document = new GdpDocument
        {
            OwnerEntityId = Guid.Empty,
            FileName = "",
            BlobStorageUrl = "",
            UploadedBy = ""
        };

        // Act
        var result = document.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().HaveCountGreaterOrEqualTo(4);
    }

    [Theory]
    [InlineData("document.pdf", ".pdf")]
    [InlineData("document.PDF", ".pdf")]
    [InlineData("my.file.doc", ".doc")]
    [InlineData("no-extension", "")]
    public void GetFileExtension_ShouldReturnCorrectExtension(string fileName, string expectedExtension)
    {
        // Arrange
        var document = new GdpDocument { FileName = fileName };

        // Act
        var extension = document.GetFileExtension();

        // Assert
        extension.Should().Be(expectedExtension);
    }

    [Theory]
    [InlineData("document.pdf", true)]
    [InlineData("document.PDF", true)]
    [InlineData("document.doc", false)]
    [InlineData("document.jpg", false)]
    public void IsPdf_ShouldReturnCorrectValue(string fileName, bool expected)
    {
        // Arrange
        var document = new GdpDocument { FileName = fileName };

        // Act
        var result = document.IsPdf();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("document.jpg", true)]
    [InlineData("document.JPEG", true)]
    [InlineData("document.png", true)]
    [InlineData("document.tiff", true)]
    [InlineData("document.pdf", false)]
    [InlineData("document.doc", false)]
    public void IsImage_ShouldReturnCorrectValue(string fileName, bool expected)
    {
        // Arrange
        var document = new GdpDocument { FileName = fileName };

        // Act
        var result = document.IsImage();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(GdpDocumentEntityType.Credential)]
    [InlineData(GdpDocumentEntityType.Site)]
    [InlineData(GdpDocumentEntityType.Inspection)]
    [InlineData(GdpDocumentEntityType.Provider)]
    [InlineData(GdpDocumentEntityType.Customer)]
    public void OwnerEntityType_ShouldSupportAllValues(GdpDocumentEntityType entityType)
    {
        // Arrange
        var document = CreateValidDocument();
        document.OwnerEntityType = entityType;

        // Act & Assert
        document.OwnerEntityType.Should().Be(entityType);
        document.Validate().IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(DocumentType.Certificate)]
    [InlineData(DocumentType.Letter)]
    [InlineData(DocumentType.InspectionReport)]
    [InlineData(DocumentType.Other)]
    public void DocumentType_ShouldSupportAllValues(DocumentType docType)
    {
        // Arrange
        var document = CreateValidDocument();
        document.DocumentType = docType;

        // Act & Assert
        document.DocumentType.Should().Be(docType);
        document.Validate().IsValid.Should().BeTrue();
    }

    private static GdpDocument CreateValidDocument() => new()
    {
        DocumentId = Guid.NewGuid(),
        OwnerEntityType = GdpDocumentEntityType.Credential,
        OwnerEntityId = Guid.NewGuid(),
        DocumentType = DocumentType.Certificate,
        FileName = "gdp-certificate.pdf",
        BlobStorageUrl = "https://storage.blob.core.windows.net/gdp-documents/test.pdf",
        UploadedDate = DateTime.UtcNow,
        UploadedBy = "Jane Smith"
    };
}
