using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for ControlledSubstance entity operations.
/// T072: Repository interface for controlled substance CRUD operations.
/// </summary>
public interface IControlledSubstanceRepository
{
    Task<ControlledSubstance?> GetByIdAsync(Guid substanceId, CancellationToken cancellationToken = default);
    Task<ControlledSubstance?> GetByInternalCodeAsync(string internalCode, CancellationToken cancellationToken = default);
    Task<IEnumerable<ControlledSubstance>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ControlledSubstance>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(ControlledSubstance substance, CancellationToken cancellationToken = default);
    Task UpdateAsync(ControlledSubstance substance, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid substanceId, CancellationToken cancellationToken = default);
}
