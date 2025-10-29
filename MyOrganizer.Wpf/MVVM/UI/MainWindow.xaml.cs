// MainWindow.xaml.cs
using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using MyOrganizer.Wpf.Extensions;
using MyOrganizer.Wpf.MVVM;
using MyOrganizer.Wpf.Services;

namespace MyOrganizer.Wpf.MVVM.UI
{
    public partial class MainWindow : Window
    {
        private readonly IReminderService _reminderService;
        private readonly DispatcherTimer _timer;
        private bool _blink;
        private string[] _messages = Array.Empty<string>();

        public MainWindow(IReminderService reminderService)
        {
            InitializeComponent();
            _reminderService = reminderService;
            BtnMessage.Visibility=Visibility.Collapsed;
            // Load today's reminders and start blinking if any
            Loaded += async (_, __) =>
            {
                var items = await _reminderService.LoadTodaysAsync();
                string session = "session".T();
                if (items.Count > 0)
                {
                    _messages = items
                        .OrderBy(i => i.When)
                        .Select(i => $"{i.FullName}  {i.When:HH:mm}  {session}")
                        .ToArray();

                    BtnMessage.Visibility = Visibility.Visible;
                    StartBlink();
                }
            };

            // timer like WinForms Timer
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (_, __) => Blink();
        }

        private void StartBlink() => _timer.Start();

        private void Blink()
        {
            _blink = !_blink;
            BtnMessage.Background = _blink ? Brushes.SkyBlue : Brushes.DeepSkyBlue;
        }

        // Drag the window (WindowStyle=None)
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void BtnExit_Click(object sender, RoutedEventArgs e)
            => Close();

        private void BtnPcainet_Click(object sender, RoutedEventArgs e)
        {
            var win = App.HostInstance.Services.GetRequiredService<ClientsWindow>();
            win.Owner = this;
            win.ShowDialog();
        }
        private void BtnSetPrice_CLick(object sender, RoutedEventArgs e)
        {
            var win = App.HostInstance.Services.GetRequiredService<SetPricesDialog>();
            win.Owner = this;
            win.ShowDialog();
        }
        

        private void BtnSenders_Click(object sender, RoutedEventArgs e)
        {
            // TODO: open Couriers/Senders window/page
            ModernDialog.Show("Open Couriers", "Info");
        }

        private void BtnTexniqs_Click(object sender, RoutedEventArgs e)
        {
            var win = App.HostInstance.Services.GetRequiredService<TechnicsWindow>();
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
}
