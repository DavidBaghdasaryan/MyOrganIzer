using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        Loaded += (_, _) =>
        {
            var lang = AppSettings.CurrentLang.ToLowerInvariant();
            var item = CmbLanguage.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(x => (x.Tag?.ToString() ?? "").ToLowerInvariant() == lang);
            CmbLanguage.SelectedItem = item ?? CmbLanguage.Items[0];
            PasswordBox.Focus();
        };
    }

    private void BtnEnter_Click(object sender, RoutedEventArgs e) => TryLogin();

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            TryLogin();
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
        Application.Current.MainWindow = main;
        main.Show();
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (FindParent<Button>(e.OriginalSource as DependencyObject) is not null)
            return;
        if (FindParent<ComboBox>(e.OriginalSource as DependencyObject) is not null)
            return;
        if (FindParent<PasswordBox>(e.OriginalSource as DependencyObject) is not null)
            return;

        try { DragMove(); }
        catch { /* ignore invalid drag */ }
    }

    private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T match)
                return match;
            child = VisualTreeHelper.GetParent(child);
        }
        return null;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
