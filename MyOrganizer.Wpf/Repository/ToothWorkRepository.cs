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

    public async Task AddAsync(int clientId, string toothFdi, string procedure, string tier, int price)
    {
        if (clientId <= 0)
            throw new InvalidOperationException("Client must be saved before adding tooth work.");

        _db.ToothWorks.Add(new ToothWork
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
        await _db.ToothWorks
            .Where(w => w.ClientId == clientId && w.ToothFdi == toothFdi)
            .ExecuteDeleteAsync();
    }
}
