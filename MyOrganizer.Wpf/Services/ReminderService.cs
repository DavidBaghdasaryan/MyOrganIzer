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
        // WinForms code queried “Client.SalectDatduoblejoin()” and then “Answers”.
        // We’ll use Clients.DateDobleJoin as the appointment time and Client’s names.
        // Adjust if you add an Answers table later.
        var today = DateTime.Today;

        var data = await _db.Clients
            .Where(c => c.DateDobleJoin.Date == today)
            .Select(c => new ReminderItem(
                c.Id,
                ((c.FirstName ?? "") + " " + (c.LastName ?? "")).Trim(),
                c.DateDobleJoin
            ))
            .ToListAsync();

        return data;
    }
}
