using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Entities;

namespace MyOrganizer.Wpf.Services;

public static class LegacyTechnicsCopy
{
    public const string ReferencePrefix = "Technics:";

    public static int CopyIfNeeded(AppDbContext db)
    {
        var rows = db.Technics.AsNoTracking().OrderBy(t => t.Id).ToList();

        if (rows.Count == 0)
            return 0;

        var copiedKeys = db.Expenses.AsNoTracking()
            .Where(e => e.Reference.StartsWith(ReferencePrefix))
            .Select(e => e.Reference)
            .ToList()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var suppliers = db.Suppliers.Include(s => s.Offerings).ThenInclude(o => o.CatalogItem).ToList();
        var catalog = db.CatalogItems.Where(c => c.Kind == OfferingKind.Service).ToList();
        var now = DateTime.UtcNow;
        var copied = 0;

        foreach (var row in rows)
        {
            var key = ReferencePrefix + row.Id;
            if (copiedKeys.Contains(key))
                continue;

            var supplierName = string.IsNullOrWhiteSpace(row.Name) ? "Unknown" : row.Name.Trim();
            var supplier = suppliers.FirstOrDefault(s =>
                string.Equals(s.Name, supplierName, StringComparison.OrdinalIgnoreCase));
            if (supplier is null)
            {
                supplier = new Supplier
                {
                    Name = supplierName,
                    Email = "",
                    Phone = "",
                    Notes = "",
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Suppliers.Add(supplier);
                suppliers.Add(supplier);
            }

            var serviceName = string.IsNullOrWhiteSpace(row.Type) ? "General work" : row.Type.Trim();
            var item = catalog.FirstOrDefault(c =>
                string.Equals(c.Name, serviceName, StringComparison.OrdinalIgnoreCase));
            if (item is null)
            {
                item = new CatalogItem
                {
                    Name = serviceName,
                    Kind = OfferingKind.Service,
                    Notes = "",
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.CatalogItems.Add(item);
                catalog.Add(item);
            }

            var association = supplier.Offerings.FirstOrDefault(o =>
                o.CatalogItemId == item.Id ||
                (o.CatalogItem is not null &&
                 string.Equals(o.CatalogItem.Name, serviceName, StringComparison.OrdinalIgnoreCase)));
            if (association is null)
            {
                association = new SupplierOffering
                {
                    Supplier = supplier,
                    CatalogItem = item,
                    SupplierPrice = row.Price,
                    Notes = "",
                    IsActive = true
                };
                supplier.Offerings.Add(association);
                db.SupplierOfferings.Add(association);
            }
            else
            {
                association.IsActive = true;
                association.CatalogItem = item;
            }

            var amount = (decimal)row.Price;
            db.Expenses.Add(new Expense
            {
                Supplier = supplier,
                Date = row.Date == default ? DateTime.Today : row.Date.Date,
                Reference = key,
                Notes = "",
                TotalAmount = amount,
                CreatedAt = now,
                UpdatedAt = now,
                Lines =
                [
                    new ExpenseLine
                    {
                        CatalogItem = item,
                        Kind = OfferingKind.Service,
                        Description = serviceName,
                        Quantity = 1,
                        UnitPrice = amount,
                        LineTotal = amount
                    }
                ]
            });
            copiedKeys.Add(key);
            copied++;
        }

        if (copied > 0)
            db.SaveChanges();

        return copied;
    }
}
