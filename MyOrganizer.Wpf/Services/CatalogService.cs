using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Entities;

namespace MyOrganizer.Wpf.Services;

public sealed class CatalogSupplierLink
{
    public int SupplierId { get; set; }
    public decimal Price { get; set; }
}

public interface ICatalogService
{
    Task<List<CatalogItem>> GetAllAsync(OfferingKind? kind = null, bool includeInactive = false, CancellationToken ct = default);
    Task<CatalogItem?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CatalogItem> SaveAsync(
        int id,
        string name,
        OfferingKind kind,
        int? unitOfMeasureId,
        string notes,
        IReadOnlyList<CatalogSupplierLink> suppliers,
        CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);
}

public sealed class CatalogService : ICatalogService
{
    private readonly AppDbContext _db;

    public CatalogService(AppDbContext db) => _db = db;

    public Task<List<CatalogItem>> GetAllAsync(OfferingKind? kind = null, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _db.CatalogItems
            .AsNoTracking()
            .Include(c => c.UnitOfMeasure)
            .Include(c => c.SupplierOfferings)
            .ThenInclude(o => o.Supplier)
            .AsQueryable();
        if (kind is { } value)
            query = query.Where(c => c.Kind == value);
        if (!includeInactive)
            query = query.Where(c => c.IsActive);
        return query.OrderBy(c => c.Kind).ThenBy(c => c.Name).ToListAsync(ct);
    }

    public Task<CatalogItem?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.CatalogItems
            .AsNoTracking()
            .Include(c => c.UnitOfMeasure)
            .Include(c => c.SupplierOfferings)
            .ThenInclude(o => o.Supplier)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<CatalogItem> SaveAsync(
        int id,
        string name,
        OfferingKind kind,
        int? unitOfMeasureId,
        string notes,
        IReadOnlyList<CatalogSupplierLink> suppliers,
        CancellationToken ct = default)
    {
        if (kind is not OfferingKind.Product and not OfferingKind.Service)
            throw new InvalidOperationException("Catalog items must be a product or a service.");

        CatalogItem item;
        if (id > 0)
        {
            item = await _db.CatalogItems
                .Include(c => c.SupplierOfferings)
                .FirstOrDefaultAsync(c => c.Id == id, ct)
                ?? throw new InvalidOperationException("Catalog item was not found.");
        }
        else
        {
            item = new CatalogItem { CreatedAt = DateTime.UtcNow };
            _db.CatalogItems.Add(item);
        }

        item.Name = name.Trim();
        item.Kind = kind;
        item.UnitOfMeasureId = unitOfMeasureId;
        item.Notes = (notes ?? "").Trim();
        item.IsActive = true;
        item.UpdatedAt = DateTime.UtcNow;

        var selected = (suppliers ?? [])
            .Where(s => s.SupplierId > 0)
            .GroupBy(s => s.SupplierId)
            .Select(g => g.Last())
            .ToDictionary(s => s.SupplierId);

        foreach (var existing in item.SupplierOfferings)
        {
            if (selected.TryGetValue(existing.SupplierId, out var link))
            {
                existing.IsActive = true;
                existing.SupplierPrice = decimal.Round(link.Price, 2);
                selected.Remove(existing.SupplierId);
            }
            else
            {
                existing.IsActive = false;
            }
        }

        foreach (var link in selected.Values)
        {
            item.SupplierOfferings.Add(new SupplierOffering
            {
                SupplierId = link.SupplierId,
                CatalogItem = item,
                SupplierPrice = decimal.Round(link.Price, 2),
                Notes = "",
                IsActive = true
            });
        }

        await _db.SaveChangesAsync(ct);
        return item;
    }

    public async Task DeactivateAsync(int id, CancellationToken ct = default)
    {
        var item = await _db.CatalogItems.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (item is null)
            return;
        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
