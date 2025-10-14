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
            dpDate.SelectedDate = DateTime.Today;
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            dgTechnics.ItemsSource = await _db.Tecnos.AsNoTracking().ToListAsync();
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

            var item = new Tecno
            {
                Type = cmbTechnics.SelectedItem.ToString(),
                Price = int.TryParse(txtPrice.Text, out var p) ? p : 0,
                Date = dpDate.SelectedDate ?? DateTime.Today,
                Name = txtTechnoName.Text
            };
            _db.Tecnos.Add(item);
            await _db.SaveChangesAsync();
            await LoadDataAsync();
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgTechnics.SelectedItem is not Tecno selected)
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

            _db.Tecnos.Update(selected);
            await _db.SaveChangesAsync();
            await LoadDataAsync();
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgTechnics.SelectedItem is not Tecno selected)
                return;

            _db.Tecnos.Remove(selected);
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

            var sum = await _db.Tecnos
                .Where(t => t.Type == type && t.Date.Month == month)
                .SumAsync(t => (int?)t.Price) ?? 0;

            txtSum.Text = sum.ToString();
        }
    }
}
