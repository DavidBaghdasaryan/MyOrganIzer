using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyOrganizer.Wpf;
using MyOrganizer.Wpf.Data;
using MyOrganizer.Wpf.Data.Entities;
using MyOrganizer.Wpf.Extensions;

namespace MyOrganizer.Wpf.MVVM.UI;

public partial class EditClientWindow : Window
{
    public Client Model { get; private set; }

    public EditClientWindow()
    {
        InitializeComponent();
        Model = new Client
        {
            DateJoin = DateTime.Now,
            DateDobleJoin = null
        };
        dpDateJoin.SelectedDate = Model.DateJoin;
    }

    public EditClientWindow(Client client) : this()
    {
        Model = new Client
        {
            Id = client.Id,
            FirstName = client.FirstName,
            LastName = client.LastName,
            MidlName = client.MidlName,
            PhoneNumber = client.PhoneNumber,
            Price = client.Price,
            Debet = client.Debet,
            DateJoin = client.DateJoin,
            DateDobleJoin = client.DateDobleJoin,
            DateJoinString = client.DateJoinString
        };

        txtName.Text = Model.FirstName;
        txtLastName.Text = Model.LastName;
        txtMidlName.Text = Model.MidlName;
        txtPhoneNumber.Text = Model.PhoneNumber;
        txtPrice.Text = Model.Price?.ToString(CultureInfo.InvariantCulture) ?? "0";
        txtDebt.Text = Model.Debet?.ToString(CultureInfo.InvariantCulture) ?? "0";
        dpDateJoin.SelectedDate = Model.DateJoin == default ? DateTime.Now : Model.DateJoin;

        if (Model.DateDobleJoin is { } doubleVisit && doubleVisit > DateTime.MinValue)
        {
            chbDouble.IsChecked = true;
            dpDateDouble.Visibility = Visibility.Visible;
            dpDateDouble.SelectedDate = doubleVisit;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    private bool ValidateClient()
    {
        if (string.IsNullOrWhiteSpace(txtName.Text) ||
            string.IsNullOrWhiteSpace(txtLastName.Text) ||
            string.IsNullOrWhiteSpace(txtMidlName.Text))
        {
            ModernDialog.Show("Required".T(), "Error".T(), MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        return true;
    }

    private void MapUiToModel()
    {
        Model.FirstName = txtName.Text?.Trim();
        Model.LastName = txtLastName.Text?.Trim();
        Model.MidlName = txtMidlName.Text?.Trim();
        Model.PhoneNumber = txtPhoneNumber.Text?.Trim();

        Model.Price = decimal.TryParse(txtPrice.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var price)
            ? price
            : 0;
        Model.Debet = decimal.TryParse(txtDebt.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var debt)
            ? debt
            : 0;

        Model.DateJoin = dpDateJoin.SelectedDate ?? DateTime.Now;

        if (chbDouble.IsChecked == true && dpDateDouble.SelectedDate.HasValue)
        {
            Model.DateDobleJoin = dpDateDouble.SelectedDate.Value;
            Model.DateJoinString = Model.DateDobleJoin.Value.ToString("dd-MM-yyyy HH:mm:ss");
        }
        else
        {
            Model.DateDobleJoin = null;
            Model.DateJoinString = null;
        }
    }

    private void btnSave1_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateClient())
            return;
        MapUiToModel();
        DialogResult = true;
        Close();
    }

    private void btrDelete_Click(object sender, RoutedEventArgs e)
    {
        txtName.Clear();
        txtLastName.Clear();
        txtMidlName.Clear();
        txtPrice.Text = "0";
        txtDebt.Text = "0";
        txtPhoneNumber.Text = "0";
        dpDateJoin.SelectedDate = DateTime.Now;
        chbDouble.IsChecked = false;
        dpDateDouble.Visibility = Visibility.Collapsed;
        dpDateDouble.SelectedDate = null;
    }

    private void btnExit_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void btnDubladd_Click(object sender, RoutedEventArgs e)
    {
        if (chbDouble.IsChecked != true)
            return;

        if (!dpDateDouble.SelectedDate.HasValue)
        {
            ModernDialog.Show("ScheduleDoubleVisit".T(), "Info");
            return;
        }

        MapUiToModel();
        ModernDialog.Show("Success".T(), "Info");
    }

    private void chbDouble_CheckedChanged(object sender, RoutedEventArgs e)
    {
        dpDateDouble.Visibility = Visibility.Visible;
        btnDubladd.Visibility = Visibility.Visible;
    }

    private void chbDouble_Unchecked(object sender, RoutedEventArgs e)
    {
        dpDateDouble.Visibility = Visibility.Collapsed;
        btnDubladd.Visibility = Visibility.Collapsed;
        dpDateDouble.SelectedDate = null;
    }

    private async void btnWork_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateClient())
            return;

        MapUiToModel();

        try
        {
            await EnsureClientSavedAsync();
        }
        catch (Exception ex)
        {
            ModernDialog.Show(ex.Message, "Error".T(), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var toothWin = WindowFactory.Create<ToothWindow>();
        toothWin.Owner = this;
        toothWin.Client = Model;
        toothWin.ShowDialog();
    }

    private async Task EnsureClientSavedAsync()
    {
        using var scope = App.HostInstance.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (Model.Id <= 0)
        {
            db.Clients.Add(Model);
            await db.SaveChangesAsync();
            return;
        }

        var entity = await db.Clients.FirstOrDefaultAsync(x => x.Id == Model.Id);
        if (entity is null)
        {
            db.Clients.Add(Model);
        }
        else
        {
            entity.FirstName = Model.FirstName;
            entity.LastName = Model.LastName;
            entity.MidlName = Model.MidlName;
            entity.PhoneNumber = Model.PhoneNumber;
            entity.Price = Model.Price;
            entity.Debet = Model.Debet;
            entity.DateJoin = Model.DateJoin;
            entity.DateDobleJoin = Model.DateDobleJoin;
            entity.DateJoinString = Model.DateJoinString;
        }

        await db.SaveChangesAsync();
    }
}
