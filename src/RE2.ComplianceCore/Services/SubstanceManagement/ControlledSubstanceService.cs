using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Services.SubstanceManagement;

/// <summary>
/// Service for managing the controlled substance master list.
/// T073b: Implements business logic for FR-003 (substance master list management).
/// </summary>
public class ControlledSubstanceService : IControlledSubstanceService
{
    private readonly IControlledSubstanceRepository _substanceRepository;
    private readonly ILogger<ControlledSubstanceService> _logger;

    public ControlledSubstanceService(
        IControlledSubstanceRepository substanceRepository,
        ILogger<ControlledSubstanceService> logger)
    {
        _substanceRepository = substanceRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ControlledSubstance?> GetByIdAsync(Guid substanceId, CancellationToken cancellationToken = default)
    {
        return await _substanceRepository.GetByIdAsync(substanceId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ControlledSubstance?> GetByInternalCodeAsync(string internalCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(internalCode))
            return null;

        return await _substanceRepository.GetByInternalCodeAsync(internalCode, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ControlledSubstance>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _substanceRepository.GetAllAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ControlledSubstance>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _substanceRepository.GetAllActiveAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ControlledSubstance>> GetByOpiumActListAsync(
        SubstanceCategories.OpiumActList opiumActList,
        CancellationToken cancellationToken = default)
    {
        var allSubstances = await _substanceRepository.GetAllActiveAsync(cancellationToken);
        return allSubstances.Where(s => s.OpiumActList == opiumActList);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ControlledSubstance>> GetByPrecursorCategoryAsync(
        SubstanceCategories.PrecursorCategory precursorCategory,
        CancellationToken cancellationToken = default)
    {
        var allSubstances = await _substanceRepository.GetAllActiveAsync(cancellationToken);
        return allSubstances.Where(s => s.PrecursorCategory == precursorCategory);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ControlledSubstance>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllActiveAsync(cancellationToken);

        var allSubstances = await _substanceRepository.GetAllActiveAsync(cancellationToken);
        var lowerSearchTerm = searchTerm.ToLowerInvariant();

        return allSubstances.Where(s =>
            s.SubstanceName.ToLowerInvariant().Contains(lowerSearchTerm) ||
            s.InternalCode.ToLowerInvariant().Contains(lowerSearchTerm));
    }

    /// <inheritdoc />
    public async Task<(Guid? Id, ValidationResult Result)> CreateAsync(
        ControlledSubstance substance,
        CancellationToken cancellationToken = default)
    {
        // Validate the substance
        var validationResult = await ValidateSubstanceAsync(substance, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Substance validation failed for {InternalCode}: {Errors}",
                substance.InternalCode,
                string.Join("; ", validationResult.Violations.Select(v => v.Message)));
            return (null, validationResult);
        }

        // Check for duplicate internal code
        var existing = await _substanceRepository.GetByInternalCodeAsync(substance.InternalCode, cancellationToken);
        if (existing != null)
        {
            var error = ValidationResult.Failure(new List<ValidationViolation>
            {
                new()
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = $"A substance with internal code '{substance.InternalCode}' already exists"
                }
            });
            return (null, error);
        }

        // Set timestamps
        substance.CreatedDate = DateTime.UtcNow;
        substance.ModifiedDate = DateTime.UtcNow;

        // Create the substance
        var id = await _substanceRepository.CreateAsync(substance, cancellationToken);

        _logger.LogInformation("Created controlled substance {Name} ({InternalCode}) with ID {Id}",
            substance.SubstanceName, substance.InternalCode, id);

        return (id, ValidationResult.Success());
    }

    /// <inheritdoc />
    public async Task<ValidationResult> UpdateAsync(
        ControlledSubstance substance,
        CancellationToken cancellationToken = default)
    {
        // Check substance exists
        var existing = await _substanceRepository.GetByIdAsync(substance.SubstanceId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new List<ValidationViolation>
            {
                new()
                {
                    ErrorCode = ErrorCodes.SUBSTANCE_NOT_FOUND,
                    Message = $"Substance with ID '{substance.SubstanceId}' not found"
                }
            });
        }

        // Validate the substance
        var validationResult = await ValidateSubstanceAsync(substance, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        // Check for duplicate internal code (if changed)
        if (existing.InternalCode != substance.InternalCode)
        {
            var duplicate = await _substanceRepository.GetByInternalCodeAsync(substance.InternalCode, cancellationToken);
            if (duplicate != null)
            {
                return ValidationResult.Failure(new List<ValidationViolation>
                {
                    new()
                    {
                        ErrorCode = ErrorCodes.VALIDATION_ERROR,
                        Message = $"A substance with internal code '{substance.InternalCode}' already exists"
                    }
                });
            }
        }

        // Update timestamp
        substance.ModifiedDate = DateTime.UtcNow;

        await _substanceRepository.UpdateAsync(substance, cancellationToken);

        _logger.LogInformation("Updated controlled substance {Id} ({InternalCode})",
            substance.SubstanceId, substance.InternalCode);

        return ValidationResult.Success();
    }

    /// <inheritdoc />
    public async Task<ValidationResult> DeleteAsync(
        Guid substanceId,
        CancellationToken cancellationToken = default)
    {
        var existing = await _substanceRepository.GetByIdAsync(substanceId, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new List<ValidationViolation>
            {
                new()
                {
                    ErrorCode = ErrorCodes.SUBSTANCE_NOT_FOUND,
                    Message = $"Substance with ID '{substanceId}' not found"
                }
            });
        }

        // TODO: Check if substance is in use by any licence mappings before allowing deletion
        // For now, allow deletion (in production, consider soft-delete via DeactivateAsync)

        await _substanceRepository.DeleteAsync(substanceId, cancellationToken);

        _logger.LogInformation("Deleted controlled substance {Id} ({InternalCode})",
            substanceId, existing.InternalCode);

        return ValidationResult.Success();
    }

    /// <inheritdoc />
    public Task<ValidationResult> ValidateSubstanceAsync(
        ControlledSubstance substance,
        CancellationToken cancellationToken = default)
    {
        // Use the model's built-in validation
        var result = substance.Validate();

        // Additional service-level validations can be added here
        // e.g., checking against external regulatory databases

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task<ValidationResult> DeactivateAsync(
        Guid substanceId,
        CancellationToken cancellationToken = default)
    {
        var substance = await _substanceRepository.GetByIdAsync(substanceId, cancellationToken);
        if (substance == null)
        {
            return ValidationResult.Failure(new List<ValidationViolation>
            {
                new()
                {
                    ErrorCode = ErrorCodes.SUBSTANCE_NOT_FOUND,
                    Message = $"Substance with ID '{substanceId}' not found"
                }
            });
        }

        if (!substance.IsActive)
        {
            return ValidationResult.Failure(new List<ValidationViolation>
            {
                new()
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = "Substance is already inactive"
                }
            });
        }

        substance.IsActive = false;
        substance.ModifiedDate = DateTime.UtcNow;

        await _substanceRepository.UpdateAsync(substance, cancellationToken);

        _logger.LogInformation("Deactivated controlled substance {Id} ({InternalCode})",
            substanceId, substance.InternalCode);

        return ValidationResult.Success();
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ReactivateAsync(
        Guid substanceId,
        CancellationToken cancellationToken = default)
    {
        var substance = await _substanceRepository.GetByIdAsync(substanceId, cancellationToken);
        if (substance == null)
        {
            return ValidationResult.Failure(new List<ValidationViolation>
            {
                new()
                {
                    ErrorCode = ErrorCodes.SUBSTANCE_NOT_FOUND,
                    Message = $"Substance with ID '{substanceId}' not found"
                }
            });
        }

        if (substance.IsActive)
        {
            return ValidationResult.Failure(new List<ValidationViolation>
            {
                new()
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = "Substance is already active"
                }
            });
        }

        substance.IsActive = true;
        substance.ModifiedDate = DateTime.UtcNow;

        await _substanceRepository.UpdateAsync(substance, cancellationToken);

        _logger.LogInformation("Reactivated controlled substance {Id} ({InternalCode})",
            substanceId, substance.InternalCode);

        return ValidationResult.Success();
    }
}
