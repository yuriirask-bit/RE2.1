using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse + D365FO composite implementation of ICustomerRepository.
/// D365FO CustomersV3 provides read-only master data via OData.
/// Dataverse phr_customercomplianceextension stores compliance extensions.
/// </summary>
public class DataverseCustomerRepository : ICustomerRepository
{
    private readonly IDataverseClient _client;
    private readonly ID365FoClient _d365FoClient;
    private readonly ILogger<DataverseCustomerRepository> _logger;
    private const string ExtensionEntityName = "phr_customercomplianceextension";

    public DataverseCustomerRepository(
        IDataverseClient client,
        ID365FoClient d365FoClient,
        ILogger<DataverseCustomerRepository> logger)
    {
        _client = client;
        _d365FoClient = d365FoClient;
        _logger = logger;
    }

    #region D365FO Customer Queries

    public async Task<IEnumerable<Customer>> GetAllD365CustomersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _d365FoClient.GetAsync<D365FinanceOperations.Models.CustomerODataResponse>(
                "CustomersV3",
                "$select=CustomerAccount,dataAreaId,OrganizationName,AddressCountryRegionId",
                cancellationToken);

            if (response?.Value == null)
                return Enumerable.Empty<Customer>();

            var customers = response.Value.Select(dto => dto.ToDomainModel()).ToList();

            // Merge compliance extensions where they exist
            foreach (var customer in customers)
            {
                var extension = await GetComplianceExtensionAsync(customer.CustomerAccount, customer.DataAreaId, cancellationToken);
                if (extension != null)
                {
                    extension.ApplyToDomainModel(customer);
                }
            }

