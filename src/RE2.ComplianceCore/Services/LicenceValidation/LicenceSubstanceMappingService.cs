using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Services.LicenceValidation;

/// <summary>
/// Service for licence-substance mapping business logic.
/// T079d: Business logic for FR-004 substance-to-licence mappings with validation per data-model.md.
/// </summary>
public class LicenceSubstanceMappingService : ILicenceSubstanceMappingService
{
    private readonly ILicenceSubstanceMappingRepository _mappingRepository;
    private readonly ILicenceRepository _licenceRepository;
    private readonly IControlledSubstanceRepository _substanceRepository;
    private readonly ILogger<LicenceSubstanceMappingService> _logger;

    public LicenceSubstanceMappingService(
        ILicenceSubstanceMappingRepository mappingRepository,
        ILicenceRepository licenceRepository,
        IControlledSubstanceRepository substanceRepository,
        ILogger<LicenceSubstanceMappingService> logger)
    {
        _mappingRepository = mappingRepository;
        _licenceRepository = licenceRepository;
        _substanceRepository = substanceRepository;
        _logger = logger;
    }

    public async Task<LicenceSubstanceMapping?> GetByIdAsync(Guid mappingId, CancellationToken cancellationToken = default)
    {
        var mapping = await _mappingRepository.GetByIdAsync(mappingId, cancellationToken);
        if (mapping != null)
        {
            await PopulateNavigationPropertiesAsync(mapping, cancellationToken);
        }
        return mapping;
    }

