using System.Windows;
using MyOrganizer.Wpf.Data.Entities;
using MyOrganizer.Wpf.MVVM.ViewModels;

namespace MyOrganizer.Wpf.MVVM.UI;

public partial class ToothWindow : Window
{
    private readonly DentalChartViewModel _chart;

    public Client Client = null!;

    public ToothWindow(DentalChartViewModel chart)
    {
        _chart = chart;
        InitializeComponent();
        PartChart.DataContext = _chart;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        TxtClientName.Text = string.Concat(Client?.FirstName ?? "", " ", Client?.LastName ?? "").Trim();
        if (Client?.Id > 0)
            await _chart.InitializeAsync(Client.Id);
    }

    private void pictureBox2_Click(object sender, RoutedEventArgs e) => Close();
}
