using FluentAssertions;
using RE2.ComplianceCore.Models;
using Xunit;

namespace RE2.ComplianceCore.Tests.Models;

/// <summary>
/// Unit tests for LicenceDocument domain model.
/// T099: Tests LicenceDocument per data-model.md entity 12.
/// </summary>
public class LicenceDocumentTests
{
    [Fact]
    public void LicenceDocument_ShouldHaveCorrectDefaultValues()
    {
        // Arrange & Act
        var document = new LicenceDocument();

        // Assert
        document.DocumentId.Should().Be(Guid.Empty);
        document.LicenceId.Should().Be(Guid.Empty);
        document.FileName.Should().BeEmpty();
        document.BlobStorageUrl.Should().BeEmpty();
        document.UploadedBy.Should().Be(Guid.Empty);
        document.ContentType.Should().BeNull();
        document.FileSizeBytes.Should().BeNull();
    }

    [Fact]
    public void Validate_WithValidDocument_ShouldPass()
    {
        // Arrange
        var document = new LicenceDocument
        {
            DocumentId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            DocumentType = DocumentType.Certificate,
            FileName = "wholesale-licence.pdf",
            BlobStorageUrl = "https://storage.blob.core.windows.net/licences/abc123.pdf",
            UploadedDate = DateTime.UtcNow,
            UploadedBy = Guid.NewGuid()
        };

        // Act
        var result = document.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Violations.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyLicenceId_ShouldFail()
    {
        // Arrange
        var document = new LicenceDocument
        {
            DocumentId = Guid.NewGuid(),
            LicenceId = Guid.Empty,
            DocumentType = DocumentType.Certificate,
            FileName = "licence.pdf",
            BlobStorageUrl = "https://storage.blob.core.windows.net/test.pdf",
            UploadedBy = Guid.NewGuid()
        };

        // Act
        var result = document.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("LicenceId is required"));
    }

    [Fact]
    public void Validate_WithEmptyFileName_ShouldFail()
    {
        // Arrange
        var document = new LicenceDocument
        {
            DocumentId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            FileName = "",
            BlobStorageUrl = "https://storage.blob.core.windows.net/test.pdf",
            UploadedBy = Guid.NewGuid()
        };

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
        var document = new LicenceDocument
        {
            DocumentId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            FileName = "licence.pdf",
            BlobStorageUrl = "",
            UploadedBy = Guid.NewGuid()
        };

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
        var document = new LicenceDocument
        {
            DocumentId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            FileName = "licence.pdf",
            BlobStorageUrl = "https://storage.blob.core.windows.net/test.pdf",
            UploadedBy = Guid.Empty
        };

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
        var document = new LicenceDocument
        {
            DocumentId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            FileName = fileName,
            BlobStorageUrl = "https://storage.blob.core.windows.net/test",
            UploadedBy = Guid.NewGuid()
        };

        // Act
        var result = document.Validate();

        // Assert
        result.IsValid.Should().Be(shouldBeValid);
    }

    [Theory]
    [InlineData("document.pdf", ".pdf")]
    [InlineData("document.PDF", ".pdf")]
    [InlineData("my.file.doc", ".doc")]
    [InlineData("no-extension", "")]
    public void GetFileExtension_ShouldReturnCorrectExtension(string fileName, string expectedExtension)
    {
        // Arrange
        var document = new LicenceDocument { FileName = fileName };

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
        var document = new LicenceDocument { FileName = fileName };

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
        var document = new LicenceDocument { FileName = fileName };

        // Act
        var result = document.IsImage();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(DocumentType.Certificate)]
    [InlineData(DocumentType.Letter)]
    [InlineData(DocumentType.InspectionReport)]
    [InlineData(DocumentType.Other)]
    public void DocumentType_ShouldSupportAllValues(DocumentType docType)
    {
        // Arrange
        var document = new LicenceDocument
        {
            DocumentId = Guid.NewGuid(),
            LicenceId = Guid.NewGuid(),
            DocumentType = docType,
            FileName = "test.pdf",
            BlobStorageUrl = "https://storage.blob.core.windows.net/test.pdf",
            UploadedBy = Guid.NewGuid()
        };

        // Act & Assert
        document.DocumentType.Should().Be(docType);
        document.Validate().IsValid.Should().BeTrue();
    }
}
