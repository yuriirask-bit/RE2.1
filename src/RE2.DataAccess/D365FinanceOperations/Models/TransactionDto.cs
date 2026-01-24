using RE2.ComplianceCore.Models;
using static RE2.Shared.Constants.TransactionTypes;

namespace RE2.DataAccess.D365FinanceOperations.Models;

/// <summary>
/// Data transfer object for D365 F&O PharmaComplianceTransactionEntity virtual data entity.
/// T133: DTO for Transaction mapping to D365 F&O OData integration.
/// Stored in D365 F&O via virtual data entity.
/// </summary>
public class TransactionDto
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// External transaction ID from ERP (e.g., sales order number).
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Transaction type as integer per TransactionType enum.
    /// </summary>
    public int TransactionType { get; set; }

    /// <summary>
    /// Transaction direction as integer per TransactionDirection enum.
    /// </summary>
    public int Direction { get; set; }

    /// <summary>
    /// Customer ID reference.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Customer name for display.
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Origin country ISO 3166-1 alpha-2 code.
    /// </summary>
    public string OriginCountry { get; set; } = "NL";

    /// <summary>
    /// Destination country ISO 3166-1 alpha-2 code (for cross-border).
    /// </summary>
    public string? DestinationCountry { get; set; }

    /// <summary>
    /// Date of the transaction.
    /// </summary>
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// Total quantity in base unit.
    /// </summary>
    public decimal TotalQuantity { get; set; }

    /// <summary>
    /// Total monetary value.
    /// </summary>
    public decimal? TotalValue { get; set; }

    /// <summary>
    /// Business status (e.g., "Pending", "Completed").
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Validation status as integer per ValidationStatus enum.
    /// </summary>
    public int ValidationStatus { get; set; }

    /// <summary>
    /// Date/time when validation was performed.
    /// </summary>
    public DateTime? ValidationDate { get; set; }

    /// <summary>
    /// Whether transaction requires override approval.
    /// </summary>
    public bool RequiresOverride { get; set; }

    /// <summary>
    /// Override status as integer per OverrideStatus enum.
    /// </summary>
    public int OverrideStatus { get; set; }

    /// <summary>
    /// User who approved/rejected the override.
    /// </summary>
    public string? OverrideDecisionBy { get; set; }

    /// <summary>
    /// Date/time of override decision.
    /// </summary>
    public DateTime? OverrideDecisionDate { get; set; }

    /// <summary>
    /// Justification for override approval.
    /// </summary>
    public string? OverrideJustification { get; set; }

    /// <summary>
    /// Reason for override rejection.
    /// </summary>
    public string? OverrideRejectionReason { get; set; }

    /// <summary>
    /// Integration system ID that submitted the transaction.
    /// Per FR-061: Record calling system identity for audit.
    /// </summary>
    public string? IntegrationSystemId { get; set; }

    /// <summary>
    /// External user ID from calling system.
    /// </summary>
    public string? ExternalUserId { get; set; }

    /// <summary>
    /// Warehouse site ID for warehouse operations.
    /// </summary>
    public string? WarehouseSiteId { get; set; }

    /// <summary>
    /// Compliance warnings as JSON array.
    /// </summary>
    public string? ComplianceWarningsJson { get; set; }

    /// <summary>
    /// Compliance errors as JSON array.
    /// </summary>
    public string? ComplianceErrorsJson { get; set; }

    /// <summary>
    /// When the transaction was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Who created the transaction.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// When the transaction was last modified.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Who last modified the transaction.
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Converts DTO to domain model.
    /// </summary>
    /// <returns>Transaction domain model.</returns>
    public Transaction ToDomainModel()
    {
        var transaction = new Transaction
        {
            Id = TransactionId,
            ExternalId = ExternalId,
            TransactionType = (Shared.Constants.TransactionTypes.TransactionType)TransactionType,
            Direction = (TransactionDirection)Direction,
            CustomerId = CustomerId,
            CustomerName = CustomerName,
            OriginCountry = OriginCountry,
            DestinationCountry = DestinationCountry,
            TransactionDate = TransactionDate,
            TotalQuantity = TotalQuantity,
            TotalValue = TotalValue,
            Status = Status,
            ValidationStatus = (Shared.Constants.TransactionTypes.ValidationStatus)ValidationStatus,
            ValidationDate = ValidationDate,
            RequiresOverride = RequiresOverride,
            OverrideStatus = (Shared.Constants.TransactionTypes.OverrideStatus)OverrideStatus,
            OverrideDecisionBy = OverrideDecisionBy,
            OverrideDecisionDate = OverrideDecisionDate,
            OverrideJustification = OverrideJustification,
            OverrideRejectionReason = OverrideRejectionReason,
            IntegrationSystemId = IntegrationSystemId,
            ExternalUserId = ExternalUserId,
            WarehouseSiteId = WarehouseSiteId,
            CreatedAt = CreatedAt,
            CreatedBy = CreatedBy,
            ModifiedAt = ModifiedAt,
            ModifiedBy = ModifiedBy
        };

        // Deserialize warnings and errors if present
        if (!string.IsNullOrEmpty(ComplianceWarningsJson))
        {
            transaction.ComplianceWarnings = System.Text.Json.JsonSerializer
                .Deserialize<List<string>>(ComplianceWarningsJson) ?? new List<string>();
        }

        if (!string.IsNullOrEmpty(ComplianceErrorsJson))
        {
            transaction.ComplianceErrors = System.Text.Json.JsonSerializer
                .Deserialize<List<string>>(ComplianceErrorsJson) ?? new List<string>();
        }

        return transaction;
    }

    /// <summary>
    /// Converts domain model to DTO.
    /// </summary>
    /// <param name="model">The domain model to convert.</param>
    /// <returns>TransactionDto for D365 F&O persistence.</returns>
    public static TransactionDto FromDomainModel(Transaction model)
    {
        return new TransactionDto
        {
            TransactionId = model.Id,
            ExternalId = model.ExternalId,
            TransactionType = (int)model.TransactionType,
            Direction = (int)model.Direction,
            CustomerId = model.CustomerId,
            CustomerName = model.CustomerName,
            OriginCountry = model.OriginCountry,
            DestinationCountry = model.DestinationCountry,
            TransactionDate = model.TransactionDate,
            TotalQuantity = model.TotalQuantity,
            TotalValue = model.TotalValue,
            Status = model.Status,
            ValidationStatus = (int)model.ValidationStatus,
            ValidationDate = model.ValidationDate,
            RequiresOverride = model.RequiresOverride,
            OverrideStatus = (int)model.OverrideStatus,
            OverrideDecisionBy = model.OverrideDecisionBy,
            OverrideDecisionDate = model.OverrideDecisionDate,
            OverrideJustification = model.OverrideJustification,
            OverrideRejectionReason = model.OverrideRejectionReason,
            IntegrationSystemId = model.IntegrationSystemId,
            ExternalUserId = model.ExternalUserId,
            WarehouseSiteId = model.WarehouseSiteId,
            ComplianceWarningsJson = model.ComplianceWarnings.Count > 0
                ? System.Text.Json.JsonSerializer.Serialize(model.ComplianceWarnings)
                : null,
            ComplianceErrorsJson = model.ComplianceErrors.Count > 0
                ? System.Text.Json.JsonSerializer.Serialize(model.ComplianceErrors)
                : null,
            CreatedAt = model.CreatedAt,
            CreatedBy = model.CreatedBy,
            ModifiedAt = model.ModifiedAt,
            ModifiedBy = model.ModifiedBy
        };
    }
}

