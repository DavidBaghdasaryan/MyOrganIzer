namespace MyOrganizer.Wpf.Entities;

public enum OfferingKind
{
    Product = 0,
    Service = 1,
    Other = 2
}

public class UnitOfMeasure
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int? BaseUnitId { get; set; }
    public decimal? ConversionFactor { get; set; }

    public UnitOfMeasure? BaseUnit { get; set; }
}

public class CatalogItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public OfferingKind Kind { get; set; }
    public int? UnitOfMeasureId { get; set; }
    public string Notes { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public UnitOfMeasure? UnitOfMeasure { get; set; }
    public List<SupplierOffering> SupplierOfferings { get; set; } = [];
}
