using MyOrganizer.Wpf.Entities.Procedures;
using MyOrganizer.Wpf.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MyOrganizer.Wpf.MVVM.UI
{
    // MVVM/ProceduresCatalogWindow.xaml.cs
    public partial class ProceduresCatalogWindow : Window
    {
        private readonly IProcedureService _service;
        private List<Procedure> _items = new();

        public ProceduresCatalogWindow(IProcedureService service)
        {
            InitializeComponent();
            _service = service;
            Loaded += async (_, __) =>
            {
                _items = await _service.GetAllAsync();
                lb.ItemsSource = _items;
            };
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            var name = tbName.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;
            var p = await _service.AddAsync(name);
            _items.Add(p);
            lb.Items.Refresh();
            tbName.Clear();
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (lb.SelectedItem is not Procedure p) return;
            await _service.DeleteAsync(p.Id);
            _items.Remove(p);
            lb.Items.Refresh();
        }
    }

}
