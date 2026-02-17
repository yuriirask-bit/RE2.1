using System.Collections.Concurrent;
using RE2.ComplianceCore.Interfaces;
using RE2.ComplianceCore.Models;

namespace RE2.DataAccess.InMemory;

/// <summary>
/// In-memory implementation of IGdpEquipmentRepository for local development and testing.
/// T257: Stores GDP equipment qualifications in ConcurrentDictionary.
/// </summary>
public class InMemoryGdpEquipmentRepository : IGdpEquipmentRepository
{
    private readonly ConcurrentDictionary<Guid, GdpEquipmentQualification> _equipment = new();

    public Task<IEnumerable<GdpEquipmentQualification>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<GdpEquipmentQualification>>(
            _equipment.Values.Select(Clone).ToList());
    }

    public Task<GdpEquipmentQualification?> GetByIdAsync(Guid equipmentQualificationId, CancellationToken cancellationToken = default)
    {
        _equipment.TryGetValue(equipmentQualificationId, out var equipment);
        return Task.FromResult(equipment != null ? Clone(equipment) : null);
    }

    public Task<IEnumerable<GdpEquipmentQualification>> GetByProviderAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        var results = _equipment.Values
            .Where(e => e.ProviderId == providerId)
            .Select(Clone)
            .ToList();
        return Task.FromResult<IEnumerable<GdpEquipmentQualification>>(results);
    }

    public Task<IEnumerable<GdpEquipmentQualification>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var results = _equipment.Values
            .Where(e => e.SiteId == siteId)
            .Select(Clone)
            .ToList();
        return Task.FromResult<IEnumerable<GdpEquipmentQualification>>(results);
    }

    public Task<IEnumerable<GdpEquipmentQualification>> GetDueForRequalificationAsync(int daysAhead = 30, CancellationToken cancellationToken = default)
    {
        var results = _equipment.Values
            .Where(e => e.IsDueForRequalification(daysAhead))
            .Select(Clone)
            .ToList();
        return Task.FromResult<IEnumerable<GdpEquipmentQualification>>(results);
    }

    public Task<Guid> CreateAsync(GdpEquipmentQualification equipment, CancellationToken cancellationToken = default)
    {
        if (equipment.EquipmentQualificationId == Guid.Empty)
        {
            equipment.EquipmentQualificationId = Guid.NewGuid();
        }

        equipment.CreatedDate = DateTime.UtcNow;
        equipment.ModifiedDate = DateTime.UtcNow;

        _equipment[equipment.EquipmentQualificationId] = Clone(equipment);
        return Task.FromResult(equipment.EquipmentQualificationId);
    }

    public Task UpdateAsync(GdpEquipmentQualification equipment, CancellationToken cancellationToken = default)
    {
        equipment.ModifiedDate = DateTime.UtcNow;
        _equipment[equipment.EquipmentQualificationId] = Clone(equipment);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid equipmentQualificationId, CancellationToken cancellationToken = default)
    {
        _equipment.TryRemove(equipmentQualificationId, out _);
        return Task.CompletedTask;
    }

    #region Seed Methods

    internal void SeedEquipment(IEnumerable<GdpEquipmentQualification> equipment)
    {
        foreach (var item in equipment)
        {
            _equipment.TryAdd(item.EquipmentQualificationId, item);
        }
    }

    #endregion

    #region Clone Helpers

    private static GdpEquipmentQualification Clone(GdpEquipmentQualification source) => new()
    {
        EquipmentQualificationId = source.EquipmentQualificationId,
        EquipmentName = source.EquipmentName,
        EquipmentType = source.EquipmentType,
        ProviderId = source.ProviderId,
        SiteId = source.SiteId,
        QualificationDate = source.QualificationDate,
        RequalificationDueDate = source.RequalificationDueDate,
        QualificationStatus = source.QualificationStatus,
        QualifiedBy = source.QualifiedBy,
        Notes = source.Notes,
        CreatedDate = source.CreatedDate,
        ModifiedDate = source.ModifiedDate
    };

    #endregion
}
