using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for Licence entity operations.
/// T068: Repository interface for licence CRUD operations.
/// </summary>
public interface ILicenceRepository
{
    Task<Licence?> GetByIdAsync(Guid licenceId, CancellationToken cancellationToken = default);
    Task<Licence?> GetByLicenceNumberAsync(string licenceNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Licence>> GetByHolderAsync(Guid holderId, string holderType, CancellationToken cancellationToken = default);
    Task<IEnumerable<Licence>> GetExpiringLicencesAsync(int daysAhead, CancellationToken cancellationToken = default);
    Task<IEnumerable<Licence>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Licence licence, CancellationToken cancellationToken = default);
    Task UpdateAsync(Licence licence, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid licenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all valid licences that cover a specific substance.
    /// T080g: Used for reclassification customer impact analysis per FR-066.
    /// </summary>
    Task<IEnumerable<Licence>> GetBySubstanceIdAsync(Guid substanceId, CancellationToken cancellationToken = default);
}
