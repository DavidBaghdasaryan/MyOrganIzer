using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Entities;
using MyOrganizer.Wpf.Extensions;

namespace MyOrganizer.Wpf.MVVM.UI;

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
        cmbTechnics.ItemsSource = await _db.Procedures
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Id)
            .Select(x => x.Name)
            .ToListAsync();

        dpDate.SelectedDate = DateTime.Today;
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        dgTechnics.ItemsSource = await _db.Technics.AsNoTracking().ToListAsync();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    private async void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        if (cmbTechnics.SelectedItem is null)
        {
            ModernDialog.Show("Materialnotspecified".T());
            return;
        }

        _db.Technics.Add(new Technic
        {
            Type = cmbTechnics.SelectedItem.ToString() ?? "",
            Price = int.TryParse(txtPrice.Text, out var p) ? p : 0,
            Date = dpDate.SelectedDate ?? DateTime.Today,
            Name = txtTechnoName.Text
        });
        await _db.SaveChangesAsync();
        await LoadDataAsync();
    }

    private async void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgTechnics.SelectedItem is not Technic selected)
            return;

        if (cmbTechnics.SelectedItem is null)
        {
            ModernDialog.Show("Materialnotspecified".T());
            return;
        }

        var entity = await _db.Technics.FirstOrDefaultAsync(x => x.Id == selected.Id);
        if (entity is null)
            return;

        entity.Type = cmbTechnics.SelectedItem.ToString() ?? "";
        entity.Price = int.TryParse(txtPrice.Text, out var p) ? p : 0;
        entity.Date = dpDate.SelectedDate ?? DateTime.Today;
        entity.Name = txtTechnoName.Text;

        await _db.SaveChangesAsync();
        await LoadDataAsync();
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgTechnics.SelectedItem is not Technic selected)
            return;

        var entity = await _db.Technics.FirstOrDefaultAsync(x => x.Id == selected.Id);
        if (entity is null)
            return;

        _db.Technics.Remove(entity);
        await _db.SaveChangesAsync();
        await LoadDataAsync();
    }

    private async void BtnSum_Click(object sender, RoutedEventArgs e)
    {
        if (cmbTechnics.SelectedItem is null)
        {
            ModernDialog.Show("Materialnotspecified".T());
            return;
        }

        var type = cmbTechnics.SelectedItem.ToString();
        var date = dpDate.SelectedDate ?? DateTime.Today;

        var sum = await _db.Technics
            .Where(t => t.Type == type && t.Date.Year == date.Year && t.Date.Month == date.Month)
            .SumAsync(t => (int?)t.Price) ?? 0;

        txtSum.Text = sum.ToString();
    }
}
