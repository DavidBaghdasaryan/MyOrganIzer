using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Entities;

namespace MyOrganizer.Wpf.Repository;

public class ToothWorkRepository : IToothWorkRepository
{
    private readonly AppDbContext _db;
    public ToothWorkRepository(AppDbContext db) => _db = db;

    public Task<List<ToothWork>> GetByClientAsync(int clientId) =>
        _db.ToothWorks
           .AsNoTracking()
           .Where(x => x.ClientId == clientId)
           .ToListAsync();

    public async Task AddAsync(int clientId, string toothFdi, string procedure, string tier, int price, string? surface = null)
    {
        if (clientId <= 0)
            throw new InvalidOperationException("Client must be saved before adding tooth work.");

        _db.ToothWorks.Add(new ToothWork
        {
            ClientId = clientId,
            ToothFdi = toothFdi,
            ProcedureName = procedure,
            Tier = tier,
            Price = price,
            Surface = surface ?? ""
        });
        await _db.SaveChangesAsync();
    }

    public async Task ClearToothAsync(int clientId, string toothFdi)
    {
        await _db.ToothWorks
            .Where(w => w.ClientId == clientId && w.ToothFdi == toothFdi)
            .ExecuteDeleteAsync();
    }

    public async Task ClearSurfacesAsync(int clientId, string toothFdi, IEnumerable<string> surfaces)
    {
        var list = surfaces.ToList();
        if (list.Count == 0)
            return;

        await _db.ToothWorks
            .Where(w => w.ClientId == clientId && w.ToothFdi == toothFdi && list.Contains(w.Surface))
            .ExecuteDeleteAsync();
    }
}
