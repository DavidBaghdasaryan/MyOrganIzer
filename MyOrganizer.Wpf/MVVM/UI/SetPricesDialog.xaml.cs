using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Entities.Procedures;
using MyOrganizer.Wpf.MVVM.DTOs;

namespace MyOrganizer.Wpf.MVVM.UI
{
    public partial class SetPricesDialog : Window
    {
        private readonly AppDbContext _db;
        public ObservableCollection<PriceRowDto> Items { get; } = new();

        public SetPricesDialog(AppDbContext db)
        {
            InitializeComponent();
            _db = db;
            Loaded += async (_, __) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            // Get all active procedures
            var procs = await _db.Procedures
                                 .AsNoTracking()
                                 .Where(p => p.IsActive)
                                 .OrderBy(p => p.Id)
                                 .ToListAsync();

            // For each proc, pick the "current" price = latest by Id (no schema changes needed)
            // If you later add CreatedAtUtc/IsCurrent, switch the ordering to that instead.
            var latestPrices = await _db.ProcedurePrices
                                        .AsNoTracking()
                                        .GroupBy(pp => pp.ProcedureId)
                                        .Select(g => g.OrderByDescending(pp => pp.Id).First())
                                        .ToListAsync();

            var map = latestPrices.ToDictionary(x => x.ProcedureId);

            Items.Clear();
            foreach (var p in procs)
            {
                map.TryGetValue(p.Id, out var price);
                Items.Add(new PriceRowDto
                {
                    ProcedureId = p.Id,
                    Name = p.Name,
                    Tier1 = price?.Tier1 ?? 0m,
                    Tier2 = price?.Tier2 ?? 0m,
                    Tier3 = price?.Tier3 ?? 0m,
                    Currency = price?.Currency ?? "AMD"
                });
            }

            dg.ItemsSource = Items;
        }

        private async void Ok_Click(object sender, RoutedEventArgs e)
        {
            // Basic validation (optional)
            foreach (var row in Items)
            {
                if (row.Tier1 < 0 || row.Tier2 < 0 || row.Tier3 < 0)
                {
                    MessageBox.Show("Tiers must be non-negative.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Append NEW price rows to preserve history (no updates to old rows)
            foreach (var row in Items)
            {
                _db.ProcedurePrices.Add(new ProcedurePrice
                {
                    ProcedureId = row.ProcedureId,
                    Tier1 = row.Tier1,
                    Tier2 = row.Tier2,
                    Tier3 = row.Tier3,
                    Currency = row.Currency
                });
            }

            await _db.SaveChangesAsync();
            DialogResult = true;
            Close();
        }
    }
}
