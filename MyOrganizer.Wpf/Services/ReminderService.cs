// Services/ReminderService.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data;

namespace MyOrganizer.Wpf.Services;

public class ReminderService : IReminderService
{
    private readonly AppDbContext _db;

    public ReminderService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ReminderItem>> LoadTodaysAsync()
    {
        var today = DateTime.Today;

        var data = await _db.Clients
            .Where(c => c.DateJoin.Date == today)
            .Select(c => new ReminderItem(
                c.Id,
                ((c.FirstName ?? "") + " " + (c.LastName ?? "")).Trim(),
                c.DateJoin
            ))
            .ToListAsync();

        return data;
    }
}
