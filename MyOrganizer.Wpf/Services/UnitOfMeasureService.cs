using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Entities;

namespace MyOrganizer.Wpf.Services;

public interface IUnitOfMeasureService
{
    Task<List<UnitOfMeasure>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<UnitOfMeasure> SaveAsync(int id, string name, int? baseUnitId, decimal? conversionFactor, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
}

public sealed class UnitOfMeasureService : IUnitOfMeasureService
{
    private readonly AppDbContext _db;

    public UnitOfMeasureService(AppDbContext db) => _db = db;

    public Task<List<UnitOfMeasure>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _db.UnitsOfMeasure.AsNoTracking().Include(u => u.BaseUnit).AsQueryable();
        if (!includeInactive)
            query = query.Where(u => u.IsActive);
        return query.OrderBy(u => u.Name).ToListAsync(ct);
    }

    public async Task<UnitOfMeasure> SaveAsync(int id, string name, int? baseUnitId, decimal? conversionFactor, CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
            throw new InvalidOperationException("Unit name is required.");
        if (baseUnitId is int baseId && baseId == id && id > 0)
            baseUnitId = null;

        UnitOfMeasure unit;
        if (id > 0)
        {
            unit = await _db.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Id == id, ct)
                ?? throw new InvalidOperationException("Unit was not found.");
        }
        else
        {
            unit = new UnitOfMeasure();
            _db.UnitsOfMeasure.Add(unit);
        }

        unit.Name = trimmed;
        unit.BaseUnitId = baseUnitId is > 0 ? baseUnitId : null;
        unit.ConversionFactor = unit.BaseUnitId is null ? null : conversionFactor;
        unit.IsActive = true;
        await _db.SaveChangesAsync(ct);
        return unit;
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        var unit = await _db.UnitsOfMeasure.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (unit is null)
            return;
        unit.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }
}
