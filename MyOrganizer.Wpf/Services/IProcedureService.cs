using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Entities.Procedures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOrganizer.Wpf.Services
{
    public interface IProcedureService
    {
        Task<List<Procedure>> GetAllAsync(CancellationToken ct = default);
        Task<Procedure> AddAsync(string name, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
        Task<List<Procedure>> GetAllWithPricesAsync(CancellationToken ct = default);
        Task UpsertPricesAsync(IEnumerable<(int procedureId, decimal t1, decimal t2, decimal t3, string currency)> items,
                               CancellationToken ct = default);
    }

    public class ProcedureService : IProcedureService
    {
        private readonly AppDbContext _db;
        public ProcedureService(AppDbContext db) => _db = db;

        public Task<List<Procedure>> GetAllAsync(CancellationToken ct = default) =>
            _db.Procedures.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync(ct);

        public async Task<Procedure> AddAsync(string name, CancellationToken ct = default)
        {
            var p = new Procedure { Name = name.Trim(), IsActive = true };
            _db.Procedures.Add(p);
            await _db.SaveChangesAsync(ct);
            return p;
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var p = await _db.Procedures.FindAsync(new object?[] { id }, ct);
            if (p is null) return;
            _db.Procedures.Remove(p);
            await _db.SaveChangesAsync(ct);
        }

        public Task<List<Procedure>> GetAllWithPricesAsync(CancellationToken ct = default) =>
            _db.Procedures
               .Where(p => p.IsActive)
               .Include(p => p.Prices)
               .OrderBy(p => p.Name)
               .ToListAsync(ct);

        public async Task UpsertPricesAsync(IEnumerable<(int procedureId, decimal t1, decimal t2, decimal t3, string currency)> items,
                                            CancellationToken ct = default)
        {
            foreach (var item in items)
            {
                var price = await _db.ProcedurePrices
                    .FirstOrDefaultAsync(x => x.ProcedureId == item.procedureId, ct);

                if (price is null)
                {
                    _db.ProcedurePrices.Add(new ProcedurePrice
                    {
                        ProcedureId = item.procedureId,
                        Tier1 = item.t1,
                        Tier2 = item.t2,
                        Tier3 = item.t3,
                        Currency = item.currency
                    });
                }
                else
                {
                    price.Tier1 = item.t1;
                    price.Tier2 = item.t2;
                    price.Tier3 = item.t3;
                    price.Currency = item.currency;
                }
            }
            await _db.SaveChangesAsync(ct);
        }
    }

}