/// <summary>
/// Data transfer object for D365 F&O PharmaComplianceTransactionLineEntity virtual data entity.
/// T133: DTO for TransactionLine mapping to D365 F&O OData integration.
/// </summary>
public class TransactionLineDto
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid TransactionLineId { get; set; }

    /// <summary>
    /// Parent transaction ID.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Line number within the transaction.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Controlled substance ID.
    /// </summary>
    public Guid SubstanceId { get; set; }

    /// <summary>
    /// Substance internal code.
    /// </summary>
    public string SubstanceCode { get; set; } = string.Empty;

    /// <summary>
    /// Product/item number from ERP.
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// Product description.
    /// </summary>
    public string? ProductDescription { get; set; }

    /// <summary>
    /// Batch/lot number.
    /// </summary>
    public string? BatchNumber { get; set; }

    /// <summary>
    /// Quantity in transaction unit.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit of measure.
    /// </summary>
    public string UnitOfMeasure { get; set; } = string.Empty;

    /// <summary>
    /// Quantity in base unit (for threshold comparison).
    /// </summary>
    public decimal BaseUnitQuantity { get; set; }

    /// <summary>
    /// Base unit (e.g., "g", "mg").
    /// </summary>
    public string BaseUnit { get; set; } = string.Empty;

    /// <summary>
    /// Line monetary value.
    /// </summary>
    public decimal? LineValue { get; set; }

    /// <summary>
    /// Unit price.
    /// </summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// Whether this line is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation error message for this line.
    /// </summary>
    public string? ValidationError { get; set; }

    /// <summary>
    /// Error code for this line.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Whether this line has valid licence coverage.
    /// </summary>
    public bool HasLicenceCoverage { get; set; }

    /// <summary>
    /// Licence ID covering this line.
    /// </summary>
    public Guid? CoveringLicenceId { get; set; }

    /// <summary>
    /// Converts DTO to domain model.
    /// </summary>
    /// <returns>TransactionLine domain model.</returns>
    public TransactionLine ToDomainModel()
    {
        return new TransactionLine
        {
            Id = TransactionLineId,
            TransactionId = TransactionId,
            LineNumber = LineNumber,
            SubstanceId = SubstanceId,
            SubstanceCode = SubstanceCode,
            ProductCode = ProductCode,
            ProductDescription = ProductDescription,
            BatchNumber = BatchNumber,
            Quantity = Quantity,
            UnitOfMeasure = UnitOfMeasure,
            BaseUnitQuantity = BaseUnitQuantity,
            BaseUnit = BaseUnit,
            LineValue = LineValue,
            UnitPrice = UnitPrice,
            IsValid = IsValid,
            ValidationError = ValidationError,
            ErrorCode = ErrorCode,
            HasLicenceCoverage = HasLicenceCoverage,
            CoveringLicenceId = CoveringLicenceId
        };
    }

    /// <summary>
    /// Converts domain model to DTO.
    /// </summary>
    /// <param name="model">The domain model to convert.</param>
    /// <returns>TransactionLineDto for D365 F&O persistence.</returns>
    public static TransactionLineDto FromDomainModel(TransactionLine model)
    {
        return new TransactionLineDto
        {
            TransactionLineId = model.Id,
            TransactionId = model.TransactionId,
            LineNumber = model.LineNumber,
            SubstanceId = model.SubstanceId,
            SubstanceCode = model.SubstanceCode,
            ProductCode = model.ProductCode,
            ProductDescription = model.ProductDescription,
            BatchNumber = model.BatchNumber,
            Quantity = model.Quantity,
            UnitOfMeasure = model.UnitOfMeasure,
            BaseUnitQuantity = model.BaseUnitQuantity,
            BaseUnit = model.BaseUnit,
            LineValue = model.LineValue,
            UnitPrice = model.UnitPrice,
            IsValid = model.IsValid,
            ValidationError = model.ValidationError,
            ErrorCode = model.ErrorCode,
            HasLicenceCoverage = model.HasLicenceCoverage,
            CoveringLicenceId = model.CoveringLicenceId
        };
    }
}

/// <summary>
/// OData response wrapper for Transaction queries.
/// </summary>
public class TransactionODataResponse
{
    /// <summary>
    /// Collection of transaction DTOs.
    /// </summary>
    public List<TransactionDto> value { get; set; } = new();
}

/// <summary>
/// OData response wrapper for TransactionLine queries.
/// </summary>
public class TransactionLineODataResponse
{
    /// <summary>
    /// Collection of transaction line DTOs.
    /// </summary>
    public List<TransactionLineDto> value { get; set; } = new();
}
