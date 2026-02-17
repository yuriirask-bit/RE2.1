using Microsoft.Extensions.Logging;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.Shared.Constants;

namespace RE2.ComplianceCore.Services.LicenceValidation;

/// <summary>
/// Service for substance reclassification operations.
/// T080f: Per FR-066 requirements for reclassification workflow.
/// Handles: recording reclassifications, customer impact analysis, flagging, and notifications.
/// </summary>
public class SubstanceReclassificationService : ISubstanceReclassificationService
{
    private readonly ISubstanceReclassificationRepository _reclassificationRepository;
    private readonly IControlledSubstanceRepository _substanceRepository;
    private readonly ILicenceRepository _licenceRepository;
    private readonly ILicenceTypeRepository _licenceTypeRepository;
    private readonly ILogger<SubstanceReclassificationService> _logger;

    public SubstanceReclassificationService(
        ISubstanceReclassificationRepository reclassificationRepository,
        IControlledSubstanceRepository substanceRepository,
        ILicenceRepository licenceRepository,
        ILicenceTypeRepository licenceTypeRepository,
        ILogger<SubstanceReclassificationService> logger)
    {
        _reclassificationRepository = reclassificationRepository;
        _substanceRepository = substanceRepository;
        _licenceRepository = licenceRepository;
        _licenceTypeRepository = licenceTypeRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SubstanceReclassification?> GetByIdAsync(Guid reclassificationId, CancellationToken cancellationToken = default)
    {
        var reclassification = await _reclassificationRepository.GetByIdAsync(reclassificationId, cancellationToken);
        if (reclassification != null)
        {
            reclassification.Substance = await _substanceRepository.GetBySubstanceCodeAsync(reclassification.SubstanceCode, cancellationToken);
            reclassification.AffectedCustomers = (await _reclassificationRepository.GetCustomerImpactsAsync(reclassificationId, cancellationToken)).ToList();
        }
        return reclassification;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SubstanceReclassification>> GetBySubstanceCodeAsync(string substanceCode, CancellationToken cancellationToken = default)
    {
        return await _reclassificationRepository.GetBySubstanceCodeAsync(substanceCode, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SubstanceReclassification>> GetPendingReclassificationsAsync(CancellationToken cancellationToken = default)
    {
        return await _reclassificationRepository.GetByStatusAsync(ReclassificationStatus.Pending, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(Guid? Id, ValidationResult Result)> CreateReclassificationAsync(
        SubstanceReclassification reclassification,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating reclassification for substance {SubstanceCode}", reclassification.SubstanceCode);

        // Validate the reclassification
        var validationResult = reclassification.Validate();
        if (!validationResult.IsValid)
        {
            return (null, validationResult);
        }

        // Verify substance exists
        var substance = await _substanceRepository.GetBySubstanceCodeAsync(reclassification.SubstanceCode, cancellationToken);
        if (substance == null)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = $"Substance with code '{reclassification.SubstanceCode}' not found"
                }
            }));
        }

        // Verify previous classification matches current substance classification
        if (reclassification.PreviousOpiumActList != substance.OpiumActList ||
            reclassification.PreviousPrecursorCategory != substance.PrecursorCategory)
        {
            return (null, ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = "Previous classification must match substance's current classification"
                }
            }));
        }

        // Set initial status
        reclassification.Status = ReclassificationStatus.Pending;

        var id = await _reclassificationRepository.CreateAsync(reclassification, cancellationToken);
        _logger.LogInformation("Created reclassification {Id} for substance {SubstanceName}",
            id, substance.SubstanceName);

        return (id, ValidationResult.Success());
    }

    /// <inheritdoc />
    public async Task<ReclassificationImpactAnalysis> AnalyzeCustomerImpactAsync(
        Guid reclassificationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing customer impact for reclassification {Id}", reclassificationId);

        var reclassification = await _reclassificationRepository.GetByIdAsync(reclassificationId, cancellationToken);
        if (reclassification == null)
        {
            throw new InvalidOperationException($"Reclassification {reclassificationId} not found");
        }

        var substance = await _substanceRepository.GetBySubstanceCodeAsync(reclassification.SubstanceCode, cancellationToken);
        reclassification.Substance = substance;

        var analysis = new ReclassificationImpactAnalysis
        {
            Reclassification = reclassification
        };

        // Get all licences that currently cover this substance
        var affectedLicences = await _licenceRepository.GetBySubstanceCodeAsync(reclassification.SubstanceCode, cancellationToken);
        var licencesList = affectedLicences.ToList();

        // Group by customer (holder)
        var customerLicences = licencesList
            .Where(l => l.HolderType == "Customer")
            .GroupBy(l => l.HolderId)
            .ToList();

        analysis.TotalAffectedCustomers = customerLicences.Count;

        // Analyze each customer
        foreach (var customerGroup in customerLicences)
        {
            var customerId = customerGroup.Key;
            var customerLicenceList = customerGroup.ToList();

            var impact = await AnalyzeCustomerLicenceSufficiency(
                reclassification,
                customerId,
                customerLicenceList,
                cancellationToken);

            analysis.CustomerImpacts.Add(impact);

            if (impact.HasSufficientLicence)
            {
                analysis.CustomersWithSufficientLicences++;
            }
            else
            {
                analysis.CustomersFlaggedForReQualification++;
            }
        }

        _logger.LogInformation(
            "Impact analysis complete for reclassification {Id}: {Total} affected, {Sufficient} sufficient, {Flagged} flagged",
            reclassificationId,
            analysis.TotalAffectedCustomers,
            analysis.CustomersWithSufficientLicences,
            analysis.CustomersFlaggedForReQualification);

        return analysis;
    }

    /// <summary>
    /// Analyzes whether a customer's licences are sufficient for the new classification.
    /// T080j: Per FR-066 customer flagging logic.
    /// </summary>
    private async Task<ReclassificationCustomerImpact> AnalyzeCustomerLicenceSufficiency(
        SubstanceReclassification reclassification,
        Guid customerId,
        List<Licence> customerLicences,
        CancellationToken cancellationToken)
    {
        var impact = new ReclassificationCustomerImpact
        {
            ReclassificationId = reclassification.ReclassificationId,
            CustomerId = customerId,
            RelevantLicenceIds = customerLicences.Select(l => l.LicenceId).ToList()
        };

        // Load licence types for analysis
        var licenceTypes = new Dictionary<Guid, LicenceType>();
        foreach (var licence in customerLicences)
        {
            if (!licenceTypes.ContainsKey(licence.LicenceTypeId))
            {
                var licenceType = await _licenceTypeRepository.GetByIdAsync(licence.LicenceTypeId, cancellationToken);
                if (licenceType != null)
                {
                    licenceTypes[licence.LicenceTypeId] = licenceType;
                    licence.LicenceType = licenceType;
                }
            }
            else
            {
                licence.LicenceType = licenceTypes[licence.LicenceTypeId];
            }
        }

        // Check if any licence covers the new classification requirements
        var hasSufficientLicence = CheckLicencesSufficient(customerLicences, reclassification);
        impact.HasSufficientLicence = hasSufficientLicence;
        impact.RequiresReQualification = !hasSufficientLicence;

        if (!hasSufficientLicence)
        {
            impact.LicenceGapSummary = GenerateLicenceGapSummary(customerLicences, reclassification);
        }

        return impact;
    }

    /// <summary>
    /// Checks if the customer's licences are sufficient for the new substance classification.
    /// </summary>
    private bool CheckLicencesSufficient(List<Licence> licences, SubstanceReclassification reclassification)
    {
        // For an upgrade (stricter classification), check if customer has appropriate licences
        if (!reclassification.IsUpgrade())
        {
            // Downgrade - existing licences should be sufficient
            return true;
        }

        // Check Opium Act upgrade requirements using regulatory severity
        var isOpiumActUpgrade = SubstanceReclassification.GetOpiumActSeverity(reclassification.NewOpiumActList) >
                                SubstanceReclassification.GetOpiumActSeverity(reclassification.PreviousOpiumActList);
        if (isOpiumActUpgrade)
        {
            // Need licence that covers the new Opium Act list
            // List I requires specific Opium Act exemption
            // List II requires at minimum a wholesale licence
            var hasOpiumActCoverage = licences.Any(l =>
                l.LicenceType != null &&
                l.Status == "Valid" &&
                !l.IsExpired() &&
                CoversOpiumActList(l.LicenceType, reclassification.NewOpiumActList));

            if (!hasOpiumActCoverage)
            {
                return false;
            }
        }

        // Check Precursor category upgrade requirements using regulatory severity
        var isPrecursorUpgrade = SubstanceReclassification.GetPrecursorSeverity(reclassification.NewPrecursorCategory) >
                                 SubstanceReclassification.GetPrecursorSeverity(reclassification.PreviousPrecursorCategory);
        if (isPrecursorUpgrade)
        {
            // Category 1 requires stricter licences than Category 2/3
            var hasPrecursorCoverage = licences.Any(l =>
                l.LicenceType != null &&
                l.Status == "Valid" &&
                !l.IsExpired() &&
                CoversPrecursorCategory(l.LicenceType, reclassification.NewPrecursorCategory));

            if (!hasPrecursorCoverage)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if a licence type covers a specific Opium Act list.
    /// </summary>
    private bool CoversOpiumActList(LicenceType licenceType, SubstanceCategories.OpiumActList list)
    {
        // Per Dutch regulations:
        // - List I substances require specific Opium Act exemption
        // - List II can be handled with wholesale licence + proper scope
        // This is a simplified check - real implementation would check IssuingAuthority and specific conditions

        if (list == SubstanceCategories.OpiumActList.ListI)
        {
            // List I requires handling capability (possess, store, distribute are typical)
            return licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Possess) &&
                   licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Store);
        }

        if (list == SubstanceCategories.OpiumActList.ListII)
        {
            // List II requires at minimum distribution capability
            return licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Distribute);
        }

        return true;
    }

    /// <summary>
    /// Checks if a licence type covers a specific precursor category.
    /// </summary>
    private bool CoversPrecursorCategory(LicenceType licenceType, SubstanceCategories.PrecursorCategory category)
    {
        // Per EU precursor regulations:
        // - Category 1 requires specific EU precursor licence
        // - Categories 2 and 3 require HandlePrecursors permission
        if (category == SubstanceCategories.PrecursorCategory.Category1)
        {
            return licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.HandlePrecursors);
        }

        if (category == SubstanceCategories.PrecursorCategory.Category2 ||
            category == SubstanceCategories.PrecursorCategory.Category3)
        {
            return licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.HandlePrecursors) ||
                   licenceType.PermittedActivities.HasFlag(LicenceTypes.PermittedActivity.Distribute);
        }

        return true;
    }

    /// <summary>
    /// Generates a summary of what licence gaps exist.
    /// </summary>
    private string GenerateLicenceGapSummary(List<Licence> licences, SubstanceReclassification reclassification)
    {
        var gaps = new List<string>();

        if (reclassification.NewOpiumActList > reclassification.PreviousOpiumActList)
        {
            gaps.Add($"Requires licence covering Opium Act {reclassification.NewOpiumActList}");
        }

        if (reclassification.NewPrecursorCategory > reclassification.PreviousPrecursorCategory)
        {
            gaps.Add($"Requires licence covering Precursor {reclassification.NewPrecursorCategory}");
        }

        var currentLicenceTypes = licences
            .Where(l => l.LicenceType != null)
            .Select(l => l.LicenceType!.Name)
            .Distinct();

        if (currentLicenceTypes.Any())
        {
            gaps.Add($"Current licences: {string.Join(", ", currentLicenceTypes)}");
        }

        return string.Join("; ", gaps);
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ProcessReclassificationAsync(
        Guid reclassificationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing reclassification {Id}", reclassificationId);

        var reclassification = await _reclassificationRepository.GetByIdAsync(reclassificationId, cancellationToken);
        if (reclassification == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = $"Reclassification {reclassificationId} not found"
                }
            });
        }

        if (reclassification.Status != ReclassificationStatus.Pending)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = $"Reclassification is already in status {reclassification.Status}"
                }
            });
        }

        // Update status to processing
        reclassification.Status = ReclassificationStatus.Processing;
        await _reclassificationRepository.UpdateAsync(reclassification, cancellationToken);

        try
        {
            // Perform impact analysis
            var analysis = await AnalyzeCustomerImpactAsync(reclassificationId, cancellationToken);

            // Create customer impact records
            await _reclassificationRepository.CreateCustomerImpactsBatchAsync(
                analysis.CustomerImpacts,
                cancellationToken);

            // Update substance classification
            var substance = await _substanceRepository.GetBySubstanceCodeAsync(reclassification.SubstanceCode, cancellationToken);
            if (substance != null)
            {
                substance.OpiumActList = reclassification.NewOpiumActList;
                substance.PrecursorCategory = reclassification.NewPrecursorCategory;
                substance.ClassificationEffectiveDate = reclassification.EffectiveDate;
                await _substanceRepository.UpdateComplianceExtensionAsync(substance, cancellationToken);
            }

            // Update reclassification with results
            reclassification.AffectedCustomerCount = analysis.TotalAffectedCustomers;
            reclassification.FlaggedCustomerCount = analysis.CustomersFlaggedForReQualification;
            reclassification.Status = ReclassificationStatus.Completed;
            reclassification.ProcessedDate = DateTime.UtcNow;
            await _reclassificationRepository.UpdateAsync(reclassification, cancellationToken);

            _logger.LogInformation(
                "Reclassification {Id} processed: {Affected} affected, {Flagged} flagged",
                reclassificationId,
                analysis.TotalAffectedCustomers,
                analysis.CustomersFlaggedForReQualification);

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing reclassification {Id}", reclassificationId);

            // Revert status on failure
            reclassification.Status = ReclassificationStatus.Pending;
            await _reclassificationRepository.UpdateAsync(reclassification, cancellationToken);

            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.INTERNAL_ERROR,
                    Message = $"Error processing reclassification: {ex.Message}"
                }
            });
        }
    }

    /// <inheritdoc />
    public async Task<ValidationResult> MarkCustomerReQualifiedAsync(
        Guid reclassificationId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Marking customer {CustomerId} as re-qualified for reclassification {ReclassificationId}",
            customerId, reclassificationId);

        var impact = await _reclassificationRepository.GetCustomerImpactAsync(reclassificationId, customerId, cancellationToken);
        if (impact == null)
        {
            return ValidationResult.Failure(new[]
            {
                new ValidationViolation
                {
                    ErrorCode = ErrorCodes.VALIDATION_ERROR,
                    Message = $"Customer impact record not found for customer {customerId} and reclassification {reclassificationId}"
                }
            });
        }

        impact.RequiresReQualification = false;
        impact.ReQualificationDate = DateTime.UtcNow;
        await _reclassificationRepository.UpdateCustomerImpactAsync(impact, cancellationToken);

        _logger.LogInformation("Customer {CustomerId} marked as re-qualified", customerId);
        return ValidationResult.Success();
    }

    /// <inheritdoc />
    public async Task<(bool IsBlocked, IEnumerable<ReclassificationCustomerImpact> BlockingImpacts)> CheckCustomerBlockedAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        // T080l: Check if customer is blocked due to reclassification requiring re-qualification
        var impacts = await _reclassificationRepository.GetCustomersRequiringReQualificationAsync(cancellationToken);
        var customerImpacts = impacts.Where(i => i.CustomerId == customerId).ToList();

        var isBlocked = customerImpacts.Any();
        return (isBlocked, customerImpacts);
    }

    /// <inheritdoc />
    public async Task<SubstanceClassification> GetEffectiveClassificationAsync(
        string substanceCode,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default)
    {
        // T080m: Historical transaction validation support
        var substance = await _substanceRepository.GetBySubstanceCodeAsync(substanceCode, cancellationToken);
        if (substance == null)
        {
            throw new InvalidOperationException($"Substance '{substanceCode}' not found");
        }

        var reclassification = await _reclassificationRepository.GetEffectiveReclassificationAsync(
            substanceCode, asOfDate, cancellationToken);

        if (reclassification != null)
        {
            return new SubstanceClassification
            {
                SubstanceCode = substanceCode,
                AsOfDate = asOfDate,
                OpiumActList = reclassification.NewOpiumActList,
                PrecursorCategory = reclassification.NewPrecursorCategory,
                SourceReclassificationId = reclassification.ReclassificationId
            };
        }

        // No reclassification found for this date - use original classification
        // or find the first reclassification to get the "before" values
        var allReclassifications = await _reclassificationRepository.GetBySubstanceCodeAsync(substanceCode, cancellationToken);
        var firstReclassification = allReclassifications
            .Where(r => r.Status == ReclassificationStatus.Completed)
            .OrderBy(r => r.EffectiveDate)
            .FirstOrDefault();

        if (firstReclassification != null && firstReclassification.EffectiveDate > asOfDate)
        {
            // The date is before any reclassification - use previous values
            return new SubstanceClassification
            {
                SubstanceCode = substanceCode,
                AsOfDate = asOfDate,
                OpiumActList = firstReclassification.PreviousOpiumActList,
                PrecursorCategory = firstReclassification.PreviousPrecursorCategory,
                SourceReclassificationId = null
            };
        }

        // Use current substance classification
        return new SubstanceClassification
        {
            SubstanceCode = substanceCode,
            AsOfDate = asOfDate,
            OpiumActList = substance.OpiumActList,
            PrecursorCategory = substance.PrecursorCategory,
            SourceReclassificationId = null
        };
    }

    /// <inheritdoc />
    public async Task<ComplianceNotification> GenerateComplianceNotificationAsync(
        Guid reclassificationId,
        CancellationToken cancellationToken = default)
    {
        // T080k: Generate compliance team notification
        var reclassification = await _reclassificationRepository.GetByIdAsync(reclassificationId, cancellationToken);
        if (reclassification == null)
        {
            throw new InvalidOperationException($"Reclassification {reclassificationId} not found");
        }

        var substance = await _substanceRepository.GetBySubstanceCodeAsync(reclassification.SubstanceCode, cancellationToken);
        var impacts = await _reclassificationRepository.GetCustomerImpactsAsync(reclassificationId, cancellationToken);
        var impactList = impacts.ToList();

        var notification = new ComplianceNotification
        {
            ReclassificationId = reclassificationId,
            SubstanceName = substance?.SubstanceName ?? "Unknown",
            RegulatoryReference = reclassification.RegulatoryReference,
            EffectiveDate = reclassification.EffectiveDate,
            TotalAffectedCustomers = impactList.Count,
            CustomersRequiringAction = impactList.Count(i => i.RequiresReQualification)
        };

        foreach (var impact in impactList.Where(i => i.RequiresReQualification))
        {
            notification.RequiredActions.Add(new CustomerActionRequired
            {
                CustomerId = impact.CustomerId,
                CustomerName = impact.CustomerName,
                ActionRequired = "Update licence to cover new classification requirements",
                LicenceGapSummary = impact.LicenceGapSummary,
                RelevantLicenceIds = impact.RelevantLicenceIds ?? new List<Guid>()
            });
        }

        _logger.LogInformation(
            "Generated compliance notification for reclassification {Id}: {RequiringAction} customers require action",
            reclassificationId,
            notification.CustomersRequiringAction);

        return notification;
    }
}