    public async Task<IEnumerable<LicenceSubstanceMapping>> GetByLicenceIdAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        var mappings = (await _mappingRepository.GetByLicenceIdAsync(licenceId, cancellationToken)).ToList();
        await PopulateNavigationPropertiesAsync(mappings, cancellationToken);
        return mappings;
    }

    public async Task<IEnumerable<LicenceSubstanceMapping>> GetBySubstanceIdAsync(Guid substanceId, CancellationToken cancellationToken = default)
    {
        var mappings = (await _mappingRepository.GetBySubstanceIdAsync(substanceId, cancellationToken)).ToList();
        await PopulateNavigationPropertiesAsync(mappings, cancellationToken);
        return mappings;
    }

    public async Task<IEnumerable<LicenceSubstanceMapping>> GetActiveMappingsByLicenceIdAsync(Guid licenceId, CancellationToken cancellationToken = default)
    {
        var mappings = (await _mappingRepository.GetActiveMappingsByLicenceIdAsync(licenceId, cancellationToken)).ToList();
        await PopulateNavigationPropertiesAsync(mappings, cancellationToken);
        return mappings;
    }

    public async Task<IEnumerable<LicenceSubstanceMapping>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var mappings = (await _mappingRepository.GetAllAsync(cancellationToken)).ToList();
        await PopulateNavigationPropertiesAsync(mappings, cancellationToken);
        return mappings;
    }

    public async Task<(Guid? Id, ValidationResult Result)> CreateAsync(LicenceSubstanceMapping mapping, CancellationToken cancellationToken = default)
    {
        // Validate the mapping
        var validationResult = await ValidateMappingAsync(mapping, cancellationToken);
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Check for duplicate (LicenceId + SubstanceId + EffectiveDate must be unique)
        var existing = await _mappingRepository.GetByLicenceSubstanceEffectiveDateAsync(
            mapping.LicenceId,
            mapping.SubstanceId,
            mapping.EffectiveDate,
            cancellationToken);

        if (existing != null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = $"A mapping already exists for this licence, substance, and effective date ({mapping.EffectiveDate:yyyy-MM-dd})"
                }
            }));
        }

        var id = await _mappingRepository.CreateAsync(mapping, cancellationToken);
        _logger.LogInformation("Created mapping {Id} for licence {LicenceId} and substance {SubstanceId}",
            id, mapping.LicenceId, mapping.SubstanceId);

        return (id, ValidationResult.Success());
    }

    public async Task<ValidationResult> UpdateAsync(LicenceSubstanceMapping mapping, CancellationToken cancellationToken = default)
    {
        // Check mapping exists
        var existing = await _mappingRepository.GetByIdAsync(mapping.MappingId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Mapping with ID '{mapping.MappingId}' not found"
                }
            });
        }

        // Validate the mapping
        var validationResult = await ValidateMappingAsync(mapping, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        // Check for duplicate (if key fields changed)
        if (existing.LicenceId != mapping.LicenceId ||
            existing.SubstanceId != mapping.SubstanceId ||
            existing.EffectiveDate != mapping.EffectiveDate)
        {
            var duplicate = await _mappingRepository.GetByLicenceSubstanceEffectiveDateAsync(
                mapping.LicenceId,
                mapping.SubstanceId,
                mapping.EffectiveDate,
                cancellationToken);

            if (duplicate != null && duplicate.MappingId != mapping.MappingId)
            {
                return ValidationResult.Failure(new[]
                {
                    new ValidationViolation
                    {
                        ErrorCode = ErrorCodes.VALIDATION_ERROR,
                        Message = $"A mapping already exists for this licence, substance, and effective date ({mapping.EffectiveDate:yyyy-MM-dd})"
                    }
                });
            }
        }

        await _mappingRepository.UpdateAsync(mapping, cancellationToken);
        _logger.LogInformation("Updated mapping {Id}", mapping.MappingId);

        return ValidationResult.Success();
    }

    public async Task<ValidationResult> DeleteAsync(Guid mappingId, CancellationToken cancellationToken = default)
    {
        var existing = await _mappingRepository.GetByIdAsync(mappingId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.NOT_FOUND,
                    Message = $"Mapping with ID '{mappingId}' not found"
                }
            });
        }

        await _mappingRepository.DeleteAsync(mappingId, cancellationToken);
        _logger.LogInformation("Deleted mapping {Id}", mappingId);

        return ValidationResult.Success();
    }

    public async Task<ValidationResult> ValidateMappingAsync(LicenceSubstanceMapping mapping, CancellationToken cancellationToken = default)
    {
        var violations = new List<ValidationViolation>();

        // Basic model validation
        var modelResult = mapping.Validate();
        if (!modelResult.IsValid)
        {
            violations.AddRange(modelResult.Violations);
        }

        // Verify licence exists
        var licence = await _licenceRepository.GetByIdAsync(mapping.LicenceId, cancellationToken);
        if (licence == null)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = $"Licence with ID '{mapping.LicenceId}' not found"
            });
        }
        else
        {
            // Validate mapping against licence (T079: ExpiryDate must not exceed licence's ExpiryDate)
            var licenceValidationResult = mapping.ValidateAgainstLicence(licence);
            if (!licenceValidationResult.IsValid)
            {
                violations.AddRange(licenceValidationResult.Violations);
            }
        }

        // Verify substance exists
        var substance = await _substanceRepository.GetByIdAsync(mapping.SubstanceId, cancellationToken);
        if (substance == null)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = $"Substance with ID '{mapping.SubstanceId}' not found"
            });
        }
        else if (!substance.IsActive)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = $"Substance '{substance.SubstanceName}' is not active"
            });
        }

        return violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success();
    }

    public async Task<bool> IsSubstanceAuthorizedByLicenceAsync(Guid licenceId, Guid substanceId, CancellationToken cancellationToken = default)
    {
        var activeMappings = await _mappingRepository.GetActiveMappingsByLicenceIdAsync(licenceId, cancellationToken);
        return activeMappings.Any(m => m.SubstanceId == substanceId);
    }

    private async Task PopulateNavigationPropertiesAsync(LicenceSubstanceMapping mapping, CancellationToken cancellationToken)
    {
        if (mapping.Licence == null && mapping.LicenceId != Guid.Empty)
        {
            mapping.Licence = await _licenceRepository.GetByIdAsync(mapping.LicenceId, cancellationToken);
        }
        if (mapping.Substance == null && mapping.SubstanceId != Guid.Empty)
        {
            mapping.Substance = await _substanceRepository.GetByIdAsync(mapping.SubstanceId, cancellationToken);
        }
    }

    private async Task PopulateNavigationPropertiesAsync(IEnumerable<LicenceSubstanceMapping> mappings, CancellationToken cancellationToken)
    {
        // Get all unique IDs
        var licenceIds = mappings.Select(m => m.LicenceId).Distinct().ToList();
        var substanceIds = mappings.Select(m => m.SubstanceId).Distinct().ToList();

        // Load all related entities in batches
        var allLicences = await _licenceRepository.GetAllAsync(cancellationToken);
        var licencesDict = allLicences.ToDictionary(l => l.LicenceId);

        var allSubstances = await _substanceRepository.GetAllAsync(cancellationToken);
        var substancesDict = allSubstances.ToDictionary(s => s.SubstanceId);

        // Populate navigation properties
        foreach (var mapping in mappings)
        {
            if (licencesDict.TryGetValue(mapping.LicenceId, out var licence))
            {
                mapping.Licence = licence;
            }
            if (substancesDict.TryGetValue(mapping.SubstanceId, out var substance))
            {
                mapping.Substance = substance;
            }
        }
    }
}
