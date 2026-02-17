using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// Licence management API endpoints.
/// T075: LicencesController v1 with GET, POST, PUT, DELETE endpoints.
/// T114-T116: Extended with document, verification, and scope change endpoints for US3.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class LicencesController : ControllerBase
{
    private readonly ILicenceService _licenceService;
    private readonly IDocumentStorage _documentStorage;
    private readonly ILogger<LicencesController> _logger;

    private const string DocumentContainerName = "licence-documents";

    public LicencesController(
        ILicenceService licenceService,
        IDocumentStorage documentStorage,
        ILogger<LicencesController> logger)
    {
        _licenceService = licenceService;
        _documentStorage = documentStorage;
        _logger = logger;
    }

    /// <summary>
    /// Gets all licences with optional filtering.
    /// </summary>
    /// <param name="holderId">Optional holder ID filter.</param>
    /// <param name="holderType">Optional holder type filter (Company/Customer).</param>
    /// <param name="status">Optional status filter (Valid/Expired/Suspended/Revoked).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of licences.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LicenceResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLicences(
        [FromQuery] Guid? holderId = null,
        [FromQuery] string? holderType = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Licence> licences;

        if (holderId.HasValue && !string.IsNullOrEmpty(holderType))
        {
            licences = await _licenceService.GetByHolderAsync(holderId.Value, holderType, cancellationToken);
        }
        else
        {
            licences = await _licenceService.GetAllAsync(cancellationToken);
        }

        // Apply status filter if provided
        if (!string.IsNullOrEmpty(status))
        {
            licences = licences.Where(l => l.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        var response = licences.Select(LicenceResponseDto.FromDomainModel);
        return Ok(response);
    }

    /// <summary>
    /// Gets a specific licence by ID.
    /// </summary>
    /// <param name="id">Licence ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The licence details.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LicenceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLicence(Guid id, CancellationToken cancellationToken = default)
    {
        var licence = await _licenceService.GetByIdAsync(id, cancellationToken);

        if (licence == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                Message = $"Licence with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(LicenceResponseDto.FromDomainModel(licence));
    }

    /// <summary>
    /// Gets a licence by licence number.
    /// </summary>
    /// <param name="licenceNumber">The official licence number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The licence details.</returns>
    [HttpGet("by-number/{licenceNumber}")]
    [ProducesResponseType(typeof(LicenceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLicenceByNumber(string licenceNumber, CancellationToken cancellationToken = default)
    {
        var licence = await _licenceService.GetByLicenceNumberAsync(licenceNumber, cancellationToken);

        if (licence == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                Message = $"Licence with number '{licenceNumber}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        return Ok(LicenceResponseDto.FromDomainModel(licence));
    }

    /// <summary>
    /// Gets licences expiring within specified days.
    /// Per FR-007: Generate alerts for expiring licences.
    /// </summary>
    /// <param name="daysAhead">Number of days ahead to check (default: 90).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of expiring licences.</returns>
    [HttpGet("expiring")]
    [ProducesResponseType(typeof(IEnumerable<LicenceResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpiringLicences(
        [FromQuery] int daysAhead = 90,
        CancellationToken cancellationToken = default)
    {
        var licences = await _licenceService.GetExpiringLicencesAsync(daysAhead, cancellationToken);
        var response = licences.Select(LicenceResponseDto.FromDomainModel);
        return Ok(response);
    }

    /// <summary>
    /// Creates a new licence.
    /// T080: Only ComplianceManager role can create licences.
    /// </summary>
    /// <param name="request">Licence creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created licence details.</returns>
    [HttpPost]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(LicenceResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLicence(
        [FromBody] CreateLicenceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var licence = request.ToDomainModel();
        var (id, result) = await _licenceService.CreateAsync(licence, cancellationToken);

        if (!result.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Licence validation failed",
                Details = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Fetch the created licence with LicenceType populated
        var created = await _licenceService.GetByIdAsync(id!.Value, cancellationToken);
        _logger.LogInformation("Created licence {LicenceNumber} with ID {Id}", created!.LicenceNumber, id);

        return CreatedAtAction(
            nameof(GetLicence),
            new { id = id },
            LicenceResponseDto.FromDomainModel(created));
    }

    /// <summary>
    /// Updates an existing licence.
    /// T080: Only ComplianceManager role can modify licences.
    /// </summary>
    /// <param name="id">Licence ID.</param>
    /// <param name="request">Updated licence data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated licence details.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(LicenceResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLicence(
        Guid id,
        [FromBody] UpdateLicenceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var licence = request.ToDomainModel(id);
        var result = await _licenceService.UpdateAsync(licence, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.FirstOrDefault()?.ErrorCode ?? ErrorCodes.VALIDATION_ERROR;

            if (errorCode == ErrorCodes.LICENCE_NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = $"Licence with ID '{id}' not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Licence validation failed",
                Details = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var updated = await _licenceService.GetByIdAsync(id, cancellationToken);
        _logger.LogInformation("Updated licence {Id}", id);

        return Ok(LicenceResponseDto.FromDomainModel(updated!));
    }

    /// <summary>
    /// Deletes a licence (soft delete).
    /// T080: Only ComplianceManager role can delete licences.
    /// </summary>
    /// <param name="id">Licence ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLicence(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _licenceService.DeleteAsync(id, cancellationToken);

        if (!result.IsValid)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                Message = $"Licence with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        _logger.LogInformation("Deleted licence {Id}", id);
        return NoContent();
    }

    #region Document Endpoints (T114)

    /// <summary>
    /// Gets all documents for a licence.
    /// T114: Document listing per FR-008.
    /// </summary>
    /// <param name="id">Licence ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of documents.</returns>
    [HttpGet("{id:guid}/documents")]
    [ProducesResponseType(typeof(IEnumerable<LicenceDocumentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocuments(Guid id, CancellationToken cancellationToken = default)
    {
        var licence = await _licenceService.GetByIdAsync(id, cancellationToken);
        if (licence == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                Message = $"Licence with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var documents = await _licenceService.GetDocumentsAsync(id, cancellationToken);
        var response = documents.Select(LicenceDocumentResponseDto.FromDomainModel);
        return Ok(response);
    }

    /// <summary>
    /// Uploads a document for a licence.
    /// T114: Document upload per FR-008.
    /// </summary>
    /// <param name="id">Licence ID.</param>
    /// <param name="file">The document file.</param>
    /// <param name="documentType">Document type (Certificate, Letter, InspectionReport, Other).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created document details.</returns>
    [HttpPost("{id:guid}/documents")]
    [Authorize(Roles = "ComplianceManager")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(LicenceDocumentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadDocument(
        Guid id,
        IFormFile file,
        [FromForm] int documentType = 0,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "File is required",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Get user ID from claims (default to empty for now)
        var userId = User.FindFirst("sub")?.Value ?? Guid.Empty.ToString();

        var document = new LicenceDocument
        {
            DocumentId = Guid.NewGuid(),
            LicenceId = id,
            DocumentType = (DocumentType)documentType,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            UploadedBy = Guid.TryParse(userId, out var uid) ? uid : Guid.Empty
        };

        using var stream = file.OpenReadStream();
        var (docId, result) = await _licenceService.UploadDocumentAsync(id, document, stream, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.FirstOrDefault()?.ErrorCode ?? ErrorCodes.VALIDATION_ERROR;

            if (errorCode == ErrorCodes.LICENCE_NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = $"Licence with ID '{id}' not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Document upload failed",
                Details = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var created = await _licenceService.GetDocumentByIdAsync(docId!.Value, cancellationToken);
        _logger.LogInformation("Uploaded document {DocumentId} for licence {LicenceId}", docId, id);

        return CreatedAtAction(
            nameof(GetDocuments),
            new { id },
            LicenceDocumentResponseDto.FromDomainModel(created!));
    }

    /// <summary>
    /// Downloads a document.
    /// T114: Document download with SAS URL generation.
    /// </summary>
    /// <param name="id">Licence ID.</param>
    /// <param name="documentId">Document ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Redirect to document download URL.</returns>
    [HttpGet("{id:guid}/documents/{documentId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDocument(
        Guid id,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _licenceService.GetDocumentByIdAsync(documentId, cancellationToken);
        if (document == null || document.LicenceId != id)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Document with ID '{documentId}' not found for licence '{id}'",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Generate SAS URL for download (valid for 1 hour)
        var blobName = $"{document.LicenceId}/{document.DocumentId}/{document.FileName}";
        var sasUri = await _documentStorage.GetDocumentSasUriAsync(
            DocumentContainerName,
            blobName,
            TimeSpan.FromHours(1),
            cancellationToken);

        return Redirect(sasUri.ToString());
    }

    /// <summary>
    /// Deletes a document from a licence.
    /// T114: Document removal per FR-008.
    /// </summary>
    /// <param name="id">Licence ID.</param>
    /// <param name="documentId">Document ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocument(
        Guid id,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _licenceService.GetDocumentByIdAsync(documentId, cancellationToken);
        if (document == null || document.LicenceId != id)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"Document with ID '{documentId}' not found for licence '{id}'",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var result = await _licenceService.DeleteDocumentAsync(documentId, cancellationToken);
        if (!result.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.INTERNAL_ERROR,
                Message = "Failed to delete document",
                Details = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        _logger.LogInformation("Deleted document {DocumentId} from licence {LicenceId}", documentId, id);
        return NoContent();
    }

    #endregion

    #region Verification Endpoints (T115)

    /// <summary>
    /// Gets verification history for a licence.
    /// T115: Verification audit trail per FR-009.
    /// </summary>
    /// <param name="id">Licence ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of verifications.</returns>
    [HttpGet("{id:guid}/verifications")]
    [ProducesResponseType(typeof(IEnumerable<LicenceVerificationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVerifications(Guid id, CancellationToken cancellationToken = default)
    {
        var licence = await _licenceService.GetByIdAsync(id, cancellationToken);
        if (licence == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                Message = $"Licence with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var verifications = await _licenceService.GetVerificationHistoryAsync(id, cancellationToken);
        var response = verifications.Select(LicenceVerificationResponseDto.FromDomainModel);
        return Ok(response);
    }

    /// <summary>
    /// Records a verification for a licence.
    /// T115: Verification recording per FR-009.
    /// </summary>
    /// <param name="id">Licence ID.</param>
    /// <param name="request">Verification details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created verification details.</returns>
    [HttpPost("{id:guid}/verifications")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(LicenceVerificationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordVerification(
        Guid id,
        [FromBody] RecordVerificationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var verification = request.ToDomainModel(id);
        var (verificationId, result) = await _licenceService.RecordVerificationAsync(verification, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.FirstOrDefault()?.ErrorCode ?? ErrorCodes.VALIDATION_ERROR;

            if (errorCode == ErrorCodes.LICENCE_NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = $"Licence with ID '{id}' not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Verification recording failed",
                Details = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Get latest verification (the one we just created)
        var created = await _licenceService.GetLatestVerificationAsync(id, cancellationToken);
        _logger.LogInformation("Recorded verification {VerificationId} for licence {LicenceId}", verificationId, id);

        return CreatedAtAction(
            nameof(GetVerifications),
            new { id },
            LicenceVerificationResponseDto.FromDomainModel(created!));
    }

    #endregion

    #region Scope Change Endpoints (T116)

    /// <summary>
    /// Gets scope change history for a licence.
    /// T116: Scope change audit trail per FR-010.
    /// </summary>
    /// <param name="id">Licence ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of scope changes.</returns>
    [HttpGet("{id:guid}/scope-changes")]
    [ProducesResponseType(typeof(IEnumerable<LicenceScopeChangeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScopeChanges(Guid id, CancellationToken cancellationToken = default)
    {
        var licence = await _licenceService.GetByIdAsync(id, cancellationToken);
        if (licence == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                Message = $"Licence with ID '{id}' not found",
                TraceId = HttpContext.TraceIdentifier
            });
        }

        var scopeChanges = await _licenceService.GetScopeChangesAsync(id, cancellationToken);
        var response = scopeChanges.Select(LicenceScopeChangeResponseDto.FromDomainModel);
        return Ok(response);
    }

    /// <summary>
    /// Records a scope change for a licence.
    /// T116: Scope change recording per FR-010.
    /// </summary>
    /// <param name="id">Licence ID.</param>
    /// <param name="request">Scope change details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created scope change details.</returns>
    [HttpPost("{id:guid}/scope-changes")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(typeof(LicenceScopeChangeResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordScopeChange(
        Guid id,
        [FromBody] RecordScopeChangeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var scopeChange = request.ToDomainModel(id);
        var (changeId, result) = await _licenceService.RecordScopeChangeAsync(scopeChange, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.FirstOrDefault()?.ErrorCode ?? ErrorCodes.VALIDATION_ERROR;

            if (errorCode == ErrorCodes.LICENCE_NOT_FOUND)
            {
                return NotFound(new ErrorResponseDto
                {
                    ErrorCode = errorCode,
                    Message = $"Licence with ID '{id}' not found",
                    TraceId = HttpContext.TraceIdentifier
                });
            }

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Scope change recording failed",
                Details = string.Join("; ", result.Violations.Select(v => v.Message)),
                TraceId = HttpContext.TraceIdentifier
            });
        }

        // Get latest scope change (the one we just created)
        var changes = await _licenceService.GetScopeChangesAsync(id, cancellationToken);
        var created = changes.FirstOrDefault();
        _logger.LogInformation("Recorded scope change {ChangeId} for licence {LicenceId}", changeId, id);

        return CreatedAtAction(
            nameof(GetScopeChanges),
            new { id },
            LicenceScopeChangeResponseDto.FromDomainModel(created!));
    }

    #endregion
}

#region DTOs

/// <summary>
/// Licence response DTO for API responses.
/// </summary>
public class LicenceResponseDto
{
    public Guid LicenceId { get; set; }
    public required string LicenceNumber { get; set; }
    public Guid LicenceTypeId { get; set; }
    public string? LicenceTypeName { get; set; }
    public required string HolderType { get; set; }
    public Guid HolderId { get; set; }
    public required string IssuingAuthority { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public required string Status { get; set; }
    public string? Scope { get; set; }
    public int PermittedActivities { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public bool IsExpired { get; set; }

    public static LicenceResponseDto FromDomainModel(Licence licence)
    {
        return new LicenceResponseDto
        {
            LicenceId = licence.LicenceId,
            LicenceNumber = licence.LicenceNumber,
            LicenceTypeId = licence.LicenceTypeId,
            LicenceTypeName = licence.LicenceType?.Name,
            HolderType = licence.HolderType,
            HolderId = licence.HolderId,
            IssuingAuthority = licence.IssuingAuthority,
            IssueDate = licence.IssueDate,
            ExpiryDate = licence.ExpiryDate,
            Status = licence.Status,
            Scope = licence.Scope,
            PermittedActivities = (int)licence.PermittedActivities,
            CreatedDate = licence.CreatedDate,
            ModifiedDate = licence.ModifiedDate,
            IsExpired = licence.IsExpired()
        };
    }
}

/// <summary>
/// Request DTO for creating a new licence.
/// </summary>
public class CreateLicenceRequestDto
{
    public required string LicenceNumber { get; set; }
    public Guid LicenceTypeId { get; set; }
    public required string HolderType { get; set; }
    public Guid HolderId { get; set; }
    public required string IssuingAuthority { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string Status { get; set; } = "Valid";
    public string? Scope { get; set; }
    public int PermittedActivities { get; set; }

    public Licence ToDomainModel()
    {
        return new Licence
        {
            LicenceId = Guid.NewGuid(),
            LicenceNumber = LicenceNumber,
            LicenceTypeId = LicenceTypeId,
            HolderType = HolderType,
            HolderId = HolderId,
            IssuingAuthority = IssuingAuthority,
            IssueDate = IssueDate,
            ExpiryDate = ExpiryDate,
            Status = Status,
            Scope = Scope,
            PermittedActivities = (LicenceTypes.PermittedActivity)PermittedActivities
        };
    }
}

/// <summary>
/// Request DTO for updating a licence.
/// </summary>
public class UpdateLicenceRequestDto
{
    public required string LicenceNumber { get; set; }
    public Guid LicenceTypeId { get; set; }
    public required string HolderType { get; set; }
    public Guid HolderId { get; set; }
    public required string IssuingAuthority { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public required string Status { get; set; }
    public string? Scope { get; set; }
    public int PermittedActivities { get; set; }

    public Licence ToDomainModel(Guid licenceId)
    {
        return new Licence
        {
            LicenceId = licenceId,
            LicenceNumber = LicenceNumber,
            LicenceTypeId = LicenceTypeId,
            HolderType = HolderType,
            HolderId = HolderId,
            IssuingAuthority = IssuingAuthority,
            IssueDate = IssueDate,
            ExpiryDate = ExpiryDate,
            Status = Status,
            Scope = Scope,
            PermittedActivities = (LicenceTypes.PermittedActivity)PermittedActivities
        };
    }
}

/// <summary>
/// Licence document response DTO for API responses.
/// T114: Document API responses.
/// </summary>
public class LicenceDocumentResponseDto
{
    public Guid DocumentId { get; set; }
    public Guid LicenceId { get; set; }
    public int DocumentType { get; set; }
    public string DocumentTypeName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedDate { get; set; }
    public Guid UploadedBy { get; set; }
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }

    public static LicenceDocumentResponseDto FromDomainModel(LicenceDocument document)
    {
        return new LicenceDocumentResponseDto
        {
            DocumentId = document.DocumentId,
            LicenceId = document.LicenceId,
            DocumentType = (int)document.DocumentType,
            DocumentTypeName = document.DocumentType.ToString(),
            FileName = document.FileName,
            UploadedDate = document.UploadedDate,
            UploadedBy = document.UploadedBy,
            ContentType = document.ContentType,
            FileSizeBytes = document.FileSizeBytes
        };
    }
}

/// <summary>
/// Licence verification response DTO for API responses.
/// T115: Verification API responses.
/// </summary>
public class LicenceVerificationResponseDto
{
    public Guid VerificationId { get; set; }
    public Guid LicenceId { get; set; }
    public int VerificationMethod { get; set; }
    public string VerificationMethodName { get; set; } = string.Empty;
    public DateOnly VerificationDate { get; set; }
    public Guid VerifiedBy { get; set; }
    public string? VerifierName { get; set; }
    public int Outcome { get; set; }
    public string OutcomeName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? AuthorityReferenceNumber { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsCurrent { get; set; }

    public static LicenceVerificationResponseDto FromDomainModel(LicenceVerification verification)
    {
        return new LicenceVerificationResponseDto
        {
            VerificationId = verification.VerificationId,
            LicenceId = verification.LicenceId,
            VerificationMethod = (int)verification.VerificationMethod,
            VerificationMethodName = verification.VerificationMethod.ToString(),
            VerificationDate = verification.VerificationDate,
            VerifiedBy = verification.VerifiedBy,
            VerifierName = verification.VerifierName,
            Outcome = (int)verification.Outcome,
            OutcomeName = verification.Outcome.ToString(),
            Notes = verification.Notes,
            AuthorityReferenceNumber = verification.AuthorityReferenceNumber,
            CreatedDate = verification.CreatedDate,
            IsCurrent = verification.IsCurrent()
        };
    }
}

/// <summary>
/// Request DTO for recording a licence verification.
/// T115: Verification recording request.
/// </summary>
public class RecordVerificationRequestDto
{
    public int VerificationMethod { get; set; }
    public DateOnly VerificationDate { get; set; }
    public Guid VerifiedBy { get; set; }
    public string? VerifierName { get; set; }
    public int Outcome { get; set; }
    public string? Notes { get; set; }
    public string? AuthorityReferenceNumber { get; set; }

    public LicenceVerification ToDomainModel(Guid licenceId)
    {
        return new LicenceVerification
        {
            VerificationId = Guid.NewGuid(),
            LicenceId = licenceId,
            VerificationMethod = (VerificationMethod)VerificationMethod,
            VerificationDate = VerificationDate,
            VerifiedBy = VerifiedBy,
            VerifierName = VerifierName,
            Outcome = (VerificationOutcome)Outcome,
            Notes = Notes,
            AuthorityReferenceNumber = AuthorityReferenceNumber
        };
    }
}

/// <summary>
/// Licence scope change response DTO for API responses.
/// T116: Scope change API responses.
/// </summary>
public class LicenceScopeChangeResponseDto
{
    public Guid ChangeId { get; set; }
    public Guid LicenceId { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string ChangeDescription { get; set; } = string.Empty;
    public int ChangeType { get; set; }
    public string ChangeTypeName { get; set; } = string.Empty;
    public Guid RecordedBy { get; set; }
    public string? RecorderName { get; set; }
    public DateTime RecordedDate { get; set; }
    public Guid? SupportingDocumentId { get; set; }
    public string? SubstancesAdded { get; set; }
    public string? SubstancesRemoved { get; set; }
    public string? ActivitiesAdded { get; set; }
    public string? ActivitiesRemoved { get; set; }

    public static LicenceScopeChangeResponseDto FromDomainModel(LicenceScopeChange scopeChange)
    {
        return new LicenceScopeChangeResponseDto
        {
            ChangeId = scopeChange.ChangeId,
            LicenceId = scopeChange.LicenceId,
            EffectiveDate = scopeChange.EffectiveDate,
            ChangeDescription = scopeChange.ChangeDescription,
            ChangeType = (int)scopeChange.ChangeType,
            ChangeTypeName = scopeChange.ChangeType.ToString(),
            RecordedBy = scopeChange.RecordedBy,
            RecorderName = scopeChange.RecorderName,
            RecordedDate = scopeChange.RecordedDate,
            SupportingDocumentId = scopeChange.SupportingDocumentId,
            SubstancesAdded = scopeChange.SubstancesAdded,
            SubstancesRemoved = scopeChange.SubstancesRemoved,
            ActivitiesAdded = scopeChange.ActivitiesAdded,
            ActivitiesRemoved = scopeChange.ActivitiesRemoved
        };
    }
}

/// <summary>
/// Request DTO for recording a licence scope change.
/// T116: Scope change recording request.
/// </summary>
public class RecordScopeChangeRequestDto
{
    public DateOnly EffectiveDate { get; set; }
    public required string ChangeDescription { get; set; }
    public int ChangeType { get; set; }
    public Guid RecordedBy { get; set; }
    public string? RecorderName { get; set; }
    public Guid? SupportingDocumentId { get; set; }
    public string? SubstancesAdded { get; set; }
    public string? SubstancesRemoved { get; set; }
    public string? ActivitiesAdded { get; set; }
    public string? ActivitiesRemoved { get; set; }

    public LicenceScopeChange ToDomainModel(Guid licenceId)
    {
        return new LicenceScopeChange
        {
            ChangeId = Guid.NewGuid(),
            LicenceId = licenceId,
            EffectiveDate = EffectiveDate,
            ChangeDescription = ChangeDescription,
            ChangeType = (ScopeChangeType)ChangeType,
            RecordedBy = RecordedBy,
            RecorderName = RecorderName,
            SupportingDocumentId = SupportingDocumentId,
            SubstancesAdded = SubstancesAdded,
            SubstancesRemoved = SubstancesRemoved,
            ActivitiesAdded = ActivitiesAdded,
            ActivitiesRemoved = ActivitiesRemoved
        };
    }
}

#endregion
