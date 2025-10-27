using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MyOrganizer.Wpf.MVVM.Helpers
{
    public static class Dialogs
    {
        /// <summary>
        /// Modern, themed MessageBox replacement with the same Show(...) overload style.
        /// </summary>
        public static class ModernMessageBox
        {
            // ---------------------------
            // Public API (like MessageBox)
            // ---------------------------

            public static MessageBoxResult Show(string messageBoxText) =>
                ShowCore(owner: Application.Current?.MainWindow, messageBoxText, "Message",
                         MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.OK);

            public static MessageBoxResult Show(string messageBoxText, string caption) =>
                ShowCore(Application.Current?.MainWindow, messageBoxText, caption,
                         MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.OK);

            public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button) =>
                ShowCore(Application.Current?.MainWindow, messageBoxText, caption,
                         button, MessageBoxImage.None, DefaultFor(button));

            public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon) =>
                ShowCore(Application.Current?.MainWindow, messageBoxText, caption,
                         button, icon, DefaultFor(button));

            public static MessageBoxResult Show(Window owner, string messageBoxText, string caption,
                                                MessageBoxButton button, MessageBoxImage icon,
                                                MessageBoxResult defaultResult = MessageBoxResult.None) =>
                ShowCore(owner, messageBoxText, caption, button, icon,
                         defaultResult == MessageBoxResult.None ? DefaultFor(button) : defaultResult);


            // ---------------------------
            // Implementation
            // ---------------------------

            private static MessageBoxResult ShowCore(Window? owner, string text, string title,
                                                     MessageBoxButton buttons, MessageBoxImage icon,
                                                     MessageBoxResult defaultResult)
            {
                // root window (code-only, no XAML)
                var win = new Window
                {
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ShowInTaskbar = false,
                    Background = Brushes.Transparent,
                    AllowsTransparency = true,
                    WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
                    Owner = owner
                };

                // Resolve theme brushes (fall back to sensible defaults)
                Brush Card() => TryRes<Brush>(win, "Brush.Card") ?? new SolidColorBrush(Color.FromRgb(0xF9, 0xFA, 0xFB));
                Brush Border() => TryRes<Brush>(win, "Brush.Border") ?? new SolidColorBrush(Color.FromRgb(0xD1, 0xD5, 0xDB));
                Brush Text() => TryRes<Brush>(win, "Brush.Text") ?? Brushes.Black;
                Brush Accent() => TryRes<Brush>(win, "Brush.Accent") ?? new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB));
                Brush Primary() => TryRes<Brush>(win, "Brush.Primary") ?? Accent();
                Brush Warning() => TryRes<Brush>(win, "Brush.Warning") ?? new SolidColorBrush(Color.FromRgb(0xF5, 0xA6, 0x0A));
                Brush Danger() => TryRes<Brush>(win, "Brush.Danger") ?? new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C));

                // Container Border
                var chrome = new Border
                {
                    CornerRadius = new CornerRadius(12),
                    Background = Card(),
                    BorderBrush = Border(),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(16)
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // title
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // body
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // buttons
                chrome.Child = grid;
                win.Content = chrome;

                // Title bar
                var titleDock = new DockPanel();
                Grid.SetRow(titleDock, 0);
                grid.Children.Add(titleDock);

                var txtTitle = new TextBlock
                {
                    Text = title,
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Text(),
                    VerticalAlignment = VerticalAlignment.Center
                };
                DockPanel.SetDock(txtTitle, Dock.Left);
                titleDock.Children.Add(txtTitle);

                var btnClose = new Button
                {
                    Content = "✕",
                    Width = 28,
                    Height = 28,
                    Margin = new Thickness(8, 0, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                ApplyStyle(win, btnClose, primary: false);
                btnClose.Click += (_, __) => { _result = MessageBoxResult.Cancel; win.Close(); };
                DockPanel.SetDock(btnClose, Dock.Right);
                titleDock.Children.Add(btnClose);

                // Icon + text
                var body = new Grid { Margin = new Thickness(0, 12, 0, 16) };
                body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
                body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
                body.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                Grid.SetRow(body, 1);
                grid.Children.Add(body);

                var iconGlyph = new TextBlock
                {
                    FontFamily = new FontFamily("Segoe Fluent Icons"),
                    FontSize = 24,
                    VerticalAlignment = VerticalAlignment.Top
                };
                (string glyph, Brush brush) = icon switch
                {
                    MessageBoxImage.Information => ("\uE946", Primary()),
                    MessageBoxImage.Question => ("\uE9CE", Accent()),
                    MessageBoxImage.Warning => ("\uE7BA", Warning()),
                    MessageBoxImage.Error => ("\uEA39", Danger()),
                    _ => ("\uE946", Primary())
                };
                iconGlyph.Text = glyph;
                iconGlyph.Foreground = brush;
                body.Children.Add(iconGlyph);

                var txt = new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Text(),
                    MaxWidth = 560
                };
                Grid.SetColumn(txt, 2);
                body.Children.Add(txt);

                // Buttons
                var buttonsPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                buttonsPanel.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 0));
                buttonsPanel.SetValue(Panel.ZIndexProperty, 1);
                buttonsPanel.SetValue(FrameworkElement.TagProperty, "buttons");
                buttonsPanel.SetValue(Panel.IsItemsHostProperty, false);
                Grid.SetRow(buttonsPanel, 2);
                grid.Children.Add(buttonsPanel);

                var btn1 = MakeButton(win, ""); // left/secondary
                var btn2 = MakeButton(win, ""); // primary/default
                var btn3 = MakeButton(win, ""); // right/secondary
                buttonsPanel.Children.Add(btn1);
                buttonsPanel.Children.Add(btn2);
                buttonsPanel.Children.Add(btn3);

                BuildButtons(buttons, defaultResult, btn1, btn2, btn3);

                // Keyboard behavior
                win.PreviewKeyDown += (_, e) =>
                {
                    if (e.Key == Key.Escape)
                    {
                        _result = MessageBoxResult.Cancel;
                        win.Close();
                        return;
                    }
                    if (e.Key == Key.Enter)
                    {
                        if (btn2.IsDefault) btn2.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        else if (btn1.IsDefault) btn1.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        else if (btn3.IsDefault) btn3.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                };

                // Show dialog
                _result = MessageBoxResult.None;
                win.ShowDialog();
                return _result;

                // ------- local functions -------

                static T? TryRes<T>(FrameworkElement fe, string key) where T : class =>
                    fe.TryFindResource(key) as T;

                static Button MakeButton(Window win, string text)
                {
                    var b = new Button
                    {
                        Content = text,
                        MinWidth = 96,
                        Height = 34,
                        Margin = new Thickness(8, 0, 0, 0),
                        Visibility = Visibility.Collapsed
                    };
                    ApplyStyle(win, b, primary: false);
                    return b;
                }

               static void ApplyStyle(Window win2, Button b, bool primary)
                {
                    // Use your themed styles if present
                    var key = primary ? "ModernButton" : "SecondaryButton";
                    if (TryRes<Style>(win2, key) is Style st) b.Style = st;
                }

                void BuildButtons(MessageBoxButton set, MessageBoxResult def, Button b1, Button b2, Button b3)
                {
                    void Show(Button b, string content, bool primary, bool isDefault, MessageBoxResult onClick)
                    {
                        b.Content = content;
                        b.Visibility = Visibility.Visible;
                        b.IsDefault = isDefault;
                        ApplyStyle(win, b, primary);
                        b.Click += (_, __) => { _result = onClick; win.Close(); };
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
            }

            private static MessageBoxResult _result;

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
}
