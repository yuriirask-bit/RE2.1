using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using RE2.Shared.Models;

namespace RE2.ComplianceApi.Controllers.V1;

/// <summary>
/// GDP service providers and credentials API endpoints.
/// T206: REST API for GDP partner qualification per User Story 8 (FR-036, FR-037, FR-038, FR-039).
/// </summary>
[ApiController]
[Route("api/v1/gdp-providers")]
[Authorize]
public class GdpProvidersController : ControllerBase
{
    private readonly IGdpComplianceService _gdpService;
    private readonly ILogger<GdpProvidersController> _logger;

    public GdpProvidersController(IGdpComplianceService gdpService, ILogger<GdpProvidersController> logger)
    {
        _gdpService = gdpService;
        _logger = logger;
    }

    #region Service Providers

    /// <summary>
    /// Gets all GDP service providers.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GdpProviderResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviders(CancellationToken cancellationToken = default)
    {
        var providers = await _gdpService.GetAllProvidersAsync(cancellationToken);
        return Ok(providers.Select(GdpProviderResponseDto.FromDomain));
    }

    /// <summary>
    /// Gets a specific GDP service provider.
    /// </summary>
    [HttpGet("{providerId:guid}")]
    [ProducesResponseType(typeof(GdpProviderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProvider(Guid providerId, CancellationToken cancellationToken = default)
    {
        var provider = await _gdpService.GetProviderAsync(providerId, cancellationToken);
        if (provider == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"GDP service provider '{providerId}' not found"
            });
        }

        return Ok(GdpProviderResponseDto.FromDomain(provider));
    }

    /// <summary>
    /// Creates a new GDP service provider.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(typeof(GdpProviderResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProvider([FromBody] CreateGdpProviderRequestDto request, CancellationToken cancellationToken = default)
    {
        var provider = request.ToDomain();
        var (id, result) = await _gdpService.CreateProviderAsync(provider, cancellationToken);

        if (!result.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        var created = await _gdpService.GetProviderAsync(id!.Value, cancellationToken);
        return CreatedAtAction(nameof(GetProvider), new { providerId = id }, GdpProviderResponseDto.FromDomain(created!));
    }

    /// <summary>
    /// Updates an existing GDP service provider.
    /// </summary>
    [HttpPut("{providerId:guid}")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(typeof(GdpProviderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProvider(Guid providerId, [FromBody] CreateGdpProviderRequestDto request, CancellationToken cancellationToken = default)
    {
        var provider = request.ToDomain();
        provider.ProviderId = providerId;

        var result = await _gdpService.UpdateProviderAsync(provider, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.First().ErrorCode;
            if (errorCode == ErrorCodes.NOT_FOUND)
                return NotFound(new ErrorResponseDto { ErrorCode = errorCode, Message = result.Violations.First().Message });

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        var updated = await _gdpService.GetProviderAsync(providerId, cancellationToken);
        return Ok(GdpProviderResponseDto.FromDomain(updated!));
    }

    /// <summary>
    /// Deletes a GDP service provider.
    /// </summary>
    [HttpDelete("{providerId:guid}")]
    [Authorize(Roles = "ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProvider(Guid providerId, CancellationToken cancellationToken = default)
    {
        var result = await _gdpService.DeleteProviderAsync(providerId, cancellationToken);
        if (!result.IsValid)
            return NotFound(new ErrorResponseDto { ErrorCode = ErrorCodes.NOT_FOUND, Message = result.Violations.First().Message });

        return NoContent();
    }

    /// <summary>
    /// Gets providers requiring re-qualification review.
    /// Per FR-039.
    /// </summary>
    [HttpGet("requiring-review")]
    [ProducesResponseType(typeof(IEnumerable<GdpProviderResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProvidersRequiringReview(CancellationToken cancellationToken = default)
    {
        var providers = await _gdpService.GetProvidersRequiringReviewAsync(cancellationToken);
        return Ok(providers.Select(GdpProviderResponseDto.FromDomain));
    }

    #endregion

    #region Credentials

    /// <summary>
    /// Gets GDP credentials for a provider.
    /// </summary>
    [HttpGet("{providerId:guid}/credentials")]
    [ProducesResponseType(typeof(IEnumerable<GdpCredentialResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviderCredentials(Guid providerId, CancellationToken cancellationToken = default)
    {
        var credentials = await _gdpService.GetCredentialsByEntityAsync(GdpCredentialEntityType.ServiceProvider, providerId, cancellationToken);
        return Ok(credentials.Select(GdpCredentialResponseDto.FromDomain));
    }

    /// <summary>
    /// Gets a specific GDP credential.
    /// </summary>
    [HttpGet("credentials/{credentialId:guid}")]
    [ProducesResponseType(typeof(GdpCredentialResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCredential(Guid credentialId, CancellationToken cancellationToken = default)
    {
        var credential = await _gdpService.GetCredentialAsync(credentialId, cancellationToken);
        if (credential == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"GDP credential '{credentialId}' not found"
            });
        }

        return Ok(GdpCredentialResponseDto.FromDomain(credential));
    }

    /// <summary>
    /// Creates a new GDP credential.
    /// </summary>
    [HttpPost("credentials")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(typeof(GdpCredentialResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCredential([FromBody] CreateGdpCredentialRequestDto request, CancellationToken cancellationToken = default)
    {
        var credential = request.ToDomain();
        var (id, result) = await _gdpService.CreateCredentialAsync(credential, cancellationToken);

        if (!result.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        var created = await _gdpService.GetCredentialAsync(id!.Value, cancellationToken);
        return CreatedAtAction(nameof(GetCredential), new { credentialId = id }, GdpCredentialResponseDto.FromDomain(created!));
    }

    /// <summary>
    /// Gets credentials expiring within specified days.
    /// </summary>
    [HttpGet("credentials/expiring")]
    [ProducesResponseType(typeof(IEnumerable<GdpCredentialResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExpiringCredentials([FromQuery] int daysAhead = 90, CancellationToken cancellationToken = default)
    {
        var credentials = await _gdpService.GetCredentialsExpiringAsync(daysAhead, cancellationToken);
        return Ok(credentials.Select(GdpCredentialResponseDto.FromDomain));
    }

    #endregion

    #region Qualification Reviews

    /// <summary>
    /// Gets qualification reviews for a provider.
    /// </summary>
    [HttpGet("{providerId:guid}/reviews")]
    [ProducesResponseType(typeof(IEnumerable<QualificationReviewResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviderReviews(Guid providerId, CancellationToken cancellationToken = default)
    {
        var reviews = await _gdpService.GetReviewsByEntityAsync(ReviewEntityType.ServiceProvider, providerId, cancellationToken);
        return Ok(reviews.Select(QualificationReviewResponseDto.FromDomain));
    }

    /// <summary>
    /// Records a qualification review for a provider.
    /// Per FR-038.
    /// </summary>
    [HttpPost("{providerId:guid}/reviews")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(typeof(QualificationReviewResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordReview(Guid providerId, [FromBody] CreateReviewRequestDto request, CancellationToken cancellationToken = default)
    {
        var review = request.ToDomain(ReviewEntityType.ServiceProvider, providerId);
        var (id, result) = await _gdpService.RecordReviewAsync(review, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.First().ErrorCode;
            if (errorCode == ErrorCodes.NOT_FOUND)
                return NotFound(new ErrorResponseDto { ErrorCode = errorCode, Message = result.Violations.First().Message });

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        return CreatedAtAction(nameof(GetProviderReviews), new { providerId }, QualificationReviewResponseDto.FromDomain(review));
    }

    #endregion

    #region Verifications

    /// <summary>
    /// Gets verifications for a credential.
    /// </summary>
    [HttpGet("credentials/{credentialId:guid}/verifications")]
    [ProducesResponseType(typeof(IEnumerable<CredentialVerificationResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVerifications(Guid credentialId, CancellationToken cancellationToken = default)
    {
        var verifications = await _gdpService.GetVerificationsByCredentialAsync(credentialId, cancellationToken);
        return Ok(verifications.Select(CredentialVerificationResponseDto.FromDomain));
    }

    /// <summary>
    /// Records a credential verification (e.g., EudraGMDP check).
    /// Per FR-045.
    /// </summary>
    [HttpPost("credentials/{credentialId:guid}/verifications")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(typeof(CredentialVerificationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordVerification(Guid credentialId, [FromBody] CreateVerificationRequestDto request, CancellationToken cancellationToken = default)
    {
        var verification = request.ToDomain(credentialId);
        var (id, result) = await _gdpService.RecordVerificationAsync(verification, cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.First().ErrorCode;
            if (errorCode == ErrorCodes.NOT_FOUND)
                return NotFound(new ErrorResponseDto { ErrorCode = errorCode, Message = result.Violations.First().Message });

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        return CreatedAtAction(nameof(GetVerifications), new { credentialId }, CredentialVerificationResponseDto.FromDomain(verification));
    }

    #endregion

    #region Documents (T243)

    /// <summary>
    /// Gets documents for a credential.
    /// Per FR-044.
    /// </summary>
    [HttpGet("credentials/{credentialId:guid}/documents")]
    [ProducesResponseType(typeof(IEnumerable<GdpDocumentResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCredentialDocuments(Guid credentialId, CancellationToken cancellationToken = default)
    {
        var documents = await _gdpService.GetDocumentsByEntityAsync(GdpDocumentEntityType.Credential, credentialId, cancellationToken);
        return Ok(documents.Select(GdpDocumentResponseDto.FromDomain));
    }

    /// <summary>
    /// Gets a specific GDP document metadata.
    /// </summary>
    [HttpGet("documents/{documentId:guid}")]
    [ProducesResponseType(typeof(GdpDocumentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocument(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _gdpService.GetDocumentAsync(documentId, cancellationToken);
        if (document == null)
        {
            return NotFound(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.NOT_FOUND,
                Message = $"GDP document '{documentId}' not found"
            });
        }

        return Ok(GdpDocumentResponseDto.FromDomain(document));
    }

    /// <summary>
    /// Uploads a document for a credential.
    /// Per FR-044. Accepts multipart/form-data.
    /// </summary>
    [HttpPost("credentials/{credentialId:guid}/documents")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(typeof(GdpDocumentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<IActionResult> UploadDocument(
        Guid credentialId,
        [FromForm] UploadDocumentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "File is required"
            });
        }

        var document = new GdpDocument
        {
            DocumentId = Guid.NewGuid(),
            OwnerEntityType = GdpDocumentEntityType.Credential,
            OwnerEntityId = credentialId,
            DocumentType = Enum.Parse<DocumentType>(request.DocumentType, true),
            FileName = request.File.FileName,
            UploadedBy = User.Identity?.Name ?? "Unknown",
            ContentType = request.File.ContentType,
            FileSizeBytes = request.File.Length,
            Description = request.Description
        };

        using var stream = request.File.OpenReadStream();
        var (id, result) = await _gdpService.UploadDocumentAsync(document, stream, cancellationToken);

        if (!result.IsValid)
        {
            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = string.Join("; ", result.Violations.Select(v => v.Message))
            });
        }

        var created = await _gdpService.GetDocumentAsync(id!.Value, cancellationToken);
        return CreatedAtAction(nameof(GetDocument), new { documentId = id }, GdpDocumentResponseDto.FromDomain(created!));
    }

    /// <summary>
    /// Gets a temporary download URL for a GDP document.
    /// </summary>
    [HttpGet("documents/{documentId:guid}/download")]
    [ProducesResponseType(typeof(DocumentDownloadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDocument(Guid documentId, CancellationToken cancellationToken = default)
    {
        var (url, result) = await _gdpService.GetDocumentDownloadUrlAsync(documentId, cancellationToken: cancellationToken);

        if (!result.IsValid)
        {
            var errorCode = result.Violations.First().ErrorCode;
            if (errorCode == ErrorCodes.NOT_FOUND)
                return NotFound(new ErrorResponseDto { ErrorCode = errorCode, Message = result.Violations.First().Message });

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.INTERNAL_ERROR,
                Message = result.Violations.First().Message
            });
        }

        return Ok(new DocumentDownloadResponseDto
        {
            DocumentId = documentId,
            DownloadUrl = url!.ToString(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15).ToString("O")
        });
    }

    /// <summary>
    /// Deletes a GDP document.
    /// </summary>
    [HttpDelete("documents/{documentId:guid}")]
    [Authorize(Roles = "QAUser,ComplianceManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocument(Guid documentId, CancellationToken cancellationToken = default)
    {
        var result = await _gdpService.DeleteDocumentAsync(documentId, cancellationToken);
        if (!result.IsValid)
        {
            var errorCode = result.Violations.First().ErrorCode;
            if (errorCode == ErrorCodes.NOT_FOUND)
                return NotFound(new ErrorResponseDto { ErrorCode = errorCode, Message = result.Violations.First().Message });

            return BadRequest(new ErrorResponseDto
            {
                ErrorCode = ErrorCodes.INTERNAL_ERROR,
                Message = result.Violations.First().Message
            });
        }

        return NoContent();
    }

    #endregion

    #region Partner Qualification Check

    /// <summary>
    /// Checks if a partner is GDP-qualified for transactions.
    /// Per FR-038.
    /// </summary>
    [HttpGet("check-qualification")]
    [ProducesResponseType(typeof(PartnerQualificationCheckDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckPartnerQualification(
        [FromQuery] string entityType,
        [FromQuery] Guid entityId,
        CancellationToken cancellationToken = default)
    {
        var parsedType = Enum.Parse<GdpCredentialEntityType>(entityType, true);
        var isQualified = await _gdpService.IsPartnerQualifiedAsync(parsedType, entityId, cancellationToken);

        return Ok(new PartnerQualificationCheckDto
        {
            EntityType = entityType,
            EntityId = entityId,
            IsQualified = isQualified
        });
    }

    #endregion
}

#region DTOs

public class GdpProviderResponseDto
{
    public Guid ProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public bool TemperatureControlledCapability { get; set; }
    public string? ApprovedRoutes { get; set; }
    public string QualificationStatus { get; set; } = string.Empty;
    public int ReviewFrequencyMonths { get; set; }
    public string? LastReviewDate { get; set; }
    public string? NextReviewDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsApproved { get; set; }
    public bool CanBeSelected { get; set; }
    public bool IsReviewDue { get; set; }

    public static GdpProviderResponseDto FromDomain(GdpServiceProvider provider) => new()
    {
        ProviderId = provider.ProviderId,
        ProviderName = provider.ProviderName,
        ServiceType = provider.ServiceType.ToString(),
        TemperatureControlledCapability = provider.TemperatureControlledCapability,
        ApprovedRoutes = provider.ApprovedRoutes,
        QualificationStatus = provider.QualificationStatus.ToString(),
        ReviewFrequencyMonths = provider.ReviewFrequencyMonths,
        LastReviewDate = provider.LastReviewDate?.ToString("yyyy-MM-dd"),
        NextReviewDate = provider.NextReviewDate?.ToString("yyyy-MM-dd"),
        IsActive = provider.IsActive,
        IsApproved = provider.IsApproved(),
        CanBeSelected = provider.CanBeSelected(),
        IsReviewDue = provider.IsReviewDue()
    };
}

public class CreateGdpProviderRequestDto
{
    public required string ProviderName { get; set; }
    public required string ServiceType { get; set; }
    public bool TemperatureControlledCapability { get; set; }
    public string? ApprovedRoutes { get; set; }
    public int ReviewFrequencyMonths { get; set; }
    public bool IsActive { get; set; } = true;

    public GdpServiceProvider ToDomain() => new()
    {
        ProviderName = ProviderName,
        ServiceType = Enum.Parse<GdpServiceType>(ServiceType, true),
        TemperatureControlledCapability = TemperatureControlledCapability,
        ApprovedRoutes = ApprovedRoutes,
        ReviewFrequencyMonths = ReviewFrequencyMonths,
        IsActive = IsActive
    };
}

public class GdpCredentialResponseDto
{
    public Guid CredentialId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? WdaNumber { get; set; }
    public string? GdpCertificateNumber { get; set; }
    public string? EudraGmdpEntryUrl { get; set; }
    public string? ValidityStartDate { get; set; }
    public string? ValidityEndDate { get; set; }
    public string QualificationStatus { get; set; } = string.Empty;
    public string? LastVerificationDate { get; set; }
    public string? NextReviewDate { get; set; }
    public bool IsValid { get; set; }
    public bool IsApproved { get; set; }

    public static GdpCredentialResponseDto FromDomain(GdpCredential credential) => new()
    {
        CredentialId = credential.CredentialId,
        EntityType = credential.EntityType.ToString(),
        EntityId = credential.EntityId,
        WdaNumber = credential.WdaNumber,
        GdpCertificateNumber = credential.GdpCertificateNumber,
        EudraGmdpEntryUrl = credential.EudraGmdpEntryUrl,
        ValidityStartDate = credential.ValidityStartDate?.ToString("yyyy-MM-dd"),
        ValidityEndDate = credential.ValidityEndDate?.ToString("yyyy-MM-dd"),
        QualificationStatus = credential.QualificationStatus.ToString(),
        LastVerificationDate = credential.LastVerificationDate?.ToString("yyyy-MM-dd"),
        NextReviewDate = credential.NextReviewDate?.ToString("yyyy-MM-dd"),
        IsValid = credential.IsValid(),
        IsApproved = credential.IsApproved()
    };
}

public class CreateGdpCredentialRequestDto
{
    public required string EntityType { get; set; }
    public required Guid EntityId { get; set; }
    public string? WdaNumber { get; set; }
    public string? GdpCertificateNumber { get; set; }
    public string? EudraGmdpEntryUrl { get; set; }
    public string? ValidityStartDate { get; set; }
    public string? ValidityEndDate { get; set; }

    public GdpCredential ToDomain() => new()
    {
        EntityType = Enum.Parse<GdpCredentialEntityType>(EntityType, true),
        EntityId = EntityId,
        WdaNumber = WdaNumber,
        GdpCertificateNumber = GdpCertificateNumber,
        EudraGmdpEntryUrl = EudraGmdpEntryUrl,
        ValidityStartDate = string.IsNullOrEmpty(ValidityStartDate) ? null : DateOnly.Parse(ValidityStartDate),
        ValidityEndDate = string.IsNullOrEmpty(ValidityEndDate) ? null : DateOnly.Parse(ValidityEndDate)
    };
}

public class QualificationReviewResponseDto
{
    public Guid ReviewId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string ReviewDate { get; set; } = string.Empty;
    public string ReviewMethod { get; set; } = string.Empty;
    public string ReviewOutcome { get; set; } = string.Empty;
    public string ReviewerName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? NextReviewDate { get; set; }
    public bool IsApproved { get; set; }

    public static QualificationReviewResponseDto FromDomain(QualificationReview review) => new()
    {
        ReviewId = review.ReviewId,
        EntityType = review.EntityType.ToString(),
        EntityId = review.EntityId,
        ReviewDate = review.ReviewDate.ToString("yyyy-MM-dd"),
        ReviewMethod = review.ReviewMethod.ToString(),
        ReviewOutcome = review.ReviewOutcome.ToString(),
        ReviewerName = review.ReviewerName,
        Notes = review.Notes,
        NextReviewDate = review.NextReviewDate?.ToString("yyyy-MM-dd"),
        IsApproved = review.IsApproved
    };
}

public class CreateReviewRequestDto
{
    public required string ReviewDate { get; set; }
    public required string ReviewMethod { get; set; }
    public required string ReviewOutcome { get; set; }
    public required string ReviewerName { get; set; }
    public string? Notes { get; set; }
    public int? NextReviewMonths { get; set; }

    public QualificationReview ToDomain(ReviewEntityType entityType, Guid entityId)
    {
        var review = new QualificationReview
        {
            ReviewId = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            ReviewDate = DateOnly.Parse(ReviewDate),
            ReviewMethod = Enum.Parse<ReviewMethod>(ReviewMethod, true),
            ReviewOutcome = Enum.Parse<ReviewOutcome>(ReviewOutcome, true),
            ReviewerName = ReviewerName,
            Notes = Notes
        };

        if (NextReviewMonths.HasValue)
            review.SetNextReviewDate(NextReviewMonths.Value);

        return review;
    }
}

public class CredentialVerificationResponseDto
{
    public Guid VerificationId { get; set; }
    public Guid CredentialId { get; set; }
    public string VerificationDate { get; set; } = string.Empty;
    public string VerificationMethod { get; set; } = string.Empty;
    public string VerifiedBy { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public static CredentialVerificationResponseDto FromDomain(GdpCredentialVerification v) => new()
    {
        VerificationId = v.VerificationId,
        CredentialId = v.CredentialId,
        VerificationDate = v.VerificationDate.ToString("yyyy-MM-dd"),
        VerificationMethod = v.VerificationMethod.ToString(),
        VerifiedBy = v.VerifiedBy,
        Outcome = v.Outcome.ToString(),
        Notes = v.Notes
    };
}

public class CreateVerificationRequestDto
{
    public required string VerificationDate { get; set; }
    public required string VerificationMethod { get; set; }
    public required string VerifiedBy { get; set; }
    public required string Outcome { get; set; }
    public string? Notes { get; set; }

    public GdpCredentialVerification ToDomain(Guid credentialId) => new()
    {
        CredentialId = credentialId,
        VerificationDate = DateOnly.Parse(VerificationDate),
        VerificationMethod = Enum.Parse<GdpVerificationMethod>(VerificationMethod, true),
        VerifiedBy = VerifiedBy,
        Outcome = Enum.Parse<GdpVerificationOutcome>(Outcome, true),
        Notes = Notes
    };
}

public class PartnerQualificationCheckDto
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public bool IsQualified { get; set; }
}

public class GdpDocumentResponseDto
{
    public Guid DocumentId { get; set; }
    public string OwnerEntityType { get; set; } = string.Empty;
    public Guid OwnerEntityId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string BlobStorageUrl { get; set; } = string.Empty;
    public string UploadedDate { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? Description { get; set; }

    public static GdpDocumentResponseDto FromDomain(GdpDocument document) => new()
    {
        DocumentId = document.DocumentId,
        OwnerEntityType = document.OwnerEntityType.ToString(),
        OwnerEntityId = document.OwnerEntityId,
        DocumentType = document.DocumentType.ToString(),
        FileName = document.FileName,
        BlobStorageUrl = document.BlobStorageUrl,
        UploadedDate = document.UploadedDate.ToString("O"),
        UploadedBy = document.UploadedBy,
        ContentType = document.ContentType,
        FileSizeBytes = document.FileSizeBytes,
        Description = document.Description
    };
}

public class UploadDocumentRequestDto
{
    public required string DocumentType { get; set; }
    public string? Description { get; set; }
    public required IFormFile File { get; set; }
}

public class DocumentDownloadResponseDto
{
    public Guid DocumentId { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public string ExpiresAt { get; set; } = string.Empty;
}

#endregion
