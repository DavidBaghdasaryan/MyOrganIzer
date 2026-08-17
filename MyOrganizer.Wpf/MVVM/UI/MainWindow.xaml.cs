using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using MyOrganizer.Wpf;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.Services;

namespace MyOrganizer.Wpf.MVVM.UI;

public partial class MainWindow : Window
{
    private readonly IReminderService _reminderService;
    private readonly DispatcherTimer _timer;
    private bool _blink;
    private string[] _messages = [];

    public MainWindow(IReminderService reminderService)
    {
        InitializeComponent();
        _reminderService = reminderService;
        BtnMessage.Visibility = Visibility.Collapsed;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => Blink();
        Closed += (_, _) => _timer.Stop();

        Loaded += async (_, _) =>
        {
            try
            {
                var items = await _reminderService.LoadTodaysAsync();
                var session = "session".T();
                if (items.Count == 0)
                    return;

                _messages = items
                    .OrderBy(i => i.When)
                    .Select(i => $"{i.FullName}  {i.When:HH:mm}  {session}")
                    .ToArray();

                BtnMessage.Visibility = Visibility.Visible;
                _timer.Start();
            }
            catch (Exception ex)
            {
                ModernDialog.Show(ex.Message, "Error".T(), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
    }

    private void Blink()
    {
        _blink = !_blink;
        BtnMessage.Background = _blink ? Brushes.SkyBlue : Brushes.DeepSkyBlue;
    }

    private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            DragMove();
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void BtnExit_Click(object sender, RoutedEventArgs e) => Close();

    private void BtnPcainet_Click(object sender, RoutedEventArgs e)
    {
        var win = WindowFactory.Create<ClientsWindow>();
        win.Owner = this;
        win.ShowDialog();
    }

    private void BtnSetPrice_CLick(object sender, RoutedEventArgs e)
    {
        var win = WindowFactory.Create<SetPricesDialog>();
        win.Owner = this;
        win.ShowDialog();
    }

    private void BtnSenders_Click(object sender, RoutedEventArgs e)
    {
        ModernDialog.Show("Open Couriers", "Info");
    }

    private void BtnTexniqs_Click(object sender, RoutedEventArgs e)
    {
        var win = WindowFactory.Create<TechnicsWindow>();
        win.Owner = this;
        win.ShowDialog();
    }

    private void BtnMessage_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        var message = string.Join(Environment.NewLine, _messages);
        ModernDialog.Show(message, "Reminders".T());
        BtnMessage.Visibility = Visibility.Collapsed;
    }
}
