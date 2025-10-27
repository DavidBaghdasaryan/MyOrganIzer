using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MyOrganizer.Wpf.MVVM.UI
{
    public partial class ModernDialog : Window
    {
        private MessageBoxResult _result = MessageBoxResult.None;

        public ModernDialog()
        {
            InitializeComponent();

            // Drag window by background
            MouseLeftButtonDown += (_, e) =>
            {
                if (e.ChangedButton == MouseButton.Left) DragMove();
            };

            // Keyboard shortcuts
            PreviewKeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    _result = MessageBoxResult.Cancel;
                    Close();
                }
                else if (e.Key == Key.Enter)
                {
                    var defaultBtn = FindDefaultButton();
                    defaultBtn?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
            };
        }

        // ---------- Public static API (sync, MessageBox-like) ----------

        public static MessageBoxResult Show(string text) =>
            ShowCore(Application.Current?.MainWindow, text, "Message",
                     MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.OK);

        public static MessageBoxResult Show(string text, string caption) =>
            ShowCore(Application.Current?.MainWindow, text, caption,
                     MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.OK);

        public static MessageBoxResult Show(string text, string caption, MessageBoxButton buttons) =>
            ShowCore(Application.Current?.MainWindow, text, caption,
                     buttons, MessageBoxImage.None, DefaultFor(buttons));

        public static MessageBoxResult Show(string text, string caption, MessageBoxButton buttons, MessageBoxImage icon) =>
            ShowCore(Application.Current?.MainWindow, text, caption,
                     buttons, icon, DefaultFor(buttons));

        public static MessageBoxResult Show(Window owner, string text, string caption,
                                            MessageBoxButton buttons, MessageBoxImage icon,
                                            MessageBoxResult defaultResult = MessageBoxResult.None) =>
            ShowCore(owner, text, caption, buttons, icon,
                     defaultResult == MessageBoxResult.None ? DefaultFor(buttons) : defaultResult);

        private static MessageBoxResult ShowCore(Window? owner, string text, string caption,
                                                 MessageBoxButton buttons, MessageBoxImage icon,
                                                 MessageBoxResult defaultResult)
        {
            var dlg = new ModernDialog
            {
                Owner = owner,
                WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
            };

            dlg.TxtTitle.Text = caption;
            dlg.TxtMessage.Text = text;
            dlg.ApplyIcon(icon);
            dlg.BuildButtons(buttons, defaultResult);

            dlg.ShowDialog();
            return dlg._result;
        }

        // ---------- NEW: Async API with optional auto-close & cancellation ----------

        public static Task<MessageBoxResult> ShowAsync(
            string text,
            string caption = "Message",
            MessageBoxButton buttons = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None,
            MessageBoxResult defaultResult = MessageBoxResult.None,
            int? autoCloseSeconds = null,
            MessageBoxResult autoCloseResult = MessageBoxResult.None,
            CancellationToken cancellationToken = default)
        {
            return ShowAsync(Application.Current?.MainWindow, text, caption,
                             buttons, icon, defaultResult, autoCloseSeconds, autoCloseResult, cancellationToken);
        }

        public static Task<MessageBoxResult> ShowAsync(
            Window? owner,
            string text,
            string caption = "Message",
            MessageBoxButton buttons = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None,
            MessageBoxResult defaultResult = MessageBoxResult.None,
            int? autoCloseSeconds = null,
            MessageBoxResult autoCloseResult = MessageBoxResult.None,
            CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<MessageBoxResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            var dlg = new ModernDialog
            {
                Owner = owner,
                WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
            };

            dlg.TxtTitle.Text = caption;
            dlg.TxtMessage.Text = text;
            dlg.ApplyIcon(icon);
            dlg.BuildButtons(buttons, defaultResult == MessageBoxResult.None ? DefaultFor(buttons) : defaultResult);

            // Auto-close timer (optional)
            DispatcherTimer? timer = null;
            int remaining = autoCloseSeconds ?? 0;

            if (autoCloseSeconds.HasValue && autoCloseSeconds.Value > 0)
            {
                var chosenResult = autoCloseResult != MessageBoxResult.None ? autoCloseResult
                                    : (defaultResult == MessageBoxResult.None ? DefaultFor(buttons) : defaultResult);

                var baseTitle = caption;
                dlg.TxtTitle.Text = $"{baseTitle} ({remaining})";

                timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, (_, __) =>
                {
                    remaining--;
                    if (remaining <= 0)
                    {
                        timer!.Stop();
                        dlg._result = chosenResult;
                        dlg.Close();
                        return;
                    }
                    dlg.TxtTitle.Text = $"{baseTitle} ({remaining})";
                }, dlg.Dispatcher);
                timer.Start();
            }

            // Cancel token (optional)
            CancellationTokenRegistration ctr = default;
            if (cancellationToken.CanBeCanceled)
            {
                ctr = cancellationToken.Register(() =>
                {
                    dlg.Dispatcher.Invoke(() =>
                    {
                        dlg._result = MessageBoxResult.Cancel;
                        dlg.Close();
                    });
                });
            }

            // When closed, complete the TCS
            dlg.Closed += (_, __) =>
            {
                try { timer?.Stop(); } catch { /* ignore */ }
                ctr.Dispose();
                tcs.TrySetResult(dlg._result);
            };

            // Show modeless, but keep owner disabled like a dialog
            // (or use ShowDialog in a background await — but we want real async UI)
            dlg.Show();

            return tcs.Task;
        }

        // ---------- Icon + Buttons ----------

        private void ApplyIcon(MessageBoxImage icon)
        {
            string glyph = "\uE946"; // Info
            Brush brush = TryFindResource("Brush.Primary") as Brush
                          ?? new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB));

            switch (icon)
            {
                case MessageBoxImage.Information:
                    glyph = "\uE946"; // Info
                    brush = TryGetBrush("Brush.Primary", brush);
                    break;
                case MessageBoxImage.Question:
                    glyph = "\uE9CE"; // Question
                    brush = TryGetBrush("Brush.Accent", brush);
                    break;
                case MessageBoxImage.Warning:
                    glyph = "\uE7BA"; // Warning
                    brush = TryGetBrush("Brush.Warning", new SolidColorBrush(Color.FromRgb(0xF5, 0xA6, 0x0A)));
                    break;
                case MessageBoxImage.Error:
                    glyph = "\uEA39"; // Error
                    brush = TryGetBrush("Brush.Danger", new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C)));
                    break;
            }

            IconGlyph.Text = glyph;
            IconGlyph.Foreground = brush;

            Brush TryGetBrush(string key, Brush fallback) =>
                (TryFindResource(key) as Brush) ?? fallback;
        }

        private void BuildButtons(MessageBoxButton set, MessageBoxResult def)
        {
            ButtonsPanel.Children.Clear();

            var b1 = MakeButton("");
            var b2 = MakeButton("");
            var b3 = MakeButton("");

            void Show(Button b, string caption, bool primary, bool isDefault, MessageBoxResult returns)
            {
                b.Content = caption;
                b.Visibility = Visibility.Visible;
                b.IsDefault = isDefault;
                ApplyStyle(b, primary);
                b.Click += (_, __) => { _result = returns; Close(); };
                ButtonsPanel.Children.Add(b);
            }

            switch (set)
            {
                case MessageBoxButton.OK:
                    Show(b2, "OK", true, def is MessageBoxResult.None or MessageBoxResult.OK, MessageBoxResult.OK);
                    break;

                case MessageBoxButton.OKCancel:
                    Show(b1, "Cancel", false, def == MessageBoxResult.Cancel, MessageBoxResult.Cancel);
                    Show(b2, "OK", true, def is MessageBoxResult.None or MessageBoxResult.OK, MessageBoxResult.OK);
                    break;

                case MessageBoxButton.YesNo:
                    Show(b1, "No", false, def == MessageBoxResult.No, MessageBoxResult.No);
                    Show(b2, "Yes", true, def is MessageBoxResult.None or MessageBoxResult.Yes, MessageBoxResult.Yes);
                    break;

                case MessageBoxButton.YesNoCancel:
                    Show(b1, "No", false, def == MessageBoxResult.No, MessageBoxResult.No);
                    Show(b2, "Yes", true, def is MessageBoxResult.None or MessageBoxResult.Yes, MessageBoxResult.Yes);
                    Show(b3, "Cancel", false, def == MessageBoxResult.Cancel, MessageBoxResult.Cancel);
                    break;
            }
        }

        private Button MakeButton(string text) => new Button
        {
            Content = text,
            MinWidth = 96,
            Height = 34,
            Margin = new Thickness(8, 0, 0, 0),
            Visibility = Visibility.Collapsed
        };

        private void ApplyStyle(Button b, bool primary)
        {
            var key = primary ? "ModernButton" : "SecondaryButton";
            if (TryFindResource(key) is Style st) b.Style = st;
        }

        private Button? FindDefaultButton()
        {
            foreach (var child in ButtonsPanel.Children)
                if (child is Button b && b.IsDefault) return b;
            return null;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            _result = MessageBoxResult.Cancel;
            Close();
        }

        private static MessageBoxResult DefaultFor(MessageBoxButton b) => b switch
        {
            MessageBoxButton.OK => MessageBoxResult.OK,
            MessageBoxButton.OKCancel => MessageBoxResult.OK,
            MessageBoxButton.YesNo => MessageBoxResult.Yes,
            MessageBoxButton.YesNoCancel => MessageBoxResult.Yes,
            _ => MessageBoxResult.OK
        };
    }
}
