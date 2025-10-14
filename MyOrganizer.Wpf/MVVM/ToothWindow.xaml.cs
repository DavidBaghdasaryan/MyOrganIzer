using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MyOrganizer.Wpf.Repository;
using MyOrganizer.Wpf.Entities;
using MyOrganizer.Wpf.Data.Entities;

namespace MyOrganizer.Wpf.MVVM
{
    public partial class ToothWindow :Window
    {
        private readonly IToothWorkRepository _repo;
        public   Client Client;
        private  string _clientFullName=string.Empty;

        public struct ToothAction
        {
            public string Fdi;          // e.g. "11" (the tooth number)
            public string Procedure;    // e.g. "Byugel"
            public string Tier;         // e.g. "A", "B", or "C"
            public int Price;           // e.g. 230000
        }

        // Pass client details via ctor
        public ToothWindow( IToothWorkRepository repo)
        {
           
            _repo = repo;

            InitializeComponent();
            
        }
       

        private static readonly string[] Procedures =
        {
            "Byugel", "Protez", "Impl/Zr", "Impl/Mk",
            "Zr/K,Emax", "Mk30", "Rest", "Plomb",
            "Shift", "Endo"
        };

        private static readonly Dictionary<string, string> _procShort = new()
        {
            ["Byugel"] = "BY",
            ["Protez"] = "PR",
            ["Impl/Zr"] = "IZ",
            ["Impl/Mk"] = "IM",
            ["Zr/K,Emax"] = "ZR",
            ["Mk30"] = "MK",
            ["Rest"] = "RS",
            ["Plomb"] = "PL",
            ["Shift"] = "SH",
            ["Endo"] = "EN"
        };

        private static readonly Dictionary<string, Brush> _procBrush = new()
        {
            ["Byugel"] = new SolidColorBrush(Color.FromRgb(0x39, 0x8E, 0xB5)),
            ["Protez"] = new SolidColorBrush(Color.FromRgb(0x6A, 0x1B, 0x9A)),
            ["Impl/Zr"] = new SolidColorBrush(Color.FromRgb(0x00, 0x8B, 0x8B)),
            ["Impl/Mk"] = new SolidColorBrush(Color.FromRgb(0x00, 0x64, 0x95)),
            ["Zr/K,Emax"] = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)),
            ["Mk30"] = new SolidColorBrush(Color.FromRgb(0xF9, 0xA8, 0x25)),
            ["Rest"] = new SolidColorBrush(Color.FromRgb(0xEF, 0x6C, 0x00)),
            ["Plomb"] = new SolidColorBrush(Color.FromRgb(0xD8, 0x3F, 0x31)),
            ["Shift"] = new SolidColorBrush(Color.FromRgb(0x45, 0x55, 0x57)),
            ["Endo"] = new SolidColorBrush(Color.FromRgb(0x15, 0x75, 0x9A)),
        };

        // remember per-tooth selections (loaded from DB, updated live)
        private readonly Dictionary<string, HashSet<string>> _toothProcedures = new(StringComparer.OrdinalIgnoreCase);

        // (optional) simple default price tiers
        private readonly Dictionary<string, int[]> _priceTable = new()
        {
            ["Byugel"] = new[] { 250000, 230000, 200000 },
            ["Protez"] = new[] { 70000, 65000, 60000 },
            ["Impl/Zr"] = new[] { 90000, 85000, 80000 },
            ["Impl/Mk"] = new[] { 70000, 65000, 60000 },
            ["Zr/K,Emax"] = new[] { 80000, 78000, 75000 },
            ["Mk30"] = new[] { 35000, 30000, 25000 },
            ["Rest"] = new[] { 20000, 18000, 15000 },
            ["Plomb"] = new[] { 15000, 13000, 10000 },
            ["Shift"] = new[] { 5000, 4000, 3000 },
            ["Endo"] = new[] { 7000, 6000, 5000 }
        };

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _clientFullName = string.Concat(Client.FirstName, " ", Client.LastName);
            TxtClientName.Text = _clientFullName;
            if (Client.Id <= 0) return;
            await LoadExistingBadgesAsync(Client.Id);
        }

        // ====== clicks & context menu ======
        private void Tooth_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            ToggleHighlight(btn);
            var fdi = (btn.ToolTip ?? "").ToString();
            Title = $"Tooth Chart — Selected {fdi}";
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
                var a = (ToothAction)mi.Tag;   // ✅ was TouchAction
                await _repo.AddAsync(Client.Id, a.Fdi, a.Procedure, a.Tier, a.Price);

                if (!_toothProcedures.TryGetValue(a.Fdi, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    _toothProcedures[a.Fdi] = set;
                }
                set.Add(a.Procedure);

                RefreshBadges(btn, set);
                Tint(btn, Colors.LightSkyBlue);
            };

            return mi;
        }

        // ====== badges ======

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
                    Code = _procShort.TryGetValue(p, out var sc) ? sc :
                           p.Substring(0, Math.Min(2, p.Length)).ToUpperInvariant(),
                    Brush = _procBrush.TryGetValue(p, out var br) ? br : Brushes.SlateGray
                })
                .ToList();

            badges.ItemsSource = items;
        }

        private async Task LoadExistingBadgesAsync(int clientId)
        {
            _toothProcedures.Clear();

            var works = await _repo.GetByClientAsync(clientId); // ToothFdi + Procedure

            foreach (var g in works.GroupBy(w => w.ToothFdi))
            {
                var set = g.Select(x => x.ProcedureName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                _toothProcedures[g.Key] = set;

                if (FindToothButton(g.Key) is Button b)
                    RefreshBadges(b, set);
            }
        }

        // ====== visuals/tint helpers ======
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
                var grid = VisualTreeHelper.GetChild(b, 0) as Grid;
                if (grid != null && grid.Children.OfType<Shape>().FirstOrDefault() is Shape s)
                    s.Fill = new SolidColorBrush(Color.FromArgb(60, c.R, c.G, c.B));
            }
        }

        private Button? FindToothButton(string fdi)
        {
            // Search all Buttons whose ToolTip == FDI
            return this.GetVisualDescendants()
                .OfType<Button>()
                .FirstOrDefault(b => (b.ToolTip ?? "").ToString() == fdi);
        }

        // small helper to traverse visual tree
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
