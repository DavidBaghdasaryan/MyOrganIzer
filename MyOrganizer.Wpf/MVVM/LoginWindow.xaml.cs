using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MyOrganizer.Wpf.Config;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.Services.DB_LocalizationService;

namespace MyOrganizer.Wpf.MVVM
{
    public partial class LoginWindow : Window
    {
        Ilan
        public LoginWindow()
        {
            InitializeComponent();

            UpdateWatermarkVisibility();
            Loaded += (_, __) =>
            {
                // Preselect the saved language
                var lang = AppSettings.CurrentLang?.ToLowerInvariant() ?? "hy";
                var item = CmbLanguage.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(x => (x.Tag?.ToString() ?? "").ToLowerInvariant() == lang);
                CmbLanguage.SelectedItem = item ?? CmbLanguage.Items[0];
            };

            PasswordBox.Focus();
        }

        private void UpdateWatermarkVisibility()
        {
            Watermark.Visibility = string.IsNullOrEmpty(PasswordBox.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void BtnEnter_Click(object sender, RoutedEventArgs e)
        {
            TryLogin();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TryLogin();
            }
        }

        private void PasswordBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // clicking should remove placeholder feel
            if (string.IsNullOrEmpty(PasswordBox.Password))
                UpdateWatermarkVisibility();
        }

        private void TryLogin()
        {
            // Replace this with real check later
            if (PasswordBox.Password == "1234")
            {
                OpenMainAndClose();
            }
            else
            {
                MessageBox.Show("Incorrectpassword".T(), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
                PasswordBox.Password = string.Empty;
                UpdateWatermarkVisibility();
                PasswordBox.Focus();
            }
        }

        private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lang = (CmbLanguage.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "en";
            AppSettings.CurrentLang = lang;

            // Warm up cache for selected language
            var loc = App.HostInstance.Services.GetRequiredService<IDbLocalizationService>();
            loc.WarmUpAsync(lang);
        }

        private void OpenMainAndClose()
        {
            // Resolve MainWindow from DI and open it
            var main = App.HostInstance.Services.GetRequiredService<MainWindow>();
            this.Hide();
            main.Closed += (_, __) => this.Close();   // close login when main exits
            main.Show();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // allow dragging since WindowStyle=None
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Keep watermark state in sync if PasswordBox changes at runtime
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            UpdateWatermarkVisibility();
        }

        // If you later bind PasswordBox to VM, hook PasswordChanged:
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdateWatermarkVisibility();
        }
    }
}
