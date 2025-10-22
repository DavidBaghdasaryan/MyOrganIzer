using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Entities;

namespace MyOrganizer.Wpf.MVVM
{
    public partial class TechnicsWindow : Window
    {
        private readonly AppDbContext _db;
        public TechnicsWindow(AppDbContext db)
        {
            InitializeComponent();
            _db = db;
            Loaded += TechnicsWindow_Loaded;
        }

        private async void TechnicsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            cmbTechnics.ItemsSource = new[]
            {
                "Ի/Մ/Կ", "Մ/կ", "Ց/կ", "Բյուգել", "Պրոթեզ", "M & S abatments"
            };
            cmbTechnics.PreviewMouseLeftButtonDown += (s, e) =>
            {
                // If the drop-down is closed and we clicked on the header area, open it
                if (!cmbTechnics.IsDropDownOpen)
                {
                    e.Handled = true;           // stop bubbling (prevents Window drag or other handlers)
                    cmbTechnics.Focus();            // ensure it has focus
                    cmbTechnics.IsDropDownOpen = true;
                }
            };
            dpDate.SelectedDate = DateTime.Today;
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            dgTechnics.ItemsSource = await _db.Technics.AsNoTracking().ToListAsync();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (cmbTechnics.SelectedItem == null)
            {
                MessageBox.Show("Նյութական միջոցի տեսակը նշված չէ");
                return;
            }

            var item = new Technic
            {
                Type = cmbTechnics.SelectedItem.ToString(),
                Price = int.TryParse(txtPrice.Text, out var p) ? p : 0,
                Date = dpDate.SelectedDate ?? DateTime.Today,
                Name = txtTechnoName.Text
            };
            _db.Technics.Add(item);
            await _db.SaveChangesAsync();
            await LoadDataAsync();
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgTechnics.SelectedItem is not Technic selected)
                return;

            if (cmbTechnics.SelectedItem == null)
            {
                MessageBox.Show("Նյութական միջոցի տեսակը նշված չէ");
                return;
            }

            selected.Type = cmbTechnics.SelectedItem.ToString();
            selected.Price = int.TryParse(txtPrice.Text, out var p) ? p : 0;
            selected.Date = dpDate.SelectedDate ?? DateTime.Today;
            selected.Name = txtTechnoName.Text;

            _db.Technics.Update(selected);
            await _db.SaveChangesAsync();
            await LoadDataAsync();
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgTechnics.SelectedItem is not Technic selected)
                return;

            _db.Technics.Remove(selected);
            await _db.SaveChangesAsync();
            await LoadDataAsync();
        }

        private async void BtnSum_Click(object sender, RoutedEventArgs e)
        {
            if (cmbTechnics.SelectedItem == null)
            {
                MessageBox.Show("Նյութական միջոցի տեսակը ընտրված չէ");
                return;
            }

            var type = cmbTechnics.SelectedItem.ToString();
            var month = dpDate.SelectedDate?.Month ?? DateTime.Today.Month;

            var sum = await _db.Technics
                .Where(t => t.Type == type && t.Date.Month == month)
                .SumAsync(t => (int?)t.Price) ?? 0;

            txtSum.Text = sum.ToString();
        }
    }
}
