using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Entities;

namespace MyOrganizer.Wpf.Services;

public sealed class ExpenseDraft
{
    public int Id { get; set; }
    public int? SupplierId { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public string Reference { get; set; } = "";
    public string Notes { get; set; } = "";
    public List<ExpenseLineDraft> Lines { get; set; } = [];
}

public sealed class ExpenseLineDraft
{
    public int? CatalogItemId { get; set; }
    public OfferingKind Kind { get; set; }
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; } = 1;
    public int? UnitOfMeasureId { get; set; }
    public decimal UnitPrice { get; set; }
    public int? ClientId { get; set; }
    public string? ToothFdi { get; set; }
    public int? CatalogProcedureId { get; set; }
}

public interface IExpenseService
{
    Task<List<Expense>> GetAllAsync(int? supplierId = null, CancellationToken ct = default);
    Task<Expense?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Expense> SaveAsync(ExpenseDraft draft, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public sealed class ExpenseService : IExpenseService
{
    private readonly AppDbContext _db;

    public ExpenseService(AppDbContext db) => _db = db;

    public Task<List<Expense>> GetAllAsync(int? supplierId = null, CancellationToken ct = default)
    {
        var query = _db.Expenses
            .AsNoTracking()
            .Include(e => e.Supplier)
            .Include(e => e.Lines)
            .AsQueryable();
        if (supplierId is int id)
            query = query.Where(e => e.SupplierId == id);
        return query
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Id)
            .ToListAsync(ct);
    }

    public Task<Expense?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Expenses
            .AsNoTracking()
            .Include(e => e.Supplier)
            .Include(e => e.Lines)
            .ThenInclude(l => l.CatalogItem)
            .Include(e => e.Lines)
            .ThenInclude(l => l.UnitOfMeasure)
            .Include(e => e.Lines)
            .ThenInclude(l => l.Client)
            .Include(e => e.Lines)
            .ThenInclude(l => l.CatalogProcedure)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Expense> SaveAsync(ExpenseDraft draft, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (draft.Lines.Count == 0)
            throw new InvalidOperationException("At least one item is required.");

        var now = DateTime.UtcNow;
        Expense expense;
        if (draft.Id > 0)
        {
            expense = await _db.Expenses
                .Include(e => e.Lines)
                .FirstOrDefaultAsync(e => e.Id == draft.Id, ct)
                ?? throw new InvalidOperationException("Expense was not found.");
            _db.ExpenseLines.RemoveRange(expense.Lines);
            expense.Lines.Clear();
        }
        else
        {
            expense = new Expense { CreatedAt = now };
            _db.Expenses.Add(expense);
        }

        expense.SupplierId = draft.SupplierId is > 0 ? draft.SupplierId : null;
        expense.Date = draft.Date.Date;
        expense.Reference = (draft.Reference ?? "").Trim();
        expense.Notes = (draft.Notes ?? "").Trim();
        expense.UpdatedAt = now;

        decimal total = 0;
        foreach (var line in draft.Lines)
        {
            var description = (line.Description ?? "").Trim();
            if (description.Length == 0)
                throw new InvalidOperationException("Each item needs a description.");

            var quantity = line.Quantity <= 0 ? 1 : decimal.Round(line.Quantity, 3);
            var unitPrice = decimal.Round(line.UnitPrice, 2);
            var lineTotal = decimal.Round(quantity * unitPrice, 2);
            total += lineTotal;
            expense.Lines.Add(new ExpenseLine
            {
                CatalogItemId = line.CatalogItemId is > 0 ? line.CatalogItemId : null,
                Kind = line.Kind,
                Description = description,
                Quantity = quantity,
                UnitOfMeasureId = line.UnitOfMeasureId is > 0 ? line.UnitOfMeasureId : null,
                UnitPrice = unitPrice,
                LineTotal = lineTotal,
                ClientId = line.ClientId,
                ToothFdi = string.IsNullOrWhiteSpace(line.ToothFdi) ? null : line.ToothFdi.Trim(),
                CatalogProcedureId = line.CatalogProcedureId
            });
        }

        expense.TotalAmount = total;
        await _db.SaveChangesAsync(ct);
        return expense;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (expense is null)
            return;
        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync(ct);
    }
}
