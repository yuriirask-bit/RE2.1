using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCli.Commands;

/// <summary>
/// T052d: Lookup licence command implementation.
/// Accepts licence number via args, returns licence details JSON to stdout.
/// </summary>
public class LookupLicenceCommand
{
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    public LookupLicenceCommand(IServiceProvider serviceProvider, JsonSerializerOptions jsonOptions)
    {
        _serviceProvider = serviceProvider;
        _jsonOptions = jsonOptions;
    }

    public async Task<int> ExecuteAsync(LookupLicenceOptions options)
    {
        if (string.IsNullOrEmpty(options.LicenceId) && string.IsNullOrEmpty(options.LicenceNumber))
        {
            OutputError("Either --id or --number must be provided.");
            return 1;
        }

        var licenceService = _serviceProvider.GetRequiredService<ILicenceService>();
        var substanceMappingService = _serviceProvider.GetService<ILicenceSubstanceMappingService>();

        Licence? licence = null;

        // Lookup by ID
        if (!string.IsNullOrEmpty(options.LicenceId))
        {
            if (!Guid.TryParse(options.LicenceId, out var licenceId))
            {
                OutputError($"Invalid licence ID format: {options.LicenceId}");
                return 1;
            }

            licence = await licenceService.GetByIdAsync(licenceId);
        }
        // Lookup by number
        else if (!string.IsNullOrEmpty(options.LicenceNumber))
        {
            licence = await licenceService.GetByLicenceNumberAsync(options.LicenceNumber);
        }

        if (licence == null)
        {
            OutputError("Licence not found.");
            return 1;
        }

        // Build output
        var output = new LicenceDetailOutput
        {
            LicenceId = licence.LicenceId,
            LicenceNumber = licence.LicenceNumber,
            LicenceTypeId = licence.LicenceTypeId,
            LicenceTypeName = licence.LicenceType?.Name,
            HolderType = licence.HolderType,
            HolderId = licence.HolderId,
            Status = licence.Status,
            IssueDate = licence.IssueDate.ToString("yyyy-MM-dd"),
            ExpiryDate = licence.ExpiryDate?.ToString("yyyy-MM-dd"),
            IsExpired = licence.IsExpired(),
            DaysUntilExpiry = licence.ExpiryDate.HasValue
                ? (int)(licence.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow).TotalDays
                : null,
            IssuingAuthority = licence.IssuingAuthority,
            PermittedActivities = licence.PermittedActivities.ToString(),
            Scope = licence.Scope,
            CreatedDate = licence.CreatedDate,
            ModifiedDate = licence.ModifiedDate
        };

        // Include substance mappings if requested
        if (options.IncludeSubstances && substanceMappingService != null)
        {
            var mappings = await substanceMappingService.GetByLicenceIdAsync(licence.LicenceId);
            output.SubstanceMappings = mappings.Select(m => new SubstanceMappingOutput
            {
                MappingId = m.MappingId,
                SubstanceCode = m.SubstanceCode,
                SubstanceName = m.Substance?.SubstanceName,
                MaxQuantityPerTransaction = m.MaxQuantityPerTransaction,
                MaxQuantityPerPeriod = m.MaxQuantityPerPeriod,
                PeriodType = m.PeriodType,
                EffectiveDate = m.EffectiveDate.ToString("yyyy-MM-dd"),
                ExpiryDate = m.ExpiryDate?.ToString("yyyy-MM-dd")
            }).ToList();
        }

        // Include documents if requested
        if (options.IncludeDocuments)
        {
            var documents = await licenceService.GetDocumentsAsync(licence.LicenceId);
            output.Documents = documents.Select(d => new DocumentOutput
            {
                DocumentId = d.DocumentId,
                FileName = d.FileName,
                DocumentType = d.DocumentType.ToString(),
                UploadedDate = d.UploadedDate,
                UploadedBy = d.UploadedBy.ToString(),
                FileSize = d.FileSizeBytes
            }).ToList();
        }

        Console.WriteLine(JsonSerializer.Serialize(output, _jsonOptions));
        return 0;
    }

    private void OutputError(string message)
    {
        var error = new { error = message, errorType = "LookupError" };
        Console.WriteLine(JsonSerializer.Serialize(error, _jsonOptions));
    }
}

#region Output DTOs

public class LicenceDetailOutput
{
    public Guid LicenceId { get; set; }
    public string LicenceNumber { get; set; } = string.Empty;
    public Guid LicenceTypeId { get; set; }
    public string? LicenceTypeName { get; set; }
    public string HolderType { get; set; } = string.Empty;
    public Guid HolderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? IssueDate { get; set; }
    public string? ExpiryDate { get; set; }
    public bool IsExpired { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public string? IssuingAuthority { get; set; }
    public string? PermittedActivities { get; set; }
    public string? Scope { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public List<SubstanceMappingOutput>? SubstanceMappings { get; set; }
    public List<DocumentOutput>? Documents { get; set; }
}

public class SubstanceMappingOutput
{
    public Guid MappingId { get; set; }
    public string SubstanceCode { get; set; } = string.Empty;
    public string? SubstanceName { get; set; }
    public decimal? MaxQuantityPerTransaction { get; set; }
    public decimal? MaxQuantityPerPeriod { get; set; }
    public string? PeriodType { get; set; }
    public string? EffectiveDate { get; set; }
    public string? ExpiryDate { get; set; }
}

public class DocumentOutput
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? DocumentType { get; set; }
    public DateTime UploadedDate { get; set; }
    public string? UploadedBy { get; set; }
    public long? FileSize { get; set; }
}

#endregion
