using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data;                 // AppDbContext
using MyOrganizer.Wpf.Data.Entities;        // your data entities if needed
using MyOrganizer.Wpf.Entities;             // Client, etc.
using MyOrganizer.Wpf.MVVM;                 // namespace of this file (keep consistent)
using MyOrganizer.Wpf.Repository;           // IToothWorkRepository

namespace MyOrganizer.Wpf.MVVM
{
    public partial class ToothWindow : Window
    {
        private readonly IToothWorkRepository _repo;
        private readonly AppDbContext _db;

        public Client Client;
        private string _clientFullName = string.Empty;

        // In-memory selections per tooth: FDI -> set of procedure names
        private readonly Dictionary<string, HashSet<string>> _toothProcedures =
            new(StringComparer.OrdinalIgnoreCase);

        // Price table (procedure name -> [A,B,C] tier prices)
        private Dictionary<string, int[]> _priceTable =
            new(StringComparer.Ordinal);

        public struct ToothAction
        {
            public string Fdi;        // e.g. "11"
            public string Procedure;  // full name (e.g., "Metal-Ceramic Crown")
            public string Tier;       // "A" | "B" | "C"
            public int Price;         // AMD (int for UI)
        }

        // Inject repo + db
        public ToothWindow(IToothWorkRepository repo, AppDbContext db)
        {
            _repo = repo;
            _db = db;
            InitializeComponent();
        }

        // ===== Static dictionaries: procedures, short codes, and colors =====

        private static readonly string[] Procedures =
        {
            "Removable Partial Denture (Metal Framework)", // BY
            "Full Denture",                                // PR
            "Implant with Zirconia Crown",                 // IZ
            "Implant with Metal-Ceramic Crown",            // IM
            "Zirconia or E-max Crown",                     // ZR
            "Metal-Ceramic Crown",                         // MK
            "Composite or Inlay Restoration",              // RS
            "Filling (Composite / Amalgam)",               // PL
            "Work Shift / Appointment Slot",               // SH
            "Endodontic Treatment (Root Canal)"            // EN
        };

        private static readonly Dictionary<string, string> _procShort =
            new(StringComparer.Ordinal)
            {
                ["Removable Partial Denture (Metal Framework)"] = "BY",
                ["Full Denture"] = "PR",
                ["Implant with Zirconia Crown"] = "IZ",
                ["Implant with Metal-Ceramic Crown"] = "IM",
                ["Zirconia or E-max Crown"] = "ZR",
                ["Metal-Ceramic Crown"] = "MK",
                ["Composite or Inlay Restoration"] = "RS",
                ["Filling (Composite / Amalgam)"] = "PL",
                ["Work Shift / Appointment Slot"] = "SH",
                ["Endodontic Treatment (Root Canal)"] = "EN"
            };

