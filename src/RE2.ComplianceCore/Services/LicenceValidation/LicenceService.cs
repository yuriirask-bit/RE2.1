using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Services.LicenceValidation;

/// <summary>
/// Service for licence management business logic.
/// T074: Business logic for licence management including validation, expiry checking, and CRUD operations.
/// </summary>
public class LicenceService : ILicenceService
{
    private readonly ILicenceRepository _licenceRepository;
    private readonly ILicenceTypeRepository _licenceTypeRepository;
    private readonly IControlledSubstanceRepository _substanceRepository;
    private readonly ILogger<LicenceService> _logger;

    public LicenceService(
        ILicenceRepository licenceRepository,
        ILicenceTypeRepository licenceTypeRepository,
        IControlledSubstanceRepository substanceRepository,
        ILogger<LicenceService> logger)
    {
        _licenceRepository = licenceRepository;
        _licenceTypeRepository = licenceTypeRepository;
        _substanceRepository = substanceRepository;
        _logger = logger;
    }

    /// <summary>
    /// Gets a licence by ID with LicenceType populated.
    /// </summary>
    public async Task<Licence?> GetByIdAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        var licence = await _licenceRepository.GetByIdAsync(licenceId, cancellationToken);
        if (licence != null)
        {
            licence.UpdateStatus(); // Auto-update expired status
            await PopulateLicenceTypeAsync(licence, cancellationToken);
        }
        return licence;
    }

    /// <summary>
    /// Populates the LicenceType navigation property for a licence.
    /// </summary>
    private async Task PopulateLicenceTypeAsync(Licence licence, CancellationToken cancellationToken)
    {
        if (licence.LicenceType == null && licence.LicenceTypeId != Guid.Empty)
        {
            licence.LicenceType = await _licenceTypeRepository.GetByIdAsync(licence.LicenceTypeId, cancellationToken);
        }
    }

    /// <summary>
    /// Populates the LicenceType navigation property for multiple licences.
    /// </summary>
    private async Task PopulateLicenceTypesAsync(IEnumerable<Licence> licences, CancellationToken cancellationToken)
    {
        // Get all unique licence type IDs
        var typeIds = licences.Select(l => l.LicenceTypeId).Distinct().ToList();

        // Load all licence types in one batch
        var allTypes = await _licenceTypeRepository.GetAllAsync(cancellationToken);
        var typesDict = allTypes.ToDictionary(t => t.LicenceTypeId);

        // Populate navigation properties
        foreach (var licence in licences)
        {
            if (typesDict.TryGetValue(licence.LicenceTypeId, out var licenceType))
            {
                licence.LicenceType = licenceType;
            }
        }
    }

    /// <summary>
    /// Gets a licence by licence number with LicenceType populated.
    /// </summary>
    public async Task<Licence?> GetByLicenceNumberAsync(string licenceNumber, CancellationToken cancellationToken = default)
    {
        var licence = await _licenceRepository.GetByLicenceNumberAsync(licenceNumber, cancellationToken);
        if (licence != null)
        {
            licence.UpdateStatus();
            await PopulateLicenceTypeAsync(licence, cancellationToken);
        }
        return licence;
    }

    /// <summary>
    /// Gets all licences for a holder (Company or Customer) with LicenceType populated.
    /// </summary>
    public async Task<IEnumerable<Licence>> GetByHolderAsync(Guid holderId, string holderType, CancellationToken cancellationToken = default)
    {
        var licences = (await _licenceRepository.GetByHolderAsync(holderId, holderType, cancellationToken)).ToList();
        foreach (var licence in licences)
        {
            licence.UpdateStatus();
        }
        await PopulateLicenceTypesAsync(licences, cancellationToken);
        return licences;
    }

    /// <summary>
    /// Gets all licences expiring within specified days with LicenceType populated.
    /// Per FR-007: Generate alerts for licences expiring within configurable period (default: 90 days).
    /// </summary>
    public async Task<IEnumerable<Licence>> GetExpiringLicencesAsync(int daysAhead = 90, CancellationToken cancellationToken = default)
    {
        var licences = (await _licenceRepository.GetExpiringLicencesAsync(daysAhead, cancellationToken)).ToList();
        await PopulateLicenceTypesAsync(licences, cancellationToken);
        return licences;
    }

    /// <summary>
    /// Gets all licences with LicenceType populated.
    /// </summary>
    public async Task<IEnumerable<Licence>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var licences = (await _licenceRepository.GetAllAsync(cancellationToken)).ToList();
        foreach (var licence in licences)
        {
            licence.UpdateStatus();
        }
        await PopulateLicenceTypesAsync(licences, cancellationToken);
        return licences;
    }

    /// <summary>
    /// Creates a new licence after validation.
    /// </summary>
    public async Task<(Guid? Id, ValidationResult Result)> CreateAsync(Licence licence, CancellationToken cancellationToken = default)
    {
        // Validate the licence
        var validationResult = await ValidateLicenceAsync(licence, cancellationToken);
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Check for duplicate licence number
        var existing = await _licenceRepository.GetByLicenceNumberAsync(licence.LicenceNumber, cancellationToken);
        if (existing != null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = $"Licence with number '{licence.LicenceNumber}' already exists"
                }
            }));
        }

        // Set timestamps
        licence.CreatedDate = DateTime.UtcNow;
        licence.ModifiedDate = DateTime.UtcNow;

        var id = await _licenceRepository.CreateAsync(licence, cancellationToken);
        _logger.LogInformation("Created licence {LicenceNumber} with ID {Id}", licence.LicenceNumber, id);

        return (id, ValidationResult.Success());
    }

    /// <summary>
    /// Updates an existing licence after validation.
    /// </summary>
    public async Task<ValidationResult> UpdateAsync(Licence licence, CancellationToken cancellationToken = default)
    {
        // Check licence exists
        var existing = await _licenceRepository.GetByIdAsync(licence.LicenceId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                    Message = $"Licence with ID '{licence.LicenceId}' not found"
                }
            });
        }

        // Validate the licence
        var validationResult = await ValidateLicenceAsync(licence, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        // Check for duplicate licence number (if changed)
        if (existing.LicenceNumber != licence.LicenceNumber)
        {
            var duplicate = await _licenceRepository.GetByLicenceNumberAsync(licence.LicenceNumber, cancellationToken);
            if (duplicate != null)
            {
                return ValidationResult.Failure(new[]
                {
                    new ValidationViolation
                    {
                        ErrorCode = ErrorCodes.VALIDATION_ERROR,
                        Message = $"Licence with number '{licence.LicenceNumber}' already exists"
                    }
                });
            }
        }

        // Update timestamp
        licence.ModifiedDate = DateTime.UtcNow;

        await _licenceRepository.UpdateAsync(licence, cancellationToken);
        _logger.LogInformation("Updated licence {LicenceNumber} with ID {Id}", licence.LicenceNumber, licence.LicenceId);

        return ValidationResult.Success();
    }

    /// <summary>
    /// Deletes a licence.
    /// </summary>
    public async Task<ValidationResult> DeleteAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        var existing = await _licenceRepository.GetByIdAsync(licenceId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.LICENCE_NOT_FOUND,
                    Message = $"Licence with ID '{licenceId}' not found"
                }
            });
        }

        await _licenceRepository.DeleteAsync(licenceId, cancellationToken);
        _logger.LogInformation("Deleted licence {Id}", licenceId);

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates a licence meets business rules.
    /// Per data-model.md validation rules.
    /// </summary>
    public async Task<ValidationResult> ValidateLicenceAsync(Licence licence, CancellationToken cancellationToken = default)
    {
        var violations = new List<ValidationViolation>();

        // Basic model validation
        var modelResult = licence.Validate();
        if (!modelResult.IsValid)
        {
            violations.AddRange(modelResult.Violations);
        }

        // Verify licence type exists
        var licenceType = await _licenceTypeRepository.GetByIdAsync(licence.LicenceTypeId, cancellationToken);
        if (licenceType == null)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = $"Licence type with ID '{licence.LicenceTypeId}' not found"
            });
        }
        else if (!licenceType.IsActive)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = $"Licence type '{licenceType.Name}' is not active"
            });
        }
        else
        {
            // T078: Validate permitted activities against licence type
            var activitiesResult = licence.ValidatePermittedActivities(licenceType);
            if (!activitiesResult.IsValid)
            {
                violations.AddRange(activitiesResult.Violations);
            }
        }

        // Validate holder type
        if (!string.IsNullOrEmpty(licence.HolderType) &&
            licence.HolderType != "Company" && licence.HolderType != "Customer")
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "HolderType must be 'Company' or 'Customer'"
            });
        }

        // Validate status
        var validStatuses = new[] { "Valid", "Expired", "Suspended", "Revoked" };
        if (!string.IsNullOrEmpty(licence.Status) && !validStatuses.Contains(licence.Status))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = $"Status must be one of: {string.Join(", ", validStatuses)}"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Checks if a holder has a valid licence for a specific activity.
    /// Per FR-018: Verify customer holds all required valid licences for each controlled product.
    /// </summary>
    public async Task<ValidationResult> CheckHolderLicenceForActivityAsync(
        Guid holderId,
        string holderType,
        LicenceTypes.PermittedActivity requiredActivity,
        CancellationToken cancellationToken = default)
    {
        var licences = await GetByHolderAsync(holderId, holderType, cancellationToken);

        var validLicences = licences
            .Where(l => l.Status == "Valid" && !l.IsExpired())
            .Where(l => l.PermittedActivities.HasFlag(requiredActivity))
            .ToList();

        if (!validLicences.Any())
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.LICENCE_MISSING,
                    Message = $"No valid licence found for activity '{requiredActivity}'"
                }
            });
        }

        return ValidationResult.Success();
    }
}
