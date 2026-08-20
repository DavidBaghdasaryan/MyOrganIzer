using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Entities;

namespace MyOrganizer.Wpf.Services;

public interface ISupplierService
{
    Task<List<Supplier>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<Supplier?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Supplier> AddAsync(string name, string email, string phone, string notes, CancellationToken ct = default);
    Task UpdateAsync(int id, string name, string email, string phone, string notes, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
    Task<List<SupplierOffering>> GetAssociationsAsync(int supplierId, OfferingKind? kind = null, bool includeInactive = false, CancellationToken ct = default);
    Task<SupplierOffering> UpsertAssociationAsync(int supplierId, int catalogItemId, decimal price, CancellationToken ct = default);
    Task DeactivateAssociationAsync(int supplierId, int catalogItemId, CancellationToken ct = default);
}

public sealed class SupplierService : ISupplierService
{
    private readonly AppDbContext _db;

    public SupplierService(AppDbContext db) => _db = db;

    public Task<List<Supplier>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _db.Suppliers.AsNoTracking().AsQueryable();
        if (!includeInactive)
            query = query.Where(s => s.IsActive);
        return query.OrderBy(s => s.Name).ToListAsync(ct);
    }

    public Task<Supplier?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Suppliers
            .AsNoTracking()
            .Include(s => s.Offerings)
            .ThenInclude(o => o.CatalogItem)
            .ThenInclude(c => c.UnitOfMeasure)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Supplier> AddAsync(string name, string email, string phone, string notes, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var supplier = new Supplier
        {
            Name = name.Trim(),
            Email = email.Trim(),
            Phone = phone.Trim(),
            Notes = notes.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync(ct);
        return supplier;
    }

    public async Task UpdateAsync(int id, string name, string email, string phone, string notes, CancellationToken ct = default)
    {
        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (supplier is null)
            return;

        supplier.Name = name.Trim();
        supplier.Email = email.Trim();
        supplier.Phone = phone.Trim();
        supplier.Notes = notes.Trim();
        supplier.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (supplier is null)
            return;

        supplier.IsActive = false;
        supplier.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public Task<List<SupplierOffering>> GetAssociationsAsync(
        int supplierId,
        OfferingKind? kind = null,
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        var query = _db.SupplierOfferings
            .AsNoTracking()
            .Include(o => o.CatalogItem)
            .ThenInclude(c => c.UnitOfMeasure)
            .Where(o => o.SupplierId == supplierId);
        if (kind is { } value)
            query = query.Where(o => o.CatalogItem.Kind == value);
        if (!includeInactive)
            query = query.Where(o => o.IsActive && o.CatalogItem.IsActive);
        return query.OrderBy(o => o.CatalogItem.Name).ToListAsync(ct);
    }

    public async Task<SupplierOffering> UpsertAssociationAsync(
        int supplierId,
        int catalogItemId,
        decimal price,
        CancellationToken ct = default)
    {
        var existing = await _db.SupplierOfferings
            .FirstOrDefaultAsync(o => o.SupplierId == supplierId && o.CatalogItemId == catalogItemId, ct);
        if (existing is null)
        {
            existing = new SupplierOffering
            {
                SupplierId = supplierId,
                CatalogItemId = catalogItemId,
                Notes = ""
            };
            _db.SupplierOfferings.Add(existing);
        }

        existing.SupplierPrice = decimal.Round(price, 2);
        existing.IsActive = true;
        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task DeactivateAssociationAsync(int supplierId, int catalogItemId, CancellationToken ct = default)
    {
        var existing = await _db.SupplierOfferings
            .FirstOrDefaultAsync(o => o.SupplierId == supplierId && o.CatalogItemId == catalogItemId, ct);
        if (existing is null)
            return;
        existing.IsActive = false;
        await _db.SaveChangesAsync(ct);
    }
}