        private static readonly Dictionary<string, Brush> _procBrush =
            new(StringComparer.Ordinal)
            {
                ["Removable Partial Denture (Metal Framework)"] = new SolidColorBrush(Color.FromRgb(0x39, 0x8E, 0xB5)), // BY
                ["Full Denture"] = new SolidColorBrush(Color.FromRgb(0x6A, 0x1B, 0x9A)), // PR
                ["Implant with Zirconia Crown"] = new SolidColorBrush(Color.FromRgb(0x00, 0x8B, 0x8B)), // IZ
                ["Implant with Metal-Ceramic Crown"] = new SolidColorBrush(Color.FromRgb(0x00, 0x64, 0x95)), // IM
                ["Zirconia or E-max Crown"] = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)), // ZR
                ["Metal-Ceramic Crown"] = new SolidColorBrush(Color.FromRgb(0xF9, 0xA8, 0x25)), // MK
                ["Composite or Inlay Restoration"] = new SolidColorBrush(Color.FromRgb(0xEF, 0x6C, 0x00)), // RS
                ["Filling (Composite / Amalgam)"] = new SolidColorBrush(Color.FromRgb(0xD8, 0x3F, 0x31)), // PL
                ["Work Shift / Appointment Slot"] = new SolidColorBrush(Color.FromRgb(0x45, 0x55, 0x57)), // SH
                ["Endodontic Treatment (Root Canal)"] = new SolidColorBrush(Color.FromRgb(0x15, 0x75, 0x9A)), // EN
            };

        // ===== Window lifecycle =====

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // show client name
            _clientFullName = string.Concat(Client?.FirstName ?? "", " ", Client?.LastName ?? "").Trim();
            if (this.FindName("TxtClientName") is TextBlock tb)
                tb.Text = _clientFullName;

            // Load prices from DB (with fallback defaults)
            await LoadPriceTableAsync();

            if (Client?.Id > 0)
                await LoadExistingBadgesAsync(Client.Id);
        }

        // ===== DB-backed price loader with fallback defaults =====

        private async Task LoadPriceTableAsync()
        {
            // latest price per procedure by Id (simple, no extra columns needed)
            var latest = await _db.ProcedurePrices
                                  .AsNoTracking()
                                  .GroupBy(pp => pp.ProcedureId)
                                  .Select(g => g.OrderByDescending(pp => pp.Id).First())
                                  .ToListAsync();

            // active procedures → map Id -> Name
            var procNames = await _db.Procedures
                                     .AsNoTracking()
                                     .Where(p => p.IsActive)
                                     .Select(p => new { p.Id, p.Name })
                                     .ToListAsync();
            var nameById = procNames.ToDictionary(x => x.Id, x => x.Name);

            _priceTable.Clear();

            foreach (var row in latest)
            {
                if (!nameById.TryGetValue(row.ProcedureId, out var name)) continue;
                _priceTable[name] = new[]
                {
                    (int)Math.Round(row.Tier1, MidpointRounding.AwayFromZero),
                    (int)Math.Round(row.Tier2, MidpointRounding.AwayFromZero),
                    (int)Math.Round(row.Tier3, MidpointRounding.AwayFromZero),
                };
            }

            // ensure defaults exist for all menu items (if no DB rows yet)
            void EnsureDefault(string key, int a, int b, int c)
            {
                if (!_priceTable.ContainsKey(key))
                    _priceTable[key] = new[] { a, b, c };
            }

            EnsureDefault("Removable Partial Denture (Metal Framework)", 250000, 230000, 200000);
            EnsureDefault("Full Denture", 70000, 65000, 60000);
            EnsureDefault("Implant with Zirconia Crown", 90000, 85000, 80000);
            EnsureDefault("Implant with Metal-Ceramic Crown", 70000, 65000, 60000);
            EnsureDefault("Zirconia or E-max Crown", 80000, 78000, 75000);
            EnsureDefault("Metal-Ceramic Crown", 35000, 30000, 25000);
            EnsureDefault("Composite or Inlay Restoration", 20000, 18000, 15000);
            EnsureDefault("Filling (Composite / Amalgam)", 15000, 13000, 10000);
            EnsureDefault("Work Shift / Appointment Slot", 5000, 4000, 3000);
            EnsureDefault("Endodontic Treatment (Root Canal)", 7000, 6000, 5000);
        }

        // ===== Clicks & context menu =====

        private void Tooth_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            ToggleHighlight(btn);
            var fdi = (btn.ToolTip ?? "").ToString();
            this.Title = $"Tooth Chart — Selected {fdi}";
        }

        private void Tooth_ContextMenu(object sender, ContextMenuEventArgs e)
        {
            var btn = (Button)sender;
            var cm = new ContextMenu();

            foreach (var proc in Procedures)
            {
                var mi = new MenuItem { Header = proc };

                var prices = _priceTable.TryGetValue(proc, out var pr) ? pr : new[] { 0, 0, 0 };
                mi.Items.Add(BuildTierItem(btn, proc, "A", prices.ElementAtOrDefault(0)));
                mi.Items.Add(BuildTierItem(btn, proc, "B", prices.ElementAtOrDefault(1)));
                mi.Items.Add(BuildTierItem(btn, proc, "C", prices.ElementAtOrDefault(2)));

                cm.Items.Add(mi);
            }

            cm.Items.Add(new Separator());
            var clear = new MenuItem { Header = "Clear tooth" };
            clear.Click += async (_, __) =>
            {
                var fdi = (btn.ToolTip ?? "").ToString();
                await _repo.ClearToothAsync(Client.Id, fdi);
                _toothProcedures.Remove(fdi);
                RefreshBadges(btn, Array.Empty<string>());
                btn.Effect = null;
            };
            cm.Items.Add(clear);

            btn.ContextMenu = cm;
        }

        private MenuItem BuildTierItem(Button btn, string proc, string tier, int price)
        {
            var fdi = (btn.ToolTip ?? "").ToString();
            var mi = new MenuItem
            {
                Header = $"{tier} — {price.ToString("N0", CultureInfo.InvariantCulture)} ֏",
                Tag = new ToothAction { Fdi = fdi, Procedure = proc, Tier = tier, Price = price }
            };

            mi.Click += async (_, __) =>
            {
                var a = (ToothAction)mi.Tag;

                // Persist selection
                await _repo.AddAsync(Client.Id, a.Fdi, a.Procedure, a.Tier, a.Price);

                // Update in-memory model
                if (!_toothProcedures.TryGetValue(a.Fdi, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    _toothProcedures[a.Fdi] = set;
                }
                set.Add(a.Procedure);

                // Refresh visuals
                RefreshBadges(btn, set);
                Tint(btn, Colors.LightSkyBlue);
            };

            return mi;
        }

        // ===== Badges =====

        private sealed class BadgeVM
        {
            public string Code { get; init; } = "";
            public Brush Brush { get; init; } = Brushes.SlateGray;
        }

        private void RefreshBadges(Button toothBtn, IEnumerable<string> procedures)
        {
            toothBtn.ApplyTemplate();
            if (toothBtn.Template.FindName("PART_Badges", toothBtn) is not ItemsControl badges)
                return;

            var items = procedures
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(p => new BadgeVM
                {
                    Code = _procShort.TryGetValue(p, out var sc)
                        ? sc
                        : p.Substring(0, Math.Min(2, p.Length)).ToUpperInvariant(),
                    Brush = _procBrush.TryGetValue(p, out var br) ? br : Brushes.SlateGray
                })
                .ToList();

            badges.ItemsSource = items;
        }

        private async Task LoadExistingBadgesAsync(int clientId)
        {
            _toothProcedures.Clear();

            var works = await _repo.GetByClientAsync(clientId); // should return ToothFdi + ProcedureName

            foreach (var g in works.GroupBy(w => w.ToothFdi))
            {
                var set = g.Select(x => x.ProcedureName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                _toothProcedures[g.Key] = set;

                if (FindToothButton(g.Key) is Button b)
                    RefreshBadges(b, set);
            }
        }

        // ===== Visual helpers =====

        private static void ToggleHighlight(Button b)
        {
            if (b.Effect == null)
            {
                b.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.DeepSkyBlue,
                    ShadowDepth = 0,
                    BlurRadius = 12,
                    Opacity = 0.9
                };
            }
            else
            {
                b.Effect = null;
            }
        }

        private static void Tint(Button b, Color c)
        {
            if (VisualTreeHelper.GetChildrenCount(b) > 0)
            {
                if (VisualTreeHelper.GetChild(b, 0) is Grid grid)
                {
                    var shape = grid.Children.OfType<Shape>().FirstOrDefault();
                    if (shape != null)
                        shape.Fill = new SolidColorBrush(Color.FromArgb(60, c.R, c.G, c.B)); // 60 alpha
                }
            }
        }

        private Button? FindToothButton(string fdi)
        {
            // Find any Button whose ToolTip equals the FDI code
            return this.GetVisualDescendants()
                .OfType<Button>()
                .FirstOrDefault(b => string.Equals((b.ToolTip ?? "").ToString(), fdi, StringComparison.Ordinal));
        }
    }

    internal static class VisualTreeExtensions
    {
        public static IEnumerable<DependencyObject> GetVisualDescendants(this DependencyObject root)
        {
            if (root == null) yield break;
            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var d = queue.Dequeue();
                var count = VisualTreeHelper.GetChildrenCount(d);
                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(d, i);
                    yield return child;
                    queue.Enqueue(child);
                }
            }
        }
    }
}
