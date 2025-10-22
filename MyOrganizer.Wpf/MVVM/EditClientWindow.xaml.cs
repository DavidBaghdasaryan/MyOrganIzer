using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using MyOrganizer.Wpf.Data.Entities;

namespace MyOrganizer.Wpf.MVVM
{
    public partial class EditClientWindow : Window
    {
        public Client Model { get; private set; }
        private readonly bool _isEdit;

        // New client
        public EditClientWindow()
        {
            InitializeComponent();

            Model = new Client
            {
                DateJoin = DateTime.Now,
                DateDobleJoin = DateTime.MinValue
            };

            dpDateJoin.SelectedDate = Model.DateJoin;
        }

        // Edit existing
        public EditClientWindow(Client client) : this()
        {
            _isEdit = true;
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

            // Fill UI
            txtName.Text = Model.FirstName;
            txtLastName.Text = Model.LastName;
            txtMidlName.Text = Model.MidlName;
            txtPhoneNumber.Text = Model.PhoneNumber;
            txtPrice.Text = Model.Price?.ToString(CultureInfo.InvariantCulture) ?? "0";
            txtDebt.Text = Model.Debet?.ToString(CultureInfo.InvariantCulture) ?? "0";
            dpDateJoin.SelectedDate = Model.DateJoin == default ? DateTime.Now : Model.DateJoin;

            if (Model.DateDobleJoin != default && Model.DateDobleJoin > DateTime.MinValue)
            {
                chbDouble.IsChecked = true;
                dpDateDouble.Visibility = Visibility.Visible;
                dpDateDouble.SelectedDate = Model.DateDobleJoin;
            }
        }

        // Drag the borderless window
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private bool ValidateClient()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) ||
                string.IsNullOrWhiteSpace(txtLastName.Text) ||
                string.IsNullOrWhiteSpace(txtMidlName.Text))
            {
                MessageBox.Show("Հաճախորդի տվյալները լրացված չեն", "Սխալ", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            if (decimal.TryParse(txtPrice.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                Model.Price = price;
            else
                Model.Price = 0;

            if (decimal.TryParse(txtDebt.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var debt))
                Model.Debet = debt;
            else
                Model.Debet = 0;

            Model.DateJoin = dpDateJoin.SelectedDate ?? DateTime.Now;

            if (chbDouble.IsChecked == true && dpDateDouble.SelectedDate.HasValue)
            {
                Model.DateDobleJoin = dpDateDouble.SelectedDate.Value;
                Model.DateJoinString = Model.DateDobleJoin.ToString("dd-MM-yyyy HH:mm:ss");
            }
            else
            {
                Model.DateDobleJoin = DateTime.MinValue;
                Model.DateJoinString = null;
            }
        }

        // Գրանցել (OK)
        private void btnSave1_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateClient()) return;

            MapUiToModel();
            // Do NOT save to DB here — let the caller (ClientsWindow) handle persistence.
            DialogResult = true;
            Close();
        }

        // Մաքրել
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

        // ՓԱԿԵԼ
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // “Պահպանել կրկնակի այցը” (WinForms btnDubladd_Click semantics)
        private void btnDubladd_Click(object sender, RoutedEventArgs e)
        {
            if (chbDouble.IsChecked == true)
            {
                if (!dpDateDouble.SelectedDate.HasValue)
                {
                    MessageBox.Show("Ընտրեք կրկնակի այցի օրը", "Info");
                    return;
                }
                // Just map & keep dialog open; caller can save later if needed
                MapUiToModel();
                MessageBox.Show("Կրկնակի այցը պահպանված է (մոդելի մեջ):", "Info");
            }
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

       
        private void btnWork_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateClient()) return;

            // Ensure Model up-to-date before opening tooth UI
            MapUiToModel();

    
            var toothWin =  App.HostInstance.Services.GetRequiredService<ToothWindow>();
            toothWin.Owner = this;
            toothWin.Client = Model;
            toothWin.ShowDialog();
        }
    }
}
