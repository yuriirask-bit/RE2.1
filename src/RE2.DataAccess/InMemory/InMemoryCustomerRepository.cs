using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of ICustomerRepository for local development and testing.
/// Uses composite key pattern with separate storage for D365FO customers and compliance extensions.
/// </summary>
public class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly ConcurrentDictionary<string, Customer> _d365Customers = new();
    private readonly ConcurrentDictionary<string, Customer> _complianceExtensions = new();

    private static string GetKey(string customerAccount, string dataAreaId) => $"{customerAccount}|{dataAreaId}";

    #region D365FO Customer Queries

    public Task<IEnumerable<Customer>> GetAllD365CustomersAsync(CancellationToken cancellationToken = default)
    {
        var customers = _d365Customers.Values.Select(c =>
        {
            var result = CloneD365Customer(c);
            var key = GetKey(c.CustomerAccount, c.DataAreaId);
            if (_complianceExtensions.TryGetValue(key, out var extension))
            {
                MergeComplianceExtension(result, extension);
            }
            return result;
        }).ToList();

        return Task.FromResult<IEnumerable<Customer>>(customers);
    }

    public Task<Customer?> GetD365CustomerAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var key = GetKey(customerAccount, dataAreaId);
        if (!_d365Customers.TryGetValue(key, out var customer))
            return Task.FromResult<Customer?>(null);

        var result = CloneD365Customer(customer);
        if (_complianceExtensions.TryGetValue(key, out var extension))
        {
            MergeComplianceExtension(result, extension);
        }

        return Task.FromResult<Customer?>(result);
    }

    #endregion

    #region Composite Queries (D365FO + compliance extension merged)

    public Task<Customer?> GetByAccountAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var key = GetKey(customerAccount, dataAreaId);
        if (!_complianceExtensions.TryGetValue(key, out var extension))
            return Task.FromResult<Customer?>(null);

        var result = CloneComplianceExtension(extension);
        if (_d365Customers.TryGetValue(key, out var d365Customer))
        {
            MergeD365Data(result, d365Customer);
        }

        return Task.FromResult<Customer?>(result);
    }

    public Task<IEnumerable<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var customers = _complianceExtensions.Values.Select(ext =>
        {
            var result = CloneComplianceExtension(ext);
            var key = GetKey(ext.CustomerAccount, ext.DataAreaId);
            if (_d365Customers.TryGetValue(key, out var d365Customer))
            {
                MergeD365Data(result, d365Customer);
            }
            return result;
        }).ToList();

        return Task.FromResult<IEnumerable<Customer>>(customers);
    }

    public Task<IEnumerable<Customer>> GetByApprovalStatusAsync(ApprovalStatus status, CancellationToken cancellationToken = default)
    {
        var customers = _complianceExtensions.Values
            .Where(c => c.ApprovalStatus == status)
            .Select(ext =>
            {
                var result = CloneComplianceExtension(ext);
                var key = GetKey(ext.CustomerAccount, ext.DataAreaId);
                if (_d365Customers.TryGetValue(key, out var d365Customer))
                    MergeD365Data(result, d365Customer);
                return result;
            }).ToList();

        return Task.FromResult<IEnumerable<Customer>>(customers);
    }

    public Task<IEnumerable<Customer>> GetByBusinessCategoryAsync(BusinessCategory category, CancellationToken cancellationToken = default)
    {
        var customers = _complianceExtensions.Values
            .Where(c => c.BusinessCategory == category)
            .Select(ext =>
            {
                var result = CloneComplianceExtension(ext);
                var key = GetKey(ext.CustomerAccount, ext.DataAreaId);
                if (_d365Customers.TryGetValue(key, out var d365Customer))
                    MergeD365Data(result, d365Customer);
                return result;
            }).ToList();

        return Task.FromResult<IEnumerable<Customer>>(customers);
    }

    public Task<IEnumerable<Customer>> GetSuspendedAsync(CancellationToken cancellationToken = default)
    {
        var customers = _complianceExtensions.Values
            .Where(c => c.IsSuspended)
            .Select(ext =>
            {
                var result = CloneComplianceExtension(ext);
                var key = GetKey(ext.CustomerAccount, ext.DataAreaId);
                if (_d365Customers.TryGetValue(key, out var d365Customer))
                    MergeD365Data(result, d365Customer);
                return result;
            }).ToList();

        return Task.FromResult<IEnumerable<Customer>>(customers);
    }

    public Task<IEnumerable<Customer>> GetReVerificationDueAsync(int daysAhead, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var threshold = today.AddDays(daysAhead);

        var customers = _complianceExtensions.Values
            .Where(c => c.NextReVerificationDate.HasValue && c.NextReVerificationDate.Value <= threshold)
            .Select(ext =>
            {
                var result = CloneComplianceExtension(ext);
                var key = GetKey(ext.CustomerAccount, ext.DataAreaId);
                if (_d365Customers.TryGetValue(key, out var d365Customer))
                    MergeD365Data(result, d365Customer);
                return result;
            }).ToList();

        return Task.FromResult<IEnumerable<Customer>>(customers);
    }

    public Task<IEnumerable<Customer>> GetCanTransactAsync(CancellationToken cancellationToken = default)
    {
        var customers = _complianceExtensions.Values
            .Where(c => c.CanTransact())
            .Select(ext =>
            {
                var result = CloneComplianceExtension(ext);
                var key = GetKey(ext.CustomerAccount, ext.DataAreaId);
                if (_d365Customers.TryGetValue(key, out var d365Customer))
                    MergeD365Data(result, d365Customer);
                return result;
            }).ToList();

        return Task.FromResult<IEnumerable<Customer>>(customers);
    }

    #endregion

    #region Compliance Extension CRUD

    public Task<Guid> SaveComplianceExtensionAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        if (customer.ComplianceExtensionId == Guid.Empty)
            customer.ComplianceExtensionId = Guid.NewGuid();

        customer.CreatedDate = DateTime.UtcNow;
        customer.ModifiedDate = DateTime.UtcNow;

        var key = GetKey(customer.CustomerAccount, customer.DataAreaId);
        _complianceExtensions[key] = CloneComplianceExtension(customer);

        return Task.FromResult(customer.ComplianceExtensionId);
    }

    public Task UpdateComplianceExtensionAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        customer.ModifiedDate = DateTime.UtcNow;
        var key = GetKey(customer.CustomerAccount, customer.DataAreaId);
        _complianceExtensions[key] = CloneComplianceExtension(customer);

        return Task.CompletedTask;
    }

    public Task DeleteComplianceExtensionAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var key = GetKey(customerAccount, dataAreaId);
        _complianceExtensions.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string customerAccount, string dataAreaId, CancellationToken cancellationToken = default)
    {
        var key = GetKey(customerAccount, dataAreaId);
        return Task.FromResult(_d365Customers.ContainsKey(key) || _complianceExtensions.ContainsKey(key));
    }

    public Task<IEnumerable<Customer>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        // Search across both D365FO customers and compliance extensions
        var d365Matches = _d365Customers.Values
            .Where(c => c.OrganizationName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .Select(c =>
            {
                var result = CloneD365Customer(c);
                var key = GetKey(c.CustomerAccount, c.DataAreaId);
                if (_complianceExtensions.TryGetValue(key, out var extension))
                    MergeComplianceExtension(result, extension);
                return result;
            });

        return Task.FromResult<IEnumerable<Customer>>(d365Matches.ToList());
    }

    #endregion

    #region Seed Methods

    /// <summary>
    /// Seeds mock D365FO customer data for local development.
    /// </summary>
    internal void SeedD365Customers(IEnumerable<Customer> customers)
    {
        foreach (var customer in customers)
        {
            var key = GetKey(customer.CustomerAccount, customer.DataAreaId);
            _d365Customers.TryAdd(key, customer);
        }
    }

    /// <summary>
    /// Seeds compliance extension data for local development.
    /// </summary>
    internal void SeedComplianceExtensions(IEnumerable<Customer> extensions)
    {
        foreach (var extension in extensions)
        {
            var key = GetKey(extension.CustomerAccount, extension.DataAreaId);
            _complianceExtensions.TryAdd(key, extension);
        }
    }

    #endregion

    #region Private Helpers

    private static Customer CloneD365Customer(Customer source) => new()
    {
        CustomerAccount = source.CustomerAccount,
        DataAreaId = source.DataAreaId,
        OrganizationName = source.OrganizationName,
        AddressCountryRegionId = source.AddressCountryRegionId
    };

    private static Customer CloneComplianceExtension(Customer source) => new()
    {
        CustomerAccount = source.CustomerAccount,
        DataAreaId = source.DataAreaId,
        ComplianceExtensionId = source.ComplianceExtensionId,
        BusinessCategory = source.BusinessCategory,
        ApprovalStatus = source.ApprovalStatus,
        GdpQualificationStatus = source.GdpQualificationStatus,
        OnboardingDate = source.OnboardingDate,
        NextReVerificationDate = source.NextReVerificationDate,
        IsSuspended = source.IsSuspended,
        SuspensionReason = source.SuspensionReason,
        CreatedDate = source.CreatedDate,
        ModifiedDate = source.ModifiedDate,
        RowVersion = source.RowVersion
    };

    private static void MergeComplianceExtension(Customer target, Customer extension)
    {
        target.ComplianceExtensionId = extension.ComplianceExtensionId;
        target.BusinessCategory = extension.BusinessCategory;
        target.ApprovalStatus = extension.ApprovalStatus;
        target.GdpQualificationStatus = extension.GdpQualificationStatus;
        target.OnboardingDate = extension.OnboardingDate;
        target.NextReVerificationDate = extension.NextReVerificationDate;
        target.IsSuspended = extension.IsSuspended;
        target.SuspensionReason = extension.SuspensionReason;
        target.CreatedDate = extension.CreatedDate;
        target.ModifiedDate = extension.ModifiedDate;
        target.RowVersion = extension.RowVersion;
    }

    private static void MergeD365Data(Customer target, Customer d365Source)
    {
        target.OrganizationName = d365Source.OrganizationName;
        target.AddressCountryRegionId = d365Source.AddressCountryRegionId;
    }

    #endregion
}
