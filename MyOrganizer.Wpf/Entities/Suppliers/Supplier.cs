namespace MyOrganizer.Wpf.Entities;

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Notes { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<SupplierOffering> Offerings { get; set; } = [];
    public List<Expense> Expenses { get; set; } = [];
}

public class SupplierOffering
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public int CatalogItemId { get; set; }
    public decimal SupplierPrice { get; set; }
    public string? Sku { get; set; }
    public string Notes { get; set; } = "";
    public bool IsActive { get; set; } = true;

    public Supplier Supplier { get; set; } = null!;
    public CatalogItem CatalogItem { get; set; } = null!;
}