            return customers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving D365FO customers");
            return Enumerable.Empty<Customer>();
        }
    }

    public async Task<Customer?> GetD365CustomerAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _d365FoClient.GetByKeyAsync<D365FinanceOperations.Models.CustomerDto>(
                "CustomersV3",
                $"CustomerAccount='{customerAccount}',dataAreaId='{dataAreaId}'",
                cancellationToken);

            if (response == null)
                return null;

            var customer = response.ToDomainModel();

            // Merge compliance extension if exists
            var extension = await GetComplianceExtensionAsync(customerAccount, dataAreaId, cancellationToken);
            if (extension != null)
            {
                extension.ApplyToDomainModel(customer);
            }

            return customer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving D365FO customer {Account}/{DataArea}", customerAccount, dataAreaId);
            return null;
        }
    }

    #endregion

    #region Composite Queries

    public async Task<Customer?> GetByAccountAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default)
    {
        // Get compliance extension first (this is the primary lookup for compliance-configured customers)
        var extension = await GetComplianceExtensionAsync(customerAccount, dataAreaId, cancellationToken);
        if (extension == null)
            return null;

        var customer = extension.ToDomainModel();

        // Merge D365FO master data
        var d365Customer = await GetD365CustomerAsync(customerAccount, dataAreaId, cancellationToken);
        if (d365Customer != null)
        {
            customer.OrganizationName = d365Customer.OrganizationName;
            customer.AddressCountryRegionId = d365Customer.AddressCountryRegionId;
        }

        return customer;
    }

    public async Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(ExtensionEntityName) { ColumnSet = new ColumnSet(true) };
        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);

        var customers = new List<Customer>();
        foreach (var entity in result.Entities)
        {
            var dto = MapToDto(entity);
            var customer = dto.ToDomainModel();

            // Merge D365FO data
            var d365Customer = await GetD365CustomerAsync(
                customer.CustomerAccount, customer.DataAreaId, cancellationToken);
            if (d365Customer != null)
            {
                customer.OrganizationName = d365Customer.OrganizationName;
                customer.AddressCountryRegionId = d365Customer.AddressCountryRegionId;
            }

            customers.Add(customer);
        }

        return customers;
    }

    public async Task<IEnumerable<Customer>> GetByApprovalStatusAsync(ApprovalStatus status, CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(ExtensionEntityName)
        {
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("phr_approvalstatus", ConditionOperator.Equal, (int)status)
                }
            }
        };

        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        return await MergeD365DataForEntities(result.Entities, cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetByBusinessCategoryAsync(BusinessCategory category, CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(ExtensionEntityName)
        {
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("phr_businesscategory", ConditionOperator.Equal, (int)category)
                }
            }
        };

        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        return await MergeD365DataForEntities(result.Entities, cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetSuspendedAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(ExtensionEntityName)
        {
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("phr_issuspended", ConditionOperator.Equal, true)
                }
            }
        };

        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        return await MergeD365DataForEntities(result.Entities, cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetReVerificationDueAsync(int daysAhead, CancellationToken cancellationToken = default)
    {
        var dueDate = DateTime.UtcNow.AddDays(daysAhead);

        var query = new QueryExpression(ExtensionEntityName)
        {
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("phr_nextreverificationdate", ConditionOperator.LessEqual, dueDate),
                    new ConditionExpression("phr_nextreverificationdate", ConditionOperator.NotNull)
                }
            }
        };

        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        return await MergeD365DataForEntities(result.Entities, cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetCanTransactAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(ExtensionEntityName)
        {
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression
            {
                FilterOperator = LogicalOperator.And,
                Conditions =
                {
                    new ConditionExpression("phr_issuspended", ConditionOperator.Equal, false)
                },
                Filters =
                {
                    new FilterExpression
                    {
                        FilterOperator = LogicalOperator.Or,
                        Conditions =
                        {
                            new ConditionExpression("phr_approvalstatus", ConditionOperator.Equal, (int)ApprovalStatus.Approved),
                            new ConditionExpression("phr_approvalstatus", ConditionOperator.Equal, (int)ApprovalStatus.ConditionallyApproved)
                        }
                    }
                }
            }
        };

        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        return await MergeD365DataForEntities(result.Entities, cancellationToken);
    }

    #endregion

    #region Compliance Extension CRUD

    public async Task<Guid> SaveComplianceExtensionAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var dto = CustomerComplianceExtensionDto.FromDomainModel(customer);
        var entity = MapToEntity(dto);
        return await _client.CreateAsync(entity, cancellationToken);
    }

    public async Task UpdateComplianceExtensionAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var dto = CustomerComplianceExtensionDto.FromDomainModel(customer);
        var entity = MapToEntity(dto);
        await _client.UpdateAsync(entity, cancellationToken);
    }

    public async Task DeleteComplianceExtensionAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var extension = await GetComplianceExtensionAsync(customerAccount, dataAreaId, cancellationToken);
        if (extension != null)
        {
            await _client.DeleteAsync(ExtensionEntityName, extension.phr_complianceextensionid, cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var extension = await GetComplianceExtensionAsync(customerAccount, dataAreaId, cancellationToken);
        return extension != null;
    }

    public async Task<IEnumerable<Customer>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        // Search D365FO customers by organization name
        try
        {
            var response = await _d365FoClient.GetAsync<D365FinanceOperations.Models.CustomerODataResponse>(
                "CustomersV3",
                $"$filter=contains(OrganizationName,'{searchTerm}')&$select=CustomerAccount,dataAreaId,OrganizationName,AddressCountryRegionId",
                cancellationToken);

            if (response?.Value == null)
                return Enumerable.Empty<Customer>();

            var customers = new List<Customer>();
            foreach (var dto in response.Value)
            {
                var customer = dto.ToDomainModel();
                var extension = await GetComplianceExtensionAsync(customer.CustomerAccount, customer.DataAreaId, cancellationToken);
                if (extension != null)
                {
                    extension.ApplyToDomainModel(customer);
                }
                customers.Add(customer);
            }

            return customers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching customers by name '{SearchTerm}'", searchTerm);
            return Enumerable.Empty<Customer>();
        }
    }

    #endregion

    #region Private Helpers

    private async Task<CustomerComplianceExtensionDto?> GetComplianceExtensionAsync(
        string customerAccount, string dataAreaId, CancellationToken cancellationToken)
    {
        var query = new QueryExpression(ExtensionEntityName)
        {
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression
            {
                FilterOperator = LogicalOperator.And,
                Conditions =
                {
                    new ConditionExpression("phr_customeraccount", ConditionOperator.Equal, customerAccount),
                    new ConditionExpression("phr_dataareaid", ConditionOperator.Equal, dataAreaId)
                }
            }
        };

        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        var entity = result.Entities.FirstOrDefault();
        return entity != null ? MapToDto(entity) : null;
    }

    private async Task<List<Customer>> MergeD365DataForEntities(
        DataCollection<Entity> entities, CancellationToken cancellationToken)
    {
        var customers = new List<Customer>();
        foreach (var entity in entities)
        {
            var dto = MapToDto(entity);
            var customer = dto.ToDomainModel();

            var d365Customer = await GetD365CustomerAsync(
                customer.CustomerAccount, customer.DataAreaId, cancellationToken);
            if (d365Customer != null)
            {
                customer.OrganizationName = d365Customer.OrganizationName;
                customer.AddressCountryRegionId = d365Customer.AddressCountryRegionId;
            }

            customers.Add(customer);
        }
        return customers;
    }

    private CustomerComplianceExtensionDto MapToDto(Entity entity)
    {
        return new CustomerComplianceExtensionDto
        {
            phr_complianceextensionid = entity.Id,
            phr_customeraccount = entity.GetAttributeValue<string>("phr_customeraccount"),
            phr_dataareaid = entity.GetAttributeValue<string>("phr_dataareaid"),
            phr_businesscategory = entity.GetAttributeValue<int>("phr_businesscategory"),
            phr_approvalstatus = entity.GetAttributeValue<int>("phr_approvalstatus"),
            phr_gdpqualificationstatus = entity.GetAttributeValue<int>("phr_gdpqualificationstatus"),
            phr_onboardingdate = entity.GetAttributeValue<DateTime?>("phr_onboardingdate"),
            phr_nextreverificationdate = entity.GetAttributeValue<DateTime?>("phr_nextreverificationdate"),
            phr_issuspended = entity.GetAttributeValue<bool>("phr_issuspended"),
            phr_suspensionreason = entity.GetAttributeValue<string>("phr_suspensionreason"),
            phr_createddate = entity.GetAttributeValue<DateTime>("phr_createddate"),
            phr_modifieddate = entity.GetAttributeValue<DateTime>("phr_modifieddate"),
            phr_rowversion = entity.GetAttributeValue<byte[]>("phr_rowversion")
        };
    }

    private Entity MapToEntity(CustomerComplianceExtensionDto dto)
    {
        var entity = new Entity(ExtensionEntityName) { Id = dto.phr_complianceextensionid };
        entity["phr_customeraccount"] = dto.phr_customeraccount;
        entity["phr_dataareaid"] = dto.phr_dataareaid;
        entity["phr_businesscategory"] = dto.phr_businesscategory;
        entity["phr_approvalstatus"] = dto.phr_approvalstatus;
        entity["phr_gdpqualificationstatus"] = dto.phr_gdpqualificationstatus;
        entity["phr_onboardingdate"] = dto.phr_onboardingdate;
        entity["phr_nextreverificationdate"] = dto.phr_nextreverificationdate;
        entity["phr_issuspended"] = dto.phr_issuspended;
        entity["phr_suspensionreason"] = dto.phr_suspensionreason;
        entity["phr_createddate"] = dto.phr_createddate;
        entity["phr_modifieddate"] = dto.phr_modifieddate;
        if (dto.phr_rowversion != null)
        {
            entity["phr_rowversion"] = dto.phr_rowversion;
        }
        return entity;
    }

    #endregion
}
