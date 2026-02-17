namespace RE2.ComplianceCore.Models;

/// <summary>
/// Transaction line item representing a controlled substance in a transaction.
/// External systems send ItemNumber + DataAreaId; the system resolves SubstanceCode via product attributes.
/// </summary>
public class TransactionLine
{
    /// <summary>
    /// Unique identifier for this line.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Parent transaction ID.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Line number within the transaction.
    /// </summary>
    public int LineNumber { get; set; }

    #region Product Identity (what external systems send)

    /// <summary>
    /// D365 product item number.
    /// </summary>
    public string ItemNumber { get; set; } = string.Empty;

    /// <summary>
    /// D365 legal entity.
    /// </summary>
    public string DataAreaId { get; set; } = string.Empty;

    #endregion

    #region Resolved Substance Info (populated during validation)

    /// <summary>
    /// Substance code resolved from product attributes. Null if product is not controlled.
    /// </summary>
    public string? SubstanceCode { get; set; }

    /// <summary>
    /// Substance name (denormalized).
    /// </summary>
    public string? SubstanceName { get; set; }

    /// <summary>
    /// Product description from D365.
    /// </summary>
    public string? ProductDescription { get; set; }

    #endregion

    #region Product Details

    /// <summary>
    /// Batch/lot number (for traceability).
    /// </summary>
    public string? BatchNumber { get; set; }

    #endregion

    #region Quantity & Value

    /// <summary>
    /// Quantity of the line item.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit of measure.
    /// </summary>
    public string UnitOfMeasure { get; set; } = "EA";

    /// <summary>
    /// Quantity in base unit (for threshold comparison).
    /// </summary>
    public decimal BaseUnitQuantity { get; set; }

    /// <summary>
    /// Base unit of measure (e.g., "g" for grams, "mg" for milligrams).
    /// </summary>
    public string BaseUnit { get; set; } = "g";

    /// <summary>
    /// Line value (quantity x unit price).
    /// </summary>
    public decimal? LineValue { get; set; }

    /// <summary>
    /// Unit price.
    /// </summary>
    public decimal? UnitPrice { get; set; }

    #endregion

    #region Licence Coverage

    /// <summary>
    /// Whether this line has valid licence coverage.
    /// Set during validation.
    /// </summary>
    public bool HasLicenceCoverage { get; set; }

    /// <summary>
    /// Licence ID covering this line (if any).
    /// </summary>
    public Guid? CoveringLicenceId { get; set; }

    /// <summary>
    /// Licence number covering this line (denormalized).
    /// </summary>
    public string? CoveringLicenceNumber { get; set; }

    #endregion

    #region Validation Results

    /// <summary>
    /// Whether this line passed validation.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation error message for this line (if any).
    /// </summary>
    public string? ValidationError { get; set; }

    /// <summary>
    /// Error code for this line (if any).
    /// </summary>
    public string? ErrorCode { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Reference to controlled substance (if loaded).
    /// </summary>
    public ControlledSubstance? Substance { get; set; }

    /// <summary>
    /// Reference to covering licence (if loaded).
    /// </summary>
    public Licence? CoveringLicence { get; set; }

    #endregion

    #region Methods

    /// <summary>
    /// Marks this line as having valid licence coverage.
    /// </summary>
    public void SetLicenceCoverage(Licence licence)
    {
        HasLicenceCoverage = true;
        CoveringLicenceId = licence.LicenceId;
        CoveringLicenceNumber = licence.LicenceNumber;
        CoveringLicence = licence;
        IsValid = true;
        ValidationError = null;
        ErrorCode = null;
    }

    /// <summary>
    /// Marks this line as failing validation.
    /// </summary>
    public void SetValidationError(string errorCode, string errorMessage)
    {
        HasLicenceCoverage = false;
        CoveringLicenceId = null;
        CoveringLicenceNumber = null;
        IsValid = false;
        ErrorCode = errorCode;
        ValidationError = errorMessage;
    }

    /// <summary>
    /// Clears validation state for re-validation.
    /// </summary>
    public void ClearValidation()
    {
        HasLicenceCoverage = false;
        CoveringLicenceId = null;
        CoveringLicenceNumber = null;
        IsValid = false;
        ValidationError = null;
        ErrorCode = null;
    }

    #endregion
}
