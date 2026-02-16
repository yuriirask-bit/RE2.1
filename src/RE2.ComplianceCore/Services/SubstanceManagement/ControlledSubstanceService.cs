using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Services.SubstanceManagement;

/// <summary>
/// Service for managing the controlled substance master list.
/// T073b: Implements business logic for FR-003 (substance master list management).
/// Refactored: ControlledSubstance is now a composite model keyed by SubstanceCode (string).
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
    public async Task<ControlledSubstance?> GetBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(substanceCode))
            return null;

        return await _substanceRepository.GetBySubstanceCodeAsync(substanceCode, cancellationToken);
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
            s.SubstanceCode.ToLowerInvariant().Contains(lowerSearchTerm));
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ConfigureComplianceAsync(
        ControlledSubstance substance,
        CancellationToken cancellationToken = default)
    {
        // Validate the substance
        var validationResult = await ValidateSubstanceAsync(substance, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Substance validation failed for {SubstanceCode}: {Errors}",
                substance.SubstanceCode,
                string.Join("; ", validationResult.Violations.Select(v => v.Message)));
            return validationResult;
        }

        // Check if compliance extension already exists
        var existing = await _substanceRepository.GetBySubstanceCodeAsync(substance.SubstanceCode, cancellationToken);
        if (existing != null && existing.IsComplianceConfigured)
        {
            return ValidationResult.Failure(new List<ValidationViolation>
            {
                new()
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = $"A compliance extension for substance '{substance.SubstanceCode}' already exists"
                }
            });
        }

        // Set timestamps
        substance.CreatedDate = DateTime.UtcNow;
        substance.ModifiedDate = DateTime.UtcNow;

        // Save the compliance extension
        await _substanceRepository.SaveComplianceExtensionAsync(substance, cancellationToken);

        _logger.LogInformation("Configured compliance for substance {Name} ({SubstanceCode})",
            substance.SubstanceName, substance.SubstanceCode);

        return ValidationResult.Success();
    }

    /// <inheritdoc />
    public async Task<ValidationResult> UpdateComplianceAsync(
        ControlledSubstance substance,
        CancellationToken cancellationToken = default)
    {
        // Check substance exists
        var existing = await _substanceRepository.GetBySubstanceCodeAsync(substance.SubstanceCode, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(new List<ValidationViolation>
            {
                new()
                {
                    ErrorCode = ErrorCodes.SUBSTANCE_NOT_FOUND,
                    Message = $"Substance with code '{substance.SubstanceCode}' not found"
                }
            });
        }

        // Validate the substance
        var validationResult = await ValidateSubstanceAsync(substance, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        // Update timestamp
        substance.ModifiedDate = DateTime.UtcNow;

        await _substanceRepository.UpdateComplianceExtensionAsync(substance, cancellationToken);

        _logger.LogInformation("Updated compliance for substance {SubstanceCode} ({SubstanceName})",
            substance.SubstanceCode, substance.SubstanceName);

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
        string substanceCode,
        CancellationToken cancellationToken = default)
    {
        var substance = await _substanceRepository.GetBySubstanceCodeAsync(substanceCode, cancellationToken);
        if (substance == null)
        {
            return ValidationResult.Failure(new List<ValidationViolation>
            {
                new()
                {
                    ErrorCode = ErrorCodes.SUBSTANCE_NOT_FOUND,
                    Message = $"Substance with code '{substanceCode}' not found"
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

        await _substanceRepository.UpdateComplianceExtensionAsync(substance, cancellationToken);

        _logger.LogInformation("Deactivated controlled substance {SubstanceCode} ({SubstanceName})",
            substanceCode, substance.SubstanceName);

        return ValidationResult.Success();
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ReactivateAsync(
        string substanceCode,
        CancellationToken cancellationToken = default)
    {
        var substance = await _substanceRepository.GetBySubstanceCodeAsync(substanceCode, cancellationToken);
        if (substance == null)
        {
            return ValidationResult.Failure(new List<ValidationViolation>
            {
                new()
                {
                    ErrorCode = ErrorCodes.SUBSTANCE_NOT_FOUND,
                    Message = $"Substance with code '{substanceCode}' not found"
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

        await _substanceRepository.UpdateComplianceExtensionAsync(substance, cancellationToken);

        _logger.LogInformation("Reactivated controlled substance {SubstanceCode} ({SubstanceName})",
            substanceCode, substance.SubstanceName);

        return ValidationResult.Success();
    }
}
