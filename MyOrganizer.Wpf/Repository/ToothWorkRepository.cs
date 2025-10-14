using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MyOrganizer.Wpf.Repository;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Entities;

public class ToothWorkRepository : IToothWorkRepository
{
    private readonly AppDbContext _db;
    public ToothWorkRepository(AppDbContext db) => _db = db;

    public Task<List<ToothWork>> GetByClientAsync(int clientId) =>
        _db.Set<ToothWork>()
           .AsNoTracking()
           .Where(x => x.ClientId == clientId)
           .ToListAsync();

    public async Task AddAsync(int clientId, string toothFdi, string procedure, string tier, int price)
    {
        _db.Add(new ToothWork
        {
            ClientId = clientId,
            ToothFdi = toothFdi,
            ProcedureName = procedure,
            Tier = tier,
            Price = price
        });
        await _db.SaveChangesAsync();
    }

    public async Task ClearToothAsync(int clientId, string toothFdi)
    {
        var rows = await _db.Set<ToothWork>()
                            .Where(w => w.ClientId == clientId && w.ToothFdi == toothFdi)
                            .ToListAsync();
        _db.RemoveRange(rows);
        await _db.SaveChangesAsync();
    }
}
