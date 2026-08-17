using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MyOrganizer.Wpf;
using MyOrganizer.Wpf.Config;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.Services.DB_LocalizationService;

namespace MyOrganizer.Wpf.MVVM.UI;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        UpdateWatermarkVisibility();
        Loaded += (_, _) =>
        {
            var lang = AppSettings.CurrentLang.ToLowerInvariant();
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

    private void BtnEnter_Click(object sender, RoutedEventArgs e) => TryLogin();

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            TryLogin();
    }

    private void PasswordBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (string.IsNullOrEmpty(PasswordBox.Password))
            UpdateWatermarkVisibility();
    }

    private void TryLogin()
    {
        var password = PasswordBox.Password;
        if (string.IsNullOrWhiteSpace(password))
        {
            ModernDialog.Show("Required".T(), "Error".T(), MessageBoxButton.OK, MessageBoxImage.Warning);
            PasswordBox.Focus();
            return;
        }

        if (!AppSettings.HasPassword)
        {
            var confirm = ModernDialog.Show(
                "PasswordRequest".T(),
                "Confirm".T(),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
                return;

            AppSettings.SetPassword(password);
            OpenMainAndClose();
            return;
        }

        if (AppSettings.VerifyPassword(password))
        {
            OpenMainAndClose();
            return;
        }

        ModernDialog.Show("Incorrectpassword".T(), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
        PasswordBox.Password = string.Empty;
        UpdateWatermarkVisibility();
        PasswordBox.Focus();
    }

    private async void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var lang = (CmbLanguage.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "en";
        AppSettings.SetLanguage(lang);

        try
        {
            var loc = App.HostInstance.Services.GetRequiredService<IDbLocalizationService>();
            await loc.WarmUpAsync(lang);
        }
        catch
        {
            // UI already switched; strings fall back to keys if cache is cold.
        }
    }

    private void OpenMainAndClose()
    {
        var main = WindowFactory.Create<MainWindow>();
        Hide();
        main.Closed += (_, _) => Close();
        main.Show();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source &&
            (source is ComboBox || source is TextBox || source is PasswordBox || source is Button))
            return;

        try { DragMove(); }
        catch { /* ignore invalid drag */ }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        UpdateWatermarkVisibility();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) =>
        UpdateWatermarkVisibility();
}
