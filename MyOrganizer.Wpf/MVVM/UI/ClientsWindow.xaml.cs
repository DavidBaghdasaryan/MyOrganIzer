using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Data.Entities;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM.Helpers;

namespace MyOrganizer.Wpf.MVVM.UI
{
    public partial class ClientsWindow : Window
    {
        private readonly AppDbContext _db;
        private ObservableCollection<Client> _items = new();

        public ClientsWindow(AppDbContext db)
        {
            InitializeComponent();
            _db = db;

            Loaded += async (_, __) =>
            {
                // Fill search fields and date
                cmbFind.ItemsSource = new[]
                {
                    "FirstName".T(),
                    "LastName".T(),
                    "MidlName".T(),
                    "Phone".T()
                };

                cmbFind.PreviewMouseLeftButtonDown += (s, e) =>
                {
                    // If the drop-down is closed and we clicked on the header area, open it
                    if (!cmbFind.IsDropDownOpen)
                    {
                        e.Handled = true;           // stop bubbling (prevents Window drag or other handlers)
                        cmbFind.Focus();            // ensure it has focus
                        cmbFind.IsDropDownOpen = true;
                    }
                };

                //cmbFind.PreviewKeyDown += (s, e) =>
                //{
                //    if (!cmbFind.IsDropDownOpen && (e.Key == Key.Down || e.Key == Key.Up || e.Key == Key.F4))
                //    {
                //        e.Handled = true;
                //        cmbFind.IsDropDownOpen = true;
                //    }
                //};

                cmbFind.SelectedIndex = 0;
                datemounth.SelectedDate = DateTime.Today;

                await LoadDataAsync();
            };
        }

        private async Task LoadDataAsync()
        {
            var data = await _db.Clients
                .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
                .AsNoTracking()
                .ToListAsync();

            _items = new ObservableCollection<Client>(data);
            dgvClients.ItemsSource = _items;
        }

        private Client? GetSelected() => dgvClients.SelectedItem as Client;

        // Dragging (WindowStyle=None)

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var src = e.OriginalSource as DependencyObject;
            if (src == null) return;

            // If click is inside any interactive input, don't start DragMove
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

            // Otherwise allow moving window
            try { DragMove(); } catch { /* ignore */ }
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T typed) return typed;
                child = System.Windows.Media.VisualTreeHelper.GetParent(child);
            }
            return null;
        }


        // Close (top-right X)
        private void pictureBox2_Click(object sender, RoutedEventArgs e) => Close();

        // ADD
        private async void btnSave_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = new EditClientWindow();
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                var c = dlg.Model;
                _db.Clients.Add(c);
                await _db.SaveChangesAsync();

                _items.Add(c);
                dgvClients.SelectedItem = c;
            }
        }

        // EDIT
        private async void btnEdit_Click(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelected();
            if (selected is null)
            {
                ModernDialog.Show("SelectClient".T(), "Info");
                return;
            }

            var entity = await _db.Clients.FirstAsync(x => x.Id == selected.Id);

            var dlg = new EditClientWindow(entity) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                entity.FirstName = dlg.Model.FirstName;
                entity.LastName = dlg.Model.LastName;
                entity.MidlName = dlg.Model.MidlName;
                entity.PhoneNumber = dlg.Model.PhoneNumber;
                entity.Price = dlg.Model.Price;
                entity.Debet = dlg.Model.Debet;
                entity.DateJoin = dlg.Model.DateJoin;
                entity.DateDobleJoin = dlg.Model.DateDobleJoin;
                entity.DateJoinString = dlg.Model.DateJoinString;

                await _db.SaveChangesAsync();
                await LoadDataAsync();
            }
        }

        // DELETE
        private async void btrDelete_Click(object? sender, RoutedEventArgs e)
        {
            var selected = GetSelected();
            if (selected is null)
            {
                ModernDialog.Show("Selecttheclienttodelete".T(), "Info");
                return;
            }

            var confirm = ModernDialog.Show("Deletelient.".T(),
                "Confirm".T(), MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            var entity = await _db.Clients.FirstAsync(x => x.Id == selected.Id);
            _db.Clients.Remove(entity);
            await _db.SaveChangesAsync();

            _items.Remove(selected);
        }

        // EXIT button (right panel)
        private void btnExit_Click(object sender, RoutedEventArgs e) => Close();

        // MONTH DEBT SUM
        private async void criculButton1_Click(object sender, RoutedEventArgs e)
        {
            if (datemounth.SelectedDate is not DateTime d) return;

            var sum = await _db.Clients
                .Where(c => c.DateDobleJoin.Year == d.Year && c.DateDobleJoin.Month == d.Month)
                .SumAsync(c => c.Debet ?? 0m);

            txtSum.Text = sum.ToString("0.##");
        }

        // MONTH INCOME SUM
        private async void criculButton2_Click(object sender, RoutedEventArgs e)
        {
            if (datemounth.SelectedDate is not DateTime d) return;

            var sum = await _db.Clients
                .Where(c => c.DateDobleJoin.Year == d.Year && c.DateDobleJoin.Month == d.Month)
                .SumAsync(c => c.Price ?? 0m);

            txtSum.Text = sum.ToString("0.##");
        }

        // SEARCH
        private async void btnFind_Click(object sender, RoutedEventArgs e)
        {
            var key = (cmbFind.SelectedItem as string) ?? "";
            var text = (txtFind.Text ?? string.Empty).Trim();
            DateTime? month = datemounth.SelectedDate;

            IQueryable<Client> q = _db.Clients;

            // Map Armenian labels to entity properties
            string col = key switch
            {
                "Անուն" => "FirstName".T(),
                "Ազգանուն" => "LastName".T(),
                "Հայրանուն" => "MidlName".T(),
                "Հեռախոսահամար" => "Phone".T(),
                _ => ""
            };  
                
            if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrEmpty(col))
            {
                q = col switch
                {
                    "FirstName" => q.Where(c => c.FirstName!.Contains(text)),
                    "LastName" => q.Where(c => c.LastName!.Contains(text)),
                    "MidlName" => q.Where(c => c.MidlName!.Contains(text)),
                    "Phone" => q.Where(c => c.PhoneNumber!.Contains(text)),
                    _ => q
                };
            }

            // Month filter (like MONTH(DateJoin) in WinForms; we use DateDobleJoin)
            if (month is DateTime m)
                q = q.Where(c => c.DateDobleJoin.Year == m.Year && c.DateDobleJoin.Month == m.Month);

            // Toggles
            if (chPrice.IsChecked == true)
                q = q.Where(c => c.Price != null && c.Price != 0);

            if (chbDebt.IsChecked == true)
                q = q.Where(c => c.Debet != null && c.Debet != 0);

            var list = await q
                .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
                .AsNoTracking()
                .ToListAsync();

            _items = new ObservableCollection<Client>(list);
            dgvClients.ItemsSource = _items;
        }

        // Keep toggles mutually exclusive
        private void chPrice_Checked(object sender, RoutedEventArgs e)
        {
            if (chPrice.IsChecked == true) chbDebt.IsChecked = false;
        }

        private void chbDebt_Checked(object sender, RoutedEventArgs e)
        {
            if (chbDebt.IsChecked == true) chPrice.IsChecked = false;
        }
    }
}
