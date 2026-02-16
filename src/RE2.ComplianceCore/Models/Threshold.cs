using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.ComplianceCore.Models;

/// <summary>
/// Threshold configuration for quantity/frequency limits on controlled substances.
/// T127: Threshold domain model per FR-020/FR-022.
/// </summary>
public class Threshold
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Display name for this threshold rule.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the threshold rule.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of threshold (Quantity, Frequency, Value, CumulativeQuantity).
    /// </summary>
    public ThresholdType ThresholdType { get; set; }

    /// <summary>
    /// Time period for the threshold.
    /// </summary>
    public ThresholdPeriod Period { get; set; }

    #region Scope

    /// <summary>
    /// Substance code this threshold applies to (null = all substances).
    /// </summary>
    public string? SubstanceCode { get; set; }

    /// <summary>
    /// Substance name (denormalized).
    /// </summary>
    public string? SubstanceName { get; set; }

    /// <summary>
    /// Licence type ID this threshold applies to (null = all licence types).
    /// </summary>
    public Guid? LicenceTypeId { get; set; }

    /// <summary>
    /// Licence type name (denormalized).
    /// </summary>
    public string? LicenceTypeName { get; set; }

    /// <summary>
    /// Customer business category this applies to (null = all categories).
    /// </summary>
    public BusinessCategory? CustomerCategory { get; set; }

    /// <summary>
    /// Specific customer ID this applies to (null = all customers).
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// Opium Act List this threshold applies to (null = all lists).
    /// </summary>
    public string? OpiumActList { get; set; }

    #endregion

    #region Limits

    /// <summary>
    /// Maximum limit value.
    /// </summary>
    public decimal LimitValue { get; set; }

    /// <summary>
    /// Unit of measure for the limit (e.g., "g", "mg", "count", "EUR").
    /// </summary>
    public string LimitUnit { get; set; } = "g";

    /// <summary>
    /// Warning threshold (percentage of limit for early warning).
    /// Default: 80% of limit triggers warning.
    /// </summary>
    public decimal WarningThresholdPercent { get; set; } = 80m;

    #endregion

    #region Override Settings

    /// <summary>
    /// Whether exceeding this threshold can be overridden by ComplianceManager.
    /// </summary>
    public bool AllowOverride { get; set; } = true;

    /// <summary>
    /// Maximum override percentage allowed (e.g., 120 = can override up to 120% of limit).
    /// </summary>
    public decimal? MaxOverridePercent { get; set; }

    #endregion

    #region Status

    /// <summary>
    /// Whether this threshold rule is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Effective start date for this threshold.
    /// </summary>
    public DateOnly? EffectiveFrom { get; set; }

    /// <summary>
    /// Effective end date for this threshold (null = no end).
    /// </summary>
    public DateOnly? EffectiveTo { get; set; }

    /// <summary>
    /// Regulatory reference for this threshold (e.g., Opium Act article).
    /// </summary>
    public string? RegulatoryReference { get; set; }

    #endregion

    #region Audit

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// User who created the record.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime? ModifiedDate { get; set; }

    /// <summary>
    /// User who last modified the record.
    /// </summary>
    public string? ModifiedBy { get; set; }

    #endregion

    #region Methods

    /// <summary>
    /// Checks if this threshold is currently effective.
    /// </summary>
    public bool IsEffective()
    {
        return IsEffective(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    /// <summary>
    /// Checks if this threshold is effective on the given date.
    /// </summary>
    public bool IsEffective(DateOnly date)
    {
        if (!IsActive) return false;

        if (EffectiveFrom.HasValue && date < EffectiveFrom.Value)
            return false;

        if (EffectiveTo.HasValue && date > EffectiveTo.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Checks if the given value exceeds this threshold.
    /// </summary>
    public bool IsExceeded(decimal value)
    {
        return value > LimitValue;
    }

    /// <summary>
    /// Checks if the given value triggers a warning (but doesn't exceed).
    /// </summary>
    public bool IsWarning(decimal value)
    {
        var warningLevel = LimitValue * (WarningThresholdPercent / 100m);
        return value >= warningLevel && value <= LimitValue;
    }

    /// <summary>
    /// Checks if the given value with override would still exceed max allowed.
    /// </summary>
    public bool ExceedsMaxOverride(decimal value)
    {
        if (!AllowOverride || !MaxOverridePercent.HasValue)
            return false;

        var maxAllowed = LimitValue * (MaxOverridePercent.Value / 100m);
        return value > maxAllowed;
    }

    /// <summary>
    /// Gets the percentage of the limit that the value represents.
    /// </summary>
    public decimal GetUsagePercent(decimal value)
    {
        if (LimitValue == 0) return 100m;
        return (value / LimitValue) * 100m;
    }

    /// <summary>
    /// Checks if this threshold applies to the given substance.
    /// </summary>
    public bool AppliesToSubstance(string substanceCode)
    {
        // If SubstanceCode is null, applies to all substances
        return string.IsNullOrEmpty(SubstanceCode) ||
               SubstanceCode.Equals(substanceCode, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if this threshold applies to the given customer category.
    /// </summary>
    public bool AppliesToCustomerCategory(BusinessCategory category)
    {
        return !CustomerCategory.HasValue || CustomerCategory.Value == category;
    }

    /// <summary>
    /// Checks if this threshold applies to the given customer.
    /// </summary>
    public bool AppliesToCustomer(Guid customerId, BusinessCategory category)
    {
        // Specific customer match
        if (CustomerId.HasValue)
            return CustomerId.Value == customerId;

        // Category match
        return AppliesToCustomerCategory(category);
    }

    #endregion
}
