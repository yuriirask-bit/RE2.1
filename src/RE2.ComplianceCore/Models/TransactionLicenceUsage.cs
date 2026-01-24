namespace RE2.ComplianceCore.Models;

/// <summary>
/// Tracks which licences were used to cover a transaction.
/// T126: TransactionLicenceUsage domain model for licence coverage tracking.
/// </summary>
public class TransactionLicenceUsage
{
    /// <summary>
    /// Unique identifier for this usage record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Transaction ID this usage belongs to.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Licence ID being used.
    /// </summary>
    public Guid LicenceId { get; set; }

    /// <summary>
    /// Licence number (denormalized for display).
    /// </summary>
    public string LicenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Licence type name (denormalized).
    /// </summary>
    public string? LicenceTypeName { get; set; }

    /// <summary>
    /// Holder type (Company or Customer).
    /// </summary>
    public string HolderType { get; set; } = string.Empty;

    /// <summary>
    /// Holder ID.
    /// </summary>
    public Guid HolderId { get; set; }

    /// <summary>
    /// Holder name (denormalized).
    /// </summary>
    public string? HolderName { get; set; }

    #region Coverage Details

    /// <summary>
    /// List of line numbers this licence covers.
    /// </summary>
    public List<int> CoveredLineNumbers { get; set; } = new();

    /// <summary>
    /// List of substance IDs this licence covers for this transaction.
    /// </summary>
    public List<Guid> CoveredSubstanceIds { get; set; } = new();

    /// <summary>
    /// Total quantity covered by this licence.
    /// </summary>
    public decimal CoveredQuantity { get; set; }

    /// <summary>
    /// Unit of measure for covered quantity.
    /// </summary>
    public string CoveredQuantityUnit { get; set; } = "g";

    #endregion

    #region Licence Status at Time of Use

    /// <summary>
    /// Licence status at time of validation.
    /// </summary>
    public string LicenceStatus { get; set; } = string.Empty;

    /// <summary>
    /// Licence expiry date at time of validation.
    /// </summary>
    public DateOnly? LicenceExpiryDate { get; set; }

    /// <summary>
    /// Whether licence was expiring soon at time of use (warning).
    /// </summary>
    public bool WasExpiringSoon { get; set; }

    /// <summary>
    /// Days until expiry at time of use.
    /// </summary>
    public int? DaysUntilExpiry { get; set; }

    #endregion

    #region Audit

    /// <summary>
    /// Date/time when this usage was recorded.
    /// </summary>
    public DateTime RecordedAt { get; set; }

    #endregion

    #region Navigation

    /// <summary>
    /// Reference to the licence (if loaded).
    /// </summary>
    public Licence? Licence { get; set; }

    /// <summary>
    /// Reference to the transaction (if loaded).
    /// </summary>
    public Transaction? Transaction { get; set; }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a usage record from a licence and transaction line.
    /// </summary>
    public static TransactionLicenceUsage FromLicence(
        Guid transactionId,
        Licence licence,
        IEnumerable<TransactionLine> coveredLines)
    {
        var linesList = coveredLines.ToList();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        int? daysUntilExpiry = null;
        if (licence.ExpiryDate.HasValue)
        {
            daysUntilExpiry = licence.ExpiryDate.Value.DayNumber - today.DayNumber;
        }

        return new TransactionLicenceUsage
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            LicenceId = licence.LicenceId,
            LicenceNumber = licence.LicenceNumber,
            LicenceTypeName = licence.LicenceType?.Name,
            HolderType = licence.HolderType,
            HolderId = licence.HolderId,
            CoveredLineNumbers = linesList.Select(l => l.LineNumber).ToList(),
            CoveredSubstanceIds = linesList.Select(l => l.SubstanceId).Distinct().ToList(),
            CoveredQuantity = linesList.Sum(l => l.BaseUnitQuantity),
            CoveredQuantityUnit = linesList.FirstOrDefault()?.BaseUnit ?? "g",
            LicenceStatus = licence.Status,
            LicenceExpiryDate = licence.ExpiryDate,
            WasExpiringSoon = daysUntilExpiry.HasValue && daysUntilExpiry.Value <= 90,
            DaysUntilExpiry = daysUntilExpiry,
            RecordedAt = DateTime.UtcNow,
            Licence = licence
        };
    }

    #endregion
}
