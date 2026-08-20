using MyOrganizer.Wpf.Data.Entities;
using MyOrganizer.Wpf.Entities.Procedures;

namespace MyOrganizer.Wpf.Entities;

public class Expense
{
    public int Id { get; set; }
    public int? SupplierId { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public string Reference { get; set; } = "";
    public string Notes { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Supplier? Supplier { get; set; }
    public List<ExpenseLine> Lines { get; set; } = [];
}

public class ExpenseLine
{
    public int Id { get; set; }
    public int ExpenseId { get; set; }
    public int? CatalogItemId { get; set; }
    public OfferingKind Kind { get; set; }
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; } = 1;
    public int? UnitOfMeasureId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public int? ClientId { get; set; }
    public string? ToothFdi { get; set; }
    public int? CatalogProcedureId { get; set; }

    public Expense Expense { get; set; } = null!;
    public CatalogItem? CatalogItem { get; set; }
    public UnitOfMeasure? UnitOfMeasure { get; set; }
    public Client? Client { get; set; }
    public Procedure? CatalogProcedure { get; set; }
}
