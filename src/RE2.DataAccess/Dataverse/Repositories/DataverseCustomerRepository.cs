using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;
using RE2.DataAccess.Dataverse.Models;

namespace RE2.DataAccess.Dataverse.Repositories;

/// <summary>
/// Dataverse implementation of ICustomerRepository.
/// T089: Repository implementation for Customer per data-model.md entity 5.
/// </summary>
public class DataverseCustomerRepository : ICustomerRepository
{
    private readonly IDataverseClient _client;
    private readonly ILogger<DataverseCustomerRepository> _logger;
    private const string EntityName = "phr_customer";

    public DataverseCustomerRepository(IDataverseClient client, ILogger<DataverseCustomerRepository> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _client.RetrieveAsync(EntityName, customerId, new ColumnSet(true), cancellationToken);
            return MapToDto(entity).ToDomainModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer {Id}", customerId);
            return null;
        }
    }

    public async Task<Customer?> GetByBusinessNameAsync(string businessName, CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EntityName)
        {
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("phr_businessname", ConditionOperator.Equal, businessName)
                }
            }
        };

        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        return result.Entities.FirstOrDefault() != null ? MapToDto(result.Entities.First()).ToDomainModel() : null;
    }

    public async Task<Customer?> GetByRegistrationNumberAsync(string registrationNumber, CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EntityName)
        {
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("phr_registrationnumber", ConditionOperator.Equal, registrationNumber)
                }
            }
        };

        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        return result.Entities.FirstOrDefault() != null ? MapToDto(result.Entities.First()).ToDomainModel() : null;
    }

    public async Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EntityName) { ColumnSet = new ColumnSet(true) };
        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
    }

    public async Task<IEnumerable<Customer>> GetByApprovalStatusAsync(ApprovalStatus status, CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EntityName)
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
        return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
    }

    public async Task<IEnumerable<Customer>> GetByBusinessCategoryAsync(BusinessCategory category, CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EntityName)
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
        return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
    }

    public async Task<IEnumerable<Customer>> GetByCountryAsync(string country, CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EntityName)
        {
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("phr_country", ConditionOperator.Equal, country)
                }
            }
        };

        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
    }

    public async Task<IEnumerable<Customer>> GetSuspendedAsync(CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EntityName)
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
        return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
    }

    public async Task<IEnumerable<Customer>> GetReVerificationDueAsync(int daysAhead, CancellationToken cancellationToken = default)
    {
        var dueDate = DateTime.UtcNow.AddDays(daysAhead);

        var query = new QueryExpression(EntityName)
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
        return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
    }

    public async Task<IEnumerable<Customer>> GetCanTransactAsync(CancellationToken cancellationToken = default)
    {
        // Per data-model.md: ApprovalStatus must be Approved or ConditionallyApproved,
        // and IsSuspended = false
        var query = new QueryExpression(EntityName)
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
        return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
    }

    public async Task<Guid> CreateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var dto = CustomerDto.FromDomainModel(customer);
        var entity = MapToEntity(dto);
        return await _client.CreateAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var dto = CustomerDto.FromDomainModel(customer);
        var entity = MapToEntity(dto);
        await _client.UpdateAsync(entity, cancellationToken);
    }

    public async Task DeleteAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        await _client.DeleteAsync(EntityName, customerId, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _client.RetrieveAsync(EntityName, customerId, new ColumnSet("phr_customerid"), cancellationToken);
            return entity != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<Customer>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var query = new QueryExpression(EntityName)
        {
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("phr_businessname", ConditionOperator.Contains, searchTerm)
                }
            }
        };

        var result = await _client.RetrieveMultipleAsync(query, cancellationToken);
        return result.Entities.Select(e => MapToDto(e).ToDomainModel()).ToList();
    }

    private CustomerDto MapToDto(Entity entity)
    {
        return new CustomerDto
        {
            phr_customerid = entity.Id,
            phr_businessname = entity.GetAttributeValue<string>("phr_businessname"),
            phr_registrationnumber = entity.GetAttributeValue<string>("phr_registrationnumber"),
            phr_businesscategory = entity.GetAttributeValue<int>("phr_businesscategory"),
            phr_country = entity.GetAttributeValue<string>("phr_country"),
            phr_approvalstatus = entity.GetAttributeValue<int>("phr_approvalstatus"),
            phr_onboardingdate = entity.GetAttributeValue<DateTime?>("phr_onboardingdate"),
            phr_nextreverificationdate = entity.GetAttributeValue<DateTime?>("phr_nextreverificationdate"),
            phr_gdpqualificationstatus = entity.GetAttributeValue<int>("phr_gdpqualificationstatus"),
            phr_issuspended = entity.GetAttributeValue<bool>("phr_issuspended"),
            phr_suspensionreason = entity.GetAttributeValue<string>("phr_suspensionreason"),
            phr_createddate = entity.GetAttributeValue<DateTime>("phr_createddate"),
            phr_modifieddate = entity.GetAttributeValue<DateTime>("phr_modifieddate"),
            phr_rowversion = entity.GetAttributeValue<byte[]>("phr_rowversion")
        };
    }

    private Entity MapToEntity(CustomerDto dto)
    {
        var entity = new Entity(EntityName) { Id = dto.phr_customerid };
        entity["phr_businessname"] = dto.phr_businessname;
        entity["phr_registrationnumber"] = dto.phr_registrationnumber;
        entity["phr_businesscategory"] = dto.phr_businesscategory;
        entity["phr_country"] = dto.phr_country;
        entity["phr_approvalstatus"] = dto.phr_approvalstatus;
        entity["phr_onboardingdate"] = dto.phr_onboardingdate;
        entity["phr_nextreverificationdate"] = dto.phr_nextreverificationdate;
        entity["phr_gdpqualificationstatus"] = dto.phr_gdpqualificationstatus;
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
}
