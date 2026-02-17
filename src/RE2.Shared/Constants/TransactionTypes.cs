namespace RE2.Shared.Constants;

/// <summary>
/// Transaction-related enums and constants for order/shipment compliance validation.
/// Per FR-018 through FR-024: Transaction compliance validation rules.
/// T123-T127: Domain model enums for User Story 4.
/// </summary>
public static class TransactionTypes
{
    /// <summary>
    /// Type of transaction being validated.
    /// </summary>
    public enum TransactionType
    {
        /// <summary>
        /// Sales order from customer.
        /// </summary>
        Order = 0,

        /// <summary>
        /// Outbound shipment to customer.
        /// </summary>
        Shipment = 1,

        /// <summary>
        /// Return from customer.
        /// </summary>
        Return = 2,

        /// <summary>
        /// Transfer between locations.
        /// </summary>
        Transfer = 3
    }

    /// <summary>
    /// Direction of transaction for cross-border validation.
    /// </summary>
    public enum TransactionDirection
    {
        /// <summary>
        /// Domestic transaction within same country.
        /// </summary>
        Internal = 0,

        /// <summary>
        /// Inbound from external source (import).
        /// </summary>
        Inbound = 1,

        /// <summary>
        /// Outbound to external destination (export).
        /// </summary>
        Outbound = 2
    }

    /// <summary>
    /// Validation status of a transaction.
    /// State machine: Pending -> (Passed | Failed | ApprovedWithOverride)
    /// </summary>
    public enum ValidationStatus
    {
        /// <summary>
        /// Validation not yet performed.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Validation passed - no violations found.
        /// </summary>
        Passed = 1,

        /// <summary>
        /// Validation failed - violations found, blocked.
        /// </summary>
        Failed = 2,

        /// <summary>
        /// Failed validation but approved by ComplianceManager override.
        /// Per FR-019a: Override approval workflow.
        /// </summary>
        ApprovedWithOverride = 3,

        /// <summary>
        /// Override was requested but rejected.
        /// </summary>
        RejectedOverride = 4
    }

    /// <summary>
    /// Type of compliance violation detected.
    /// Maps to ErrorCodes constants.
    /// </summary>
    public enum ViolationType
    {
        /// <summary>
        /// No valid licence found for substance.
        /// </summary>
        NoLicence = 0,

        /// <summary>
        /// Licence exists but has expired.
        /// </summary>
        ExpiredLicence = 1,

        /// <summary>
        /// Licence scope does not cover the activity or substance.
        /// </summary>
        InsufficientScope = 2,

        /// <summary>
        /// Quantity or frequency threshold exceeded.
        /// </summary>
        ThresholdExceeded = 3,

        /// <summary>
        /// Customer does not meet qualification requirements (GDP, approval).
        /// </summary>
        CustomerNotQualified = 4,

        /// <summary>
        /// Cross-border transaction missing required import/export permit.
        /// </summary>
        CrossBorderNoPermit = 5,

        /// <summary>
        /// Licence is suspended.
        /// </summary>
        LicenceSuspended = 6,

        /// <summary>
        /// Customer account is suspended.
        /// </summary>
        CustomerSuspended = 7,

        /// <summary>
        /// Substance not found in system.
        /// </summary>
        SubstanceNotFound = 8
    }

    /// <summary>
    /// Type of threshold being checked.
    /// Per FR-020/FR-022: Quantity and frequency limits.
    /// </summary>
    public enum ThresholdType
    {
        /// <summary>
        /// Maximum quantity per transaction.
        /// </summary>
        Quantity = 0,

        /// <summary>
        /// Maximum transaction frequency.
        /// </summary>
        Frequency = 1,

        /// <summary>
        /// Maximum total value.
        /// </summary>
        Value = 2,

        /// <summary>
        /// Cumulative quantity over period.
        /// </summary>
        CumulativeQuantity = 3
    }

    /// <summary>
    /// Time period for threshold calculation.
    /// </summary>
    public enum ThresholdPeriod
    {
        /// <summary>
        /// Per individual transaction.
        /// </summary>
        PerTransaction = 0,

        /// <summary>
        /// Daily limit.
        /// </summary>
        Daily = 1,

        /// <summary>
        /// Weekly limit.
        /// </summary>
        Weekly = 2,

        /// <summary>
        /// Monthly limit.
        /// </summary>
        Monthly = 3,

        /// <summary>
        /// Yearly limit.
        /// </summary>
        Yearly = 4
    }

    /// <summary>
    /// Override request status.
    /// </summary>
    public enum OverrideStatus
    {
        /// <summary>
        /// No override requested.
        /// </summary>
        None = 0,

        /// <summary>
        /// Override pending approval.
        /// </summary>
        Pending = 1,

        /// <summary>
        /// Override approved by ComplianceManager.
        /// </summary>
        Approved = 2,

        /// <summary>
        /// Override rejected by ComplianceManager.
        /// </summary>
        Rejected = 3
    }
}
