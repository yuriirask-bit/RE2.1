using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceCore.Services.RiskMonitoring;

/// <summary>
/// Service for threshold management business logic.
/// T132c: ThresholdService with CRUD operations and validation logic per FR-022.
/// </summary>
public class ThresholdService : IThresholdService
{
    private readonly IThresholdRepository _thresholdRepository;
    private readonly IControlledSubstanceRepository _substanceRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<ThresholdService> _logger;

    public ThresholdService(
        IThresholdRepository thresholdRepository,
        IControlledSubstanceRepository substanceRepository,
        ICustomerRepository customerRepository,
        ILogger<ThresholdService> logger)
    {
        _thresholdRepository = thresholdRepository;
        _substanceRepository = substanceRepository;
        _customerRepository = customerRepository;
        _logger = logger;
    }

    #region CRUD Operations

    public async Task<Threshold?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var threshold = await _thresholdRepository.GetByIdAsync(id, cancellationToken);
        if (threshold != null)
        {
            await PopulateNavigationPropertiesAsync(threshold, cancellationToken);
        }
        return threshold;
    }

    public async Task<IEnumerable<Threshold>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var thresholds = (await _thresholdRepository.GetAllAsync(cancellationToken)).ToList();
        await PopulateNavigationPropertiesAsync(thresholds, cancellationToken);
        return thresholds;
    }

    public async Task<IEnumerable<Threshold>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var thresholds = (await _thresholdRepository.GetActiveAsync(cancellationToken)).ToList();
        await PopulateNavigationPropertiesAsync(thresholds, cancellationToken);
        return thresholds;
    }

    public async Task<(Guid? Id, ValidationResult Result)> CreateAsync(Threshold threshold, CancellationToken cancellationToken = default)
    {
        // Validate the threshold
        var validationResult = await ValidateAsync(threshold, cancellationToken);
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Set audit fields
        threshold.Id = Guid.NewGuid();
        threshold.CreatedDate = DateTime.UtcNow;

        // Populate denormalized fields
        await PopulateDenormalizedFieldsAsync(threshold, cancellationToken);

        try
        {
            var id = await _thresholdRepository.CreateAsync(threshold, cancellationToken);

            _logger.LogInformation(
                "Created threshold {ThresholdId} ({Name}) for {Type}/{Period}",
                id, threshold.Name, threshold.ThresholdType, threshold.Period);

            return (id, ValidationResult.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating threshold {Name}", threshold.Name);
            return (null, ValidationResult.Failure(ErrorCodes.INTERNAL_ERROR, $"Failed to create threshold: {ex.Message}"));
        }
    }

    public async Task<ValidationResult> UpdateAsync(Threshold threshold, CancellationToken cancellationToken = default)
    {
        // Check if threshold exists
        var existing = await _thresholdRepository.GetByIdAsync(threshold.Id, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(ErrorCodes.THRESHOLD_NOT_FOUND, $"Threshold with ID '{threshold.Id}' not found");
        }

        // Validate the threshold
        var validationResult = await ValidateAsync(threshold, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        // Set audit fields
        threshold.ModifiedDate = DateTime.UtcNow;

        // Populate denormalized fields
        await PopulateDenormalizedFieldsAsync(threshold, cancellationToken);

        try
        {
            await _thresholdRepository.UpdateAsync(threshold, cancellationToken);

            _logger.LogInformation(
                "Updated threshold {ThresholdId} ({Name})",
                threshold.Id, threshold.Name);

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating threshold {ThresholdId}", threshold.Id);
            return ValidationResult.Failure(ErrorCodes.INTERNAL_ERROR, $"Failed to update threshold: {ex.Message}");
        }
    }

    public async Task<ValidationResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _thresholdRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
        {
            return ValidationResult.Failure(ErrorCodes.THRESHOLD_NOT_FOUND, $"Threshold with ID '{id}' not found");
        }

        try
        {
            await _thresholdRepository.DeleteAsync(id, cancellationToken);

            _logger.LogInformation("Deleted threshold {ThresholdId} ({Name})", id, existing.Name);

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting threshold {ThresholdId}", id);
            return ValidationResult.Failure(ErrorCodes.INTERNAL_ERROR, $"Failed to delete threshold: {ex.Message}");
        }
    }

    #endregion

    #region Filtered Queries

    public async Task<IEnumerable<Threshold>> GetByTypeAsync(ThresholdType type, CancellationToken cancellationToken = default)
    {
        var thresholds = (await _thresholdRepository.GetByTypeAsync(type, cancellationToken)).ToList();
        await PopulateNavigationPropertiesAsync(thresholds, cancellationToken);
        return thresholds;
    }

    public async Task<IEnumerable<Threshold>> GetBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default)
    {
        var thresholds = (await _thresholdRepository.GetBySubstanceCodeAsync(substanceCode, cancellationToken)).ToList();
        await PopulateNavigationPropertiesAsync(thresholds, cancellationToken);
        return thresholds;
    }

    public async Task<IEnumerable<Threshold>> GetByCustomerCategoryAsync(BusinessCategory category, CancellationToken cancellationToken = default)
    {
        var thresholds = (await _thresholdRepository.GetByCustomerCategoryAsync(category, cancellationToken)).ToList();
        await PopulateNavigationPropertiesAsync(thresholds, cancellationToken);
        return thresholds;
    }

    public async Task<IEnumerable<Threshold>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var allThresholds = await _thresholdRepository.GetAllAsync(cancellationToken);
        var filtered = allThresholds.Where(t =>
            t.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
            (t.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (t.SubstanceName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (t.SubstanceCode?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false)
        ).ToList();

        await PopulateNavigationPropertiesAsync(filtered, cancellationToken);
        return filtered;
    }

    #endregion

    #region Validation

    public Task<ValidationResult> ValidateAsync(Threshold threshold, CancellationToken cancellationToken = default)
    {
        var violations = new List<ValidationViolation>();

        // Name is required
        if (string.IsNullOrWhiteSpace(threshold.Name))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Threshold name is required",
                Severity = ViolationSeverity.Critical
            });
        }

        // Limit value must be positive
        if (threshold.LimitValue <= 0)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Limit value must be greater than zero",
                Severity = ViolationSeverity.Critical
            });
        }

        // Limit unit is required
        if (string.IsNullOrWhiteSpace(threshold.LimitUnit))
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Limit unit is required",
                Severity = ViolationSeverity.Critical
            });
        }

        // Warning threshold must be between 0 and 100
        if (threshold.WarningThresholdPercent < 0 || threshold.WarningThresholdPercent > 100)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Warning threshold percent must be between 0 and 100",
                Severity = ViolationSeverity.Critical
            });
        }

        // Max override percent must be greater than 100 if set
        if (threshold.MaxOverridePercent.HasValue && threshold.MaxOverridePercent.Value <= 100)
        {
            violations.Add(new ValidationViolation
            {
                ErrorCode = ErrorCodes.VALIDATION_ERROR,
                Message = "Max override percent must be greater than 100 (e.g., 120 for 20% override allowance)",
                Severity = ViolationSeverity.Warning
            });
        }

        // Effective dates validation
        if (threshold.EffectiveFrom.HasValue && threshold.EffectiveTo.HasValue)
        {
            if (threshold.EffectiveTo.Value < threshold.EffectiveFrom.Value)
            {
                violations.Add(new ValidationViolation
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = "Effective end date cannot be before start date",
                    Severity = ViolationSeverity.Critical
                });
            }
        }

        return Task.FromResult(violations.Any()
            ? ValidationResult.Failure(violations)
            : ValidationResult.Success());
    }

    #endregion

    #region Private Methods

    private async Task PopulateDenormalizedFieldsAsync(Threshold threshold, CancellationToken cancellationToken)
    {
        // Populate substance info if set
        if (!string.IsNullOrEmpty(threshold.SubstanceCode))
        {
            var substance = await _substanceRepository.GetBySubstanceCodeAsync(threshold.SubstanceCode, cancellationToken);
            if (substance != null)
            {
                threshold.SubstanceName = substance.SubstanceName;
            }
        }
    }

    private async Task PopulateNavigationPropertiesAsync(Threshold threshold, CancellationToken cancellationToken)
    {
        // Populate substance info if needed
        if (!string.IsNullOrEmpty(threshold.SubstanceCode) && string.IsNullOrEmpty(threshold.SubstanceName))
        {
            var substance = await _substanceRepository.GetBySubstanceCodeAsync(threshold.SubstanceCode, cancellationToken);
            if (substance != null)
            {
                threshold.SubstanceName = substance.SubstanceName;
            }
        }
    }

    private async Task PopulateNavigationPropertiesAsync(IEnumerable<Threshold> thresholds, CancellationToken cancellationToken)
    {
        var substanceCodes = thresholds
            .Where(t => !string.IsNullOrEmpty(t.SubstanceCode) && string.IsNullOrEmpty(t.SubstanceName))
            .Select(t => t.SubstanceCode!)
            .Distinct()
            .ToList();

        if (!substanceCodes.Any()) return;

        // Batch load substances
        var substanceLookup = new Dictionary<string, ControlledSubstance>();
        foreach (var code in substanceCodes)
        {
            var substance = await _substanceRepository.GetBySubstanceCodeAsync(code, cancellationToken);
            if (substance != null)
            {
                substanceLookup[code] = substance;
            }
        }

        // Populate thresholds
        foreach (var threshold in thresholds)
        {
            if (!string.IsNullOrEmpty(threshold.SubstanceCode) &&
                substanceLookup.TryGetValue(threshold.SubstanceCode, out var substance))
            {
                threshold.SubstanceName = substance.SubstanceName;
            }
        }
    }

    #endregion
}
