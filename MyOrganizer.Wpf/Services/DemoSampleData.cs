using System.IO;
using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Data.Entities;
using MyOrganizer.Wpf.Entities;

namespace MyOrganizer.Wpf.Services;

public static class DemoSampleData
{
    public const string Marker = "[demo-seed]";

    public static void Ensure(AppDbContext db)
    {
        var now = DateTime.UtcNow;
        var today = DateTime.Today;
        var pieceId = db.UnitsOfMeasure.AsNoTracking()
            .Where(u => u.Name == "Piece")
            .Select(u => (int?)u.Id)
            .FirstOrDefault();
        var hourId = db.UnitsOfMeasure.AsNoTracking()
            .Where(u => u.Name == "Hour")
            .Select(u => (int?)u.Id)
            .FirstOrDefault();

        var products = EnsureCatalog(db, OfferingKind.Product, pieceId, now,
        [
            "Composite A2 syringe",
            "Alginate impression",
            "Nitrile gloves",
            "Articaine cartridge",
            "Temporary crown kit"
        ]);
        var services = EnsureCatalog(db, OfferingKind.Service, hourId, now,
        [
            "Zirconia crown mill",
            "Denture repair",
            "Night guard",
            "Whitening trays",
            "Study model"
        ]);

        var suppliers = new (string Name, string Phone, string Email)[]
        {
            ("Yerevan Dental Lab", "+374 10 111111", "lab@yerevandental.test"),
            ("MedSupply Plus", "+374 10 222222", "sales@medsupply.test"),
            ("Apex Implants", "+374 10 333333", "apex@implants.test"),
            ("Smile Ceramics", "+374 10 444444", "hello@smileceramics.test"),
            ("Nord Depot", "+374 10 555555", "nord@depot.test")
        };

        var supplierIds = new List<int>();
        foreach (var row in suppliers)
        {
            var supplier = db.Suppliers.FirstOrDefault(s => s.Name == row.Name);
            if (supplier is null)
            {
                supplier = new Supplier
                {
                    Name = row.Name,
                    Phone = row.Phone,
                    Email = row.Email,
                    Notes = Marker,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Suppliers.Add(supplier);
                db.SaveChanges();
            }
            supplierIds.Add(supplier.Id);
        }

        var catalogIds = products.Concat(services).ToArray();
        var existingLinks = db.SupplierOfferings
            .AsNoTracking()
            .Where(o => supplierIds.Contains(o.SupplierId))
            .Select(o => new { o.SupplierId, o.CatalogItemId })
            .ToList()
            .Select(o => (o.SupplierId, o.CatalogItemId))
            .ToHashSet();

        for (var i = 0; i < supplierIds.Count; i++)
        {
            var supplierId = supplierIds[i];
            var bump = (i + 1) * 500m;
            foreach (var itemId in catalogIds)
            {
                if (!existingLinks.Add((supplierId, itemId)))
                    continue;
                db.SupplierOfferings.Add(new SupplierOffering
                {
                    SupplierId = supplierId,
                    CatalogItemId = itemId,
                    SupplierPrice = 4000m + bump,
                    Notes = Marker,
                    IsActive = true
                });
            }
        }

        var clients = new (string First, string Last, string Mid, string Phone, decimal Price, decimal Debet, DateTime? Visit)[]
        {
            ("Ani", "Hakobyan", "Kareni", "+374 91 100001", 85000, 15000, today.AddHours(10)),
            ("Davit", "Sargsyan", "Armeni", "+374 91 100002", 120000, 0, today.AddHours(11).AddMinutes(30)),
            ("Lilit", "Grigoryan", "Sureni", "+374 91 100003", 45000, 12000, today.AddHours(14)),
            ("Narek", "Avetisyan", "Vahani", "+374 91 100004", 60000, 60000, null),
            ("Mariam", "Karapetyan", "Tigrani", "+374 91 100005", 210000, 35000, today.AddHours(16).AddMinutes(15))
        };

        var addedClients = 0;
        for (var i = 0; i < clients.Length; i++)
        {
            var row = clients[i];
            if (db.Clients.Any(c => c.PhoneNumber == row.Phone))
                continue;
            db.Clients.Add(new Client
            {
                FirstName = row.First,
                LastName = row.Last,
                MidlName = row.Mid,
                PhoneNumber = row.Phone,
                Price = row.Price,
                Debet = row.Debet,
                DateJoin = today.AddDays(-(i + 2)),
                DateDobleJoin = row.Visit
            });
            addedClients++;
        }

        if (db.ChangeTracker.HasChanges())
            db.SaveChanges();

        // #region agent log
        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                sessionId = "ee2893",
                runId = "post-fix",
                hypothesisId = "D",
                location = "DemoSampleData.Ensure",
                message = "demo sample seed",
                data = new
                {
                    clients = db.Clients.Count(),
                    suppliers = db.Suppliers.Count(),
                    catalog = db.CatalogItems.Count(),
                    offerings = db.SupplierOfferings.Count(),
                    addedClients
                },
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            File.AppendAllText(@"c:\Users\david\source\repos\MyOrganIzer\debug-ee2893.log", payload + Environment.NewLine);
        }
        catch { }
        // #endregion
    }

    private static List<int> EnsureCatalog(
        AppDbContext db,
        OfferingKind kind,
        int? unitId,
        DateTime now,
        string[] names)
    {
        var ids = new List<int>();
        foreach (var name in names)
        {
            var item = db.CatalogItems.FirstOrDefault(c => c.Name == name);
            if (item is null)
            {
                item = new CatalogItem
                {
                    Name = name,
                    Kind = kind,
                    UnitOfMeasureId = unitId,
                    Notes = Marker,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.CatalogItems.Add(item);
                db.SaveChanges();
            }
            ids.Add(item.Id);
        }
        return ids;
    }
}
