using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.ComplianceCore.Services.LicenceValidation;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Tests.Services;

/// <summary>
/// T283: Unit tests for LicenceService.
/// Tests CRUD operations, validation, document management, verification, and scope changes.
/// </summary>
public class LicenceServiceTests
{
    private readonly Mock<ILicenceRepository> _licenceRepoMock;
    private readonly Mock<ILicenceTypeRepository> _licenceTypeRepoMock;
    private readonly Mock<IControlledSubstanceRepository> _substanceRepoMock;
    private readonly Mock<IDocumentStorage> _documentStorageMock;
    private readonly Mock<ILogger<LicenceService>> _loggerMock;
    private readonly LicenceService _service;

    public LicenceServiceTests()
    {
        _licenceRepoMock = new Mock<ILicenceRepository>();
        _licenceTypeRepoMock = new Mock<ILicenceTypeRepository>();
        _substanceRepoMock = new Mock<IControlledSubstanceRepository>();
        _documentStorageMock = new Mock<IDocumentStorage>();
        _loggerMock = new Mock<ILogger<LicenceService>>();

        _service = new LicenceService(
            _licenceRepoMock.Object,
            _licenceTypeRepoMock.Object,
            _substanceRepoMock.Object,
            _documentStorageMock.Object,
            _loggerMock.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ReturnsLicence_WhenFound()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var licence = CreateValidLicence(licenceId);

        _licenceRepoMock.Setup(r => r.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);
        _licenceTypeRepoMock.Setup(r => r.GetByIdAsync(licence.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateActiveLicenceType());

        // Act
        var result = await _service.GetByIdAsync(licenceId);

        // Assert
        result.Should().NotBeNull();
        result!.LicenceId.Should().Be(licenceId);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        _licenceRepoMock.Setup(r => r.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licence?)null);

        // Act
        var result = await _service.GetByIdAsync(licenceId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByLicenceNumberAsync Tests

    [Fact]
    public async Task GetByLicenceNumberAsync_ReturnsLicence_WhenFound()
    {
        // Arrange
        var licence = CreateValidLicence();
        _licenceRepoMock.Setup(r => r.GetByLicenceNumberAsync("LIC-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);
        _licenceTypeRepoMock.Setup(r => r.GetByIdAsync(licence.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateActiveLicenceType());

        // Act
        var result = await _service.GetByLicenceNumberAsync("LIC-001");

        // Assert
        result.Should().NotBeNull();
        result!.LicenceNumber.Should().Be("LIC-001");
    }

    [Fact]
    public async Task GetByLicenceNumberAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        _licenceRepoMock.Setup(r => r.GetByLicenceNumberAsync("NONEXISTENT", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licence?)null);

        // Act
        var result = await _service.GetByLicenceNumberAsync("NONEXISTENT");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByHolderAsync Tests

    [Fact]
    public async Task GetByHolderAsync_ReturnsLicences_WhenFound()
    {
        // Arrange
        var holderId = Guid.NewGuid();
        var licences = new List<Licence> { CreateValidLicence(), CreateValidLicence() };
        _licenceRepoMock.Setup(r => r.GetByHolderAsync(holderId, "Customer", It.IsAny<CancellationToken>()))
            .ReturnsAsync(licences);
        _licenceTypeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateActiveLicenceType() });

        // Act
        var result = await _service.GetByHolderAsync(holderId, "Customer");

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllLicences()
    {
        // Arrange
        var licences = new List<Licence> { CreateValidLicence(), CreateValidLicence() };
        _licenceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(licences);
        _licenceTypeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateActiveLicenceType() });

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetExpiringLicencesAsync Tests

    [Fact]
    public async Task GetExpiringLicencesAsync_ReturnsExpiringLicences()
    {
        // Arrange
        var licences = new List<Licence> { CreateValidLicence() };
        _licenceRepoMock.Setup(r => r.GetExpiringLicencesAsync(90, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licences);
        _licenceTypeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateActiveLicenceType() });

        // Act
        var result = await _service.GetExpiringLicencesAsync(90);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ReturnsId_WhenValid()
    {
        // Arrange
        var licence = CreateValidLicence();
        var expectedId = Guid.NewGuid();

        _licenceTypeRepoMock.Setup(r => r.GetByIdAsync(licence.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateActiveLicenceType());
        _licenceRepoMock.Setup(r => r.GetByLicenceNumberAsync(licence.LicenceNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licence?)null);
        _licenceRepoMock.Setup(r => r.CreateAsync(licence, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        var (id, result) = await _service.CreateAsync(licence);

        // Assert
        result.IsValid.Should().BeTrue();
        id.Should().Be(expectedId);
    }

    [Fact]
    public async Task CreateAsync_FailsValidation_WhenDuplicateNumber()
    {
        // Arrange
        var licence = CreateValidLicence();
        var existing = CreateValidLicence();

        _licenceTypeRepoMock.Setup(r => r.GetByIdAsync(licence.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateActiveLicenceType());
        _licenceRepoMock.Setup(r => r.GetByLicenceNumberAsync(licence.LicenceNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var (id, result) = await _service.CreateAsync(licence);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.Message.Contains("already exists"));
    }

    [Fact]
    public async Task CreateAsync_FailsValidation_WhenMissingLicenceType()
    {
        // Arrange
        var licence = CreateValidLicence();
        _licenceTypeRepoMock.Setup(r => r.GetByIdAsync(licence.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LicenceType?)null);

        // Act
        var (id, result) = await _service.CreateAsync(licence);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_FailsValidation_WhenInactiveLicenceType()
    {
        // Arrange
        var licence = CreateValidLicence();
        var inactiveType = CreateActiveLicenceType();
        inactiveType.IsActive = false;

        _licenceTypeRepoMock.Setup(r => r.GetByIdAsync(licence.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveType);

        // Act
        var (id, result) = await _service.CreateAsync(licence);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.Message.Contains("not active"));
    }

    [Fact]
    public async Task CreateAsync_FailsValidation_WhenInvalidPermittedActivities()
    {
        // Arrange
        var licenceType = CreateActiveLicenceType();
        licenceType.PermittedActivities = LicenceTypes.PermittedActivity.Store; // Only store

        var licence = CreateValidLicence();
        licence.PermittedActivities = LicenceTypes.PermittedActivity.Import; // Not in type

        _licenceTypeRepoMock.Setup(r => r.GetByIdAsync(licence.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licenceType);

        // Act
        var (id, result) = await _service.CreateAsync(licence);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_FailsValidation_WhenInvalidHolderType()
    {
        // Arrange
        var licence = CreateValidLicence();
        licence.HolderType = "Invalid";

        _licenceTypeRepoMock.Setup(r => r.GetByIdAsync(licence.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateActiveLicenceType());

        // Act
        var (id, result) = await _service.CreateAsync(licence);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_FailsValidation_WhenModelValidationFails()
    {
        // Arrange - licence with empty required fields
        var licence = new Licence
        {
            LicenceNumber = "",
            HolderType = "Customer",
            IssuingAuthority = "",
            Status = "Valid"
        };

        // Act
        var (id, result) = await _service.CreateAsync(licence);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_Succeeds_WhenValid()
    {
        // Arrange
        var licence = CreateValidLicence();
        _licenceRepoMock.Setup(r => r.GetByIdAsync(licence.LicenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(licence);
        _licenceTypeRepoMock.Setup(r => r.GetByIdAsync(licence.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateActiveLicenceType());

        // Act
        var result = await _service.UpdateAsync(licence);

        // Assert
        result.IsValid.Should().BeTrue();
        _licenceRepoMock.Verify(r => r.UpdateAsync(licence, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Fails_WhenNotFound()
    {
        // Arrange
        var licence = CreateValidLicence();
        _licenceRepoMock.Setup(r => r.GetByIdAsync(licence.LicenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licence?)null);

        // Act
        var result = await _service.UpdateAsync(licence);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.LICENCE_NOT_FOUND);
    }

    [Fact]
    public async Task UpdateAsync_Fails_WhenDuplicateNumber()
    {
        // Arrange
        var licence = CreateValidLicence();
        var existing = CreateValidLicence();
        existing.LicenceNumber = "OLD-NUMBER";
        var duplicate = CreateValidLicence();

        _licenceRepoMock.Setup(r => r.GetByIdAsync(licence.LicenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _licenceTypeRepoMock.Setup(r => r.GetByIdAsync(licence.LicenceTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateActiveLicenceType());
        _licenceRepoMock.Setup(r => r.GetByLicenceNumberAsync(licence.LicenceNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(duplicate);

        // Act
        var result = await _service.UpdateAsync(licence);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.Message.Contains("already exists"));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_Succeeds_WhenFound()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        _licenceRepoMock.Setup(r => r.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidLicence(licenceId));

        // Act
        var result = await _service.DeleteAsync(licenceId);

        // Assert
        result.IsValid.Should().BeTrue();
        _licenceRepoMock.Verify(r => r.DeleteAsync(licenceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Fails_WhenNotFound()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        _licenceRepoMock.Setup(r => r.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licence?)null);

        // Act
        var result = await _service.DeleteAsync(licenceId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.LICENCE_NOT_FOUND);
    }

    #endregion

    #region Document Operations Tests

    [Fact]
    public async Task UploadDocumentAsync_Succeeds_WhenLicenceExists()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var document = CreateValidDocument(licenceId);
        var content = new MemoryStream(new byte[] { 1, 2, 3 });
        var expectedId = Guid.NewGuid();

        _licenceRepoMock.Setup(r => r.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidLicence(licenceId));
        _documentStorageMock.Setup(r => r.UploadDocumentAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Uri("https://storage.blob.core.windows.net/docs/test.pdf"));
        _licenceRepoMock.Setup(r => r.AddDocumentAsync(document, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        var (id, result) = await _service.UploadDocumentAsync(licenceId, document, content);

        // Assert
        result.IsValid.Should().BeTrue();
        id.Should().Be(expectedId);
    }

    [Fact]
    public async Task UploadDocumentAsync_Fails_WhenLicenceNotFound()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var document = CreateValidDocument(licenceId);
        var content = new MemoryStream(new byte[] { 1, 2, 3 });

        _licenceRepoMock.Setup(r => r.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licence?)null);

        // Act
        var (id, result) = await _service.UploadDocumentAsync(licenceId, document, content);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.LICENCE_NOT_FOUND);
    }

    [Fact]
    public async Task DeleteDocumentAsync_Succeeds_WhenFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var document = CreateValidDocument(Guid.NewGuid());
        document.DocumentId = documentId;
        document.BlobStorageUrl = "https://storage.blob.core.windows.net/docs/test.pdf";

        _licenceRepoMock.Setup(r => r.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        // Act
        var result = await _service.DeleteDocumentAsync(documentId);

        // Assert
        result.IsValid.Should().BeTrue();
        _licenceRepoMock.Verify(r => r.DeleteDocumentAsync(documentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteDocumentAsync_Fails_WhenNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _licenceRepoMock.Setup(r => r.GetDocumentByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LicenceDocument?)null);

        // Act
        var result = await _service.DeleteDocumentAsync(documentId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.NOT_FOUND);
    }

    #endregion

    #region Verification Operations Tests

    [Fact]
    public async Task RecordVerificationAsync_Succeeds_WhenLicenceExists()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var verification = CreateValidVerification(licenceId);
        var expectedId = Guid.NewGuid();

        _licenceRepoMock.Setup(r => r.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidLicence(licenceId));
        _licenceRepoMock.Setup(r => r.AddVerificationAsync(verification, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        var (id, result) = await _service.RecordVerificationAsync(verification);

        // Assert
        result.IsValid.Should().BeTrue();
        id.Should().Be(expectedId);
    }

    [Fact]
    public async Task RecordVerificationAsync_Fails_WhenLicenceNotFound()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var verification = CreateValidVerification(licenceId);

        _licenceRepoMock.Setup(r => r.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licence?)null);

        // Act
        var (id, result) = await _service.RecordVerificationAsync(verification);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.LICENCE_NOT_FOUND);
    }

    [Fact]
    public async Task GetVerificationHistoryAsync_ReturnsHistory()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var verifications = new List<LicenceVerification> { CreateValidVerification(licenceId) };
        _licenceRepoMock.Setup(r => r.GetVerificationHistoryAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(verifications);

        // Act
        var result = await _service.GetVerificationHistoryAsync(licenceId);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region Scope Change Operations Tests

    [Fact]
    public async Task RecordScopeChangeAsync_Succeeds_WhenLicenceExists()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var scopeChange = CreateValidScopeChange(licenceId);
        var expectedId = Guid.NewGuid();

        _licenceRepoMock.Setup(r => r.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidLicence(licenceId));
        _licenceRepoMock.Setup(r => r.AddScopeChangeAsync(scopeChange, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        // Act
        var (id, result) = await _service.RecordScopeChangeAsync(scopeChange);

        // Assert
        result.IsValid.Should().BeTrue();
        id.Should().Be(expectedId);
    }

    [Fact]
    public async Task RecordScopeChangeAsync_Fails_WhenLicenceNotFound()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var scopeChange = CreateValidScopeChange(licenceId);

        _licenceRepoMock.Setup(r => r.GetByIdAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Licence?)null);

        // Act
        var (id, result) = await _service.RecordScopeChangeAsync(scopeChange);

        // Assert
        result.IsValid.Should().BeFalse();
        id.Should().BeNull();
        result.Violations.Should().Contain(v => v.ErrorCode == ErrorCodes.LICENCE_NOT_FOUND);
    }

    [Fact]
    public async Task GetScopeChangesAsync_ReturnsHistory()
    {
        // Arrange
        var licenceId = Guid.NewGuid();
        var changes = new List<LicenceScopeChange> { CreateValidScopeChange(licenceId) };
        _licenceRepoMock.Setup(r => r.GetScopeChangesAsync(licenceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(changes);

        // Act
        var result = await _service.GetScopeChangesAsync(licenceId);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region Helper Methods

    private static readonly Guid DefaultLicenceTypeId = Guid.NewGuid();

    private static Licence CreateValidLicence(Guid? licenceId = null)
    {
        return new Licence
        {
            LicenceId = licenceId ?? Guid.NewGuid(),
            LicenceNumber = "LIC-001",
            LicenceTypeId = DefaultLicenceTypeId,
            HolderType = "Customer",
            HolderId = Guid.NewGuid(),
            IssuingAuthority = "IGJ",
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            Status = "Valid",
            PermittedActivities = LicenceTypes.PermittedActivity.Store | LicenceTypes.PermittedActivity.Distribute
        };
    }

    private static LicenceType CreateActiveLicenceType()
    {
        return new LicenceType
        {
            LicenceTypeId = DefaultLicenceTypeId,
            Name = "Opium Act Licence",
            IssuingAuthority = "IGJ",
            PermittedActivities = LicenceTypes.PermittedActivity.Store |
                                  LicenceTypes.PermittedActivity.Distribute |
                                  LicenceTypes.PermittedActivity.Import |
                                  LicenceTypes.PermittedActivity.Export,
            IsActive = true
        };
    }

    private static LicenceDocument CreateValidDocument(Guid licenceId)
    {
        return new LicenceDocument
        {
            DocumentId = Guid.NewGuid(),
            LicenceId = licenceId,
            DocumentType = DocumentType.Certificate,
            FileName = "certificate.pdf",
            BlobStorageUrl = "https://storage.blob.core.windows.net/docs/cert.pdf",
            UploadedBy = Guid.NewGuid(),
            UploadedDate = DateTime.UtcNow
        };
    }

    private static LicenceVerification CreateValidVerification(Guid licenceId)
    {
        return new LicenceVerification
        {
            VerificationId = Guid.NewGuid(),
            LicenceId = licenceId,
            VerificationMethod = VerificationMethod.AuthorityWebsite,
            VerificationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            VerifiedBy = Guid.NewGuid(),
            VerifierName = "Test Verifier",
            Outcome = VerificationOutcome.Valid,
            AuthorityReferenceNumber = "REF-001"
        };
    }

    private static LicenceScopeChange CreateValidScopeChange(Guid licenceId)
    {
        return new LicenceScopeChange
        {
            ChangeId = Guid.NewGuid(),
            LicenceId = licenceId,
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ChangeDescription = "Added morphine to scope",
            ChangeType = ScopeChangeType.SubstancesAdded,
            RecordedBy = Guid.NewGuid(),
            RecorderName = "Test Recorder",
            SubstancesAdded = "MOR-001"
        };
    }

    #endregion
}
