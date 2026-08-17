using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Data.Entities;
using MyOrganizer.Wpf.Extensions;

namespace MyOrganizer.Wpf.MVVM.UI;

public partial class ClientsWindow : Window
{
    private const int DemoClientLimit = 10;
    private readonly AppDbContext _db;
    private ObservableCollection<Client> _items = [];

    public ClientsWindow(AppDbContext db)
    {
        InitializeComponent();
        _db = db;

        Loaded += async (_, _) =>
        {
            cmbFind.DisplayMemberPath = nameof(ClientSearchField.Label);
            cmbFind.SelectedValuePath = nameof(ClientSearchField.Property);
            cmbFind.ItemsSource = new ClientSearchField[]
            {
                new("FirstName", "FirstName".T()),
                new("LastName", "LastName".T()),
                new("MidlName", "MidlName".T()),
                new("Phone", "Phone".T())
            };
            cmbFind.SelectedIndex = 0;
            datemounth.SelectedDate = DateTime.Today;
            await LoadDataAsync();
        };
    }

    private async Task LoadDataAsync()
    {
        var data = await _db.Clients
            .AsNoTracking()
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .ToListAsync();

        _items = new ObservableCollection<Client>(data);
        dgvClients.ItemsSource = _items;
    }

    private Client? GetSelected() => dgvClients.SelectedItem as Client;

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var src = e.OriginalSource as DependencyObject;
        if (src is null)
            return;

        if (FindParent<ComboBox>(src) != null ||
            FindParent<TextBoxBase>(src) != null ||
            FindParent<PasswordBox>(src) != null ||
            FindParent<ButtonBase>(src) != null ||
            FindParent<ListBox>(src) != null ||
            FindParent<DataGrid>(src) != null ||
            FindParent<DatePicker>(src) != null)
        {
            return;
        }

        try { DragMove(); }
        catch { /* ignore invalid drag */ }
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T typed)
                return typed;
            child = System.Windows.Media.VisualTreeHelper.GetParent(child);
        }
        return null;
    }

    private void pictureBox2_Click(object sender, RoutedEventArgs e) => Close();

    private async void btnSave_Click(object? sender, RoutedEventArgs e)
    {
        if (!await CheckClientLimitAsync())
            return;

        var dlg = new EditClientWindow { Owner = this };
        if (dlg.ShowDialog() != true)
            return;

        await PersistClientAsync(dlg.Model);
        await LoadDataAsync();
        dgvClients.SelectedItem = _items.FirstOrDefault(c => c.Id == dlg.Model.Id);
    }

    private async Task PersistClientAsync(Client c)
    {
        if (c.Id == 0)
        {
            _db.Clients.Add(c);
        }
        else
        {
            var tracked = await _db.Clients.FirstOrDefaultAsync(x => x.Id == c.Id);
            if (tracked is null)
            {
                _db.Clients.Add(c);
            }
            else
            {
                tracked.FirstName = c.FirstName;
                tracked.LastName = c.LastName;
                tracked.MidlName = c.MidlName;
                tracked.PhoneNumber = c.PhoneNumber;
                tracked.Price = c.Price;
                tracked.Debet = c.Debet;
                tracked.DateJoin = c.DateJoin;
                tracked.DateDobleJoin = c.DateDobleJoin;
                tracked.DateJoinString = c.DateJoinString;
            }
        }

        await _db.SaveChangesAsync();
    }

    private async Task<bool> CheckClientLimitAsync()
    {
        var count = await _db.Clients.CountAsync();
        if (count >= DemoClientLimit)
        {
            ModernDialog.ShowWithLink(
                beforeText: $"Demo limit reached.\nYou can store up to {DemoClientLimit} clients.\n\nPlease ",
                linkText: "contact us by email",
                navigateUri: "mailto:myorganizer.dental@gmail.com?subject=Upgrade%20Request",
                afterText: " to unlock the full version.",
                caption: "Demo Limit",
                buttons: MessageBoxButton.OK,
                icon: MessageBoxImage.Information);
            return false;
        }

        if (count >= DemoClientLimit * 0.8)
        {
            ModernDialog.Show(
                "You’re nearing the demo limit (80%).\nUpgrade anytime to keep adding clients.",
                "Demo Warning",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        return true;
    }

    private async void btnEdit_Click(object? sender, RoutedEventArgs e)
    {
        var selected = GetSelected();
        if (selected is null)
        {
            ModernDialog.Show("SelectClient".T(), "Info");
            return;
        }

        var entity = await _db.Clients.AsNoTracking().FirstAsync(x => x.Id == selected.Id);
        var dlg = new EditClientWindow(entity) { Owner = this };
        if (dlg.ShowDialog() != true)
            return;

        await PersistClientAsync(dlg.Model);
        await LoadDataAsync();
    }

    private async void btrDelete_Click(object? sender, RoutedEventArgs e)
    {
        var selected = GetSelected();
        if (selected is null)
        {
            ModernDialog.Show("Selecttheclienttodelete".T(), "Info");
            return;
        }

        var confirm = ModernDialog.Show(
            "Deletelient.".T(),
            "Confirm".T(),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
            return;

        var entity = await _db.Clients.FirstAsync(x => x.Id == selected.Id);
        _db.Clients.Remove(entity);
        await _db.SaveChangesAsync();
        _items.Remove(selected);
    }

    private void btnExit_Click(object sender, RoutedEventArgs e) => Close();

    private async void criculButton1_Click(object sender, RoutedEventArgs e)
    {
        if (datemounth.SelectedDate is not DateTime d)
            return;

        var sum = await _db.Clients
            .Where(c => c.DateJoin.Year == d.Year && c.DateJoin.Month == d.Month)
            .SumAsync(c => c.Debet ?? 0m);

        txtSum.Text = sum.ToString("0.##");
    }

    private async void criculButton2_Click(object sender, RoutedEventArgs e)
    {
        if (datemounth.SelectedDate is not DateTime d)
            return;

        var sum = await _db.Clients
            .Where(c => c.DateJoin.Year == d.Year && c.DateJoin.Month == d.Month)
            .SumAsync(c => c.Price ?? 0m);

        txtSum.Text = sum.ToString("0.##");
    }

    private async void btnFind_Click(object sender, RoutedEventArgs e)
    {
        var prop = (cmbFind.SelectedItem as ClientSearchField)?.Property;
        var text = (txtFind.Text ?? string.Empty).Trim();
        DateTime? month = datemounth.SelectedDate;

        IQueryable<Client> q = _db.Clients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrEmpty(prop))
        {
            q = prop switch
            {
                "FirstName" => q.Where(c => c.FirstName != null && c.FirstName.Contains(text)),
                "LastName" => q.Where(c => c.LastName != null && c.LastName.Contains(text)),
                "MidlName" => q.Where(c => c.MidlName != null && c.MidlName.Contains(text)),
                "Phone" => q.Where(c => c.PhoneNumber != null && c.PhoneNumber.Contains(text)),
                _ => q
            };
        }

        if (month is DateTime m)
            q = q.Where(c => c.DateJoin.Year == m.Year && c.DateJoin.Month == m.Month);

        var list = await q
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .ToListAsync();

        _items = new ObservableCollection<Client>(list);
        dgvClients.ItemsSource = _items;
    }

    public sealed class ClientSearchField(string property, string label)
    {
        public string Property { get; } = property;
        public string Label { get; } = label;
        public override string ToString() => Label;
    }
}
