using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.Dataverse;

/// <summary>
/// Hosted service that ensures all required Dataverse OptionSet values exist at startup.
/// Adds missing choice values for BusinessCategory, ApprovalStatus, and GdpQualificationStatus
/// on the phr_customercomplianceextension table.
/// </summary>
public class DataverseOptionSetSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataverseOptionSetSeeder> _logger;

    public DataverseOptionSetSeeder(
        IServiceProvider serviceProvider,
        ILogger<DataverseOptionSetSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<IDataverseClient>();

            if (!client.IsConnected)
            {
                _logger.LogWarning("Dataverse client not connected, skipping OptionSet seeding");
                return;
            }

            await SeedOptionSetAsync(client, "phr_customercomplianceextension", "phr_businesscategory",
                GetBusinessCategoryOptions(), cancellationToken);

            await SeedOptionSetAsync(client, "phr_customercomplianceextension", "phr_approvalstatus",
                GetApprovalStatusOptions(), cancellationToken);

            await SeedOptionSetAsync(client, "phr_customercomplianceextension", "phr_gdpqualificationstatus",
                GetGdpQualificationStatusOptions(), cancellationToken);

            await SeedOptionSetAsync(client, "phr_gdpwarehouseextension", "phr_gdpsitetype",
                GetGdpSiteTypeOptions(), cancellationToken);

            _logger.LogInformation("Dataverse OptionSet seeding completed");
        }
        catch (Exception ex)
        {
            // Non-fatal: log and continue — the app should still start even if seeding fails
            _logger.LogWarning(ex, "Dataverse OptionSet seeding failed (non-fatal). " +
                "OptionSet values may need to be added manually in the Power Platform admin center.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedOptionSetAsync(
        IDataverseClient client,
        string entityLogicalName,
        string attributeLogicalName,
        Dictionary<int, string> requiredValues,
        CancellationToken cancellationToken)
    {
        try
        {
            // Retrieve existing OptionSet metadata
            var retrieveRequest = new RetrieveAttributeRequest
            {
                EntityLogicalName = entityLogicalName,
                LogicalName = attributeLogicalName,
                RetrieveAsIfPublished = true
            };

            var retrieveResponse = await client.ExecuteAsync(retrieveRequest, cancellationToken);
            var metadata = (PicklistAttributeMetadata)((RetrieveAttributeResponse)retrieveResponse).AttributeMetadata;
            var existingValues = metadata.OptionSet.Options
                .Select(o => o.Value!.Value)
                .ToHashSet();

            var added = 0;
            foreach (var (value, label) in requiredValues)
            {
                if (existingValues.Contains(value))
                {
                    continue;
                }

                var insertRequest = new InsertOptionValueRequest
                {
                    EntityLogicalName = entityLogicalName,
                    AttributeLogicalName = attributeLogicalName,
                    Value = value,
                    Label = new Label(label, 1033) // English
                };

                await client.ExecuteAsync(insertRequest, cancellationToken);
                added++;
                _logger.LogInformation("Added OptionSet value {Value} '{Label}' to {Entity}.{Attribute}",
                    value, label, entityLogicalName, attributeLogicalName);
            }

            if (added > 0)
            {
                // Publish the entity to make changes effective
                var publishRequest = new PublishXmlRequest
                {
                    ParameterXml = $"<importexportxml><entities><entity>{entityLogicalName}</entity></entities></importexportxml>"
                };
                await client.ExecuteAsync(publishRequest, cancellationToken);
                _logger.LogInformation("Published {Entity} after adding {Count} OptionSet values to {Attribute}",
                    entityLogicalName, added, attributeLogicalName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to seed OptionSet {Entity}.{Attribute} (non-fatal)",
                entityLogicalName, attributeLogicalName);
        }
    }

    private static Dictionary<int, string> GetBusinessCategoryOptions() => new()
    {
        [(int)BusinessCategory.HospitalPharmacy] = "Hospital Pharmacy",
        [(int)BusinessCategory.CommunityPharmacy] = "Community Pharmacy",
        [(int)BusinessCategory.Veterinarian] = "Veterinarian",
        [(int)BusinessCategory.Manufacturer] = "Manufacturer",
        [(int)BusinessCategory.WholesalerEU] = "Wholesaler EU",
        [(int)BusinessCategory.WholesalerNonEU] = "Wholesaler Non-EU",
        [(int)BusinessCategory.ResearchInstitution] = "Research Institution"
    };

    private static Dictionary<int, string> GetApprovalStatusOptions() => new()
    {
        [(int)ApprovalStatus.Pending] = "Pending",
        [(int)ApprovalStatus.Approved] = "Approved",
        [(int)ApprovalStatus.ConditionallyApproved] = "Conditionally Approved",
        [(int)ApprovalStatus.Rejected] = "Rejected",
        [(int)ApprovalStatus.Suspended] = "Suspended"
    };

    private static Dictionary<int, string> GetGdpQualificationStatusOptions() => new()
    {
        [(int)GdpQualificationStatus.NotRequired] = "Not Required",
        [(int)GdpQualificationStatus.Pending] = "Pending",
        [(int)GdpQualificationStatus.Approved] = "Approved",
        [(int)GdpQualificationStatus.ConditionallyApproved] = "Conditionally Approved",
        [(int)GdpQualificationStatus.Rejected] = "Rejected",
        [(int)GdpQualificationStatus.UnderReview] = "Under Review"
    };

    private static Dictionary<int, string> GetGdpSiteTypeOptions() => new()
    {
        [(int)GdpSiteType.Warehouse] = "Warehouse",
        [(int)GdpSiteType.CrossDock] = "Cross Dock",
        [(int)GdpSiteType.TransportHub] = "Transport Hub"
    };
}
