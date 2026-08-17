using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Entities.Procedures;
using MyOrganizer.Wpf.MVVM.DTOs;

namespace MyOrganizer.Wpf.MVVM.UI;

public partial class SetPricesDialog : Window
{
    private readonly AppDbContext _db;
    public ObservableCollection<PriceRowDto> Items { get; } = [];

    public SetPricesDialog(AppDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var procs = await _db.Procedures
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Id)
            .ToListAsync();

        var latestPrices = await _db.LoadLatestPricesAsync();
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
        foreach (var row in Items)
        {
            if (row.Tier1 < 0 || row.Tier2 < 0 || row.Tier3 < 0)
            {
                MessageBox.Show("Tiers must be non-negative.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        var latestIds = await _db.ProcedurePrices
            .GroupBy(p => p.ProcedureId)
            .Select(g => g.Max(x => x.Id))
            .ToListAsync();

        var tracked = await _db.ProcedurePrices
            .Where(p => latestIds.Contains(p.Id))
            .ToListAsync();
        var map = tracked.ToDictionary(x => x.ProcedureId);

        foreach (var row in Items)
        {
            if (!map.TryGetValue(row.ProcedureId, out var price))
            {
                _db.ProcedurePrices.Add(new ProcedurePrice
                {
                    ProcedureId = row.ProcedureId,
                    Tier1 = row.Tier1,
                    Tier2 = row.Tier2,
                    Tier3 = row.Tier3,
                    Currency = row.Currency
                });
                continue;
            }

            if (price.Tier1 == row.Tier1 &&
                price.Tier2 == row.Tier2 &&
                price.Tier3 == row.Tier3 &&
                price.Currency == row.Currency)
                continue;

            price.Tier1 = row.Tier1;
            price.Tier2 = row.Tier2;
            price.Tier3 = row.Tier3;
            price.Currency = row.Currency;
        }

        await _db.SaveChangesAsync();
        DialogResult = true;
        Close();
    }

    private void Catalog_Click(object sender, RoutedEventArgs e)
    {
        var win = WindowFactory.Create<ProceduresCatalogWindow>();
        win.Owner = this;
        win.ShowDialog();
        _ = LoadAsync();
    }
}
