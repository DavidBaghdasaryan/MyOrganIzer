using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data;

namespace MyOrganizer.Wpf.Services;

public class ReminderService : IReminderService
{
    private readonly AppDbContext _db;

    public ReminderService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ReminderItem>> LoadTodaysAsync()
    {
        var start = DateTime.Today;
        var end = start.AddDays(1);

        return await _db.Clients
            .AsNoTracking()
            .Where(c =>
                (c.DateJoin >= start && c.DateJoin < end) ||
                (c.DateDobleJoin != null && c.DateDobleJoin >= start && c.DateDobleJoin < end))
            .Select(c => new ReminderItem(
                c.Id,
                ((c.FirstName ?? "") + " " + (c.LastName ?? "")).Trim(),
                (c.DateJoin >= start && c.DateJoin < end) ? c.DateJoin : c.DateDobleJoin!.Value
            ))
            .ToListAsync();
    }
}
