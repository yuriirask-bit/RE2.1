using RE2.ComplianceCore.Models;

namespace RE2.ComplianceCore.Interfaces;

/// <summary>
/// Repository interface for LicenceType entity operations.
/// T070: Repository interface for licence type CRUD operations.
/// </summary>
public interface ILicenceTypeRepository
{
    Task<LicenceType?> GetByIdAsync(Guid licenceTypeId, CancellationToken cancellationToken = default);
    Task<LicenceType?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<LicenceType>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<LicenceType>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(LicenceType licenceType, CancellationToken cancellationToken = default);
    Task UpdateAsync(LicenceType licenceType, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid licenceTypeId, CancellationToken cancellationToken = default);
}
