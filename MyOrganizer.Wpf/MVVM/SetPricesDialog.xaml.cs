using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MyOrganizer.Wpf.MVVM
{
    public partial class SetPricesDialog : Window
    {
        private readonly List<Row> _rows;

        public SetPricesDialog(string[] byugel, string[] protez, string[] implzr, string[] implmk,
                               string[] zrkemax, string[] mk30, string[] rest, string[] plomb, string[] shift, string[] endo)
        {
            InitializeComponent();

            _rows = new()
            {
                new Row("Բյուգել", byugel),
                new Row("Պրոթեզ", protez),
                new Row("Ի/Ց/Կ", implzr),
                new Row("Ի/Մ/Կ", implmk),
                new Row("Ց/Կ", zrkemax),
                new Row("Մ/Կ", mk30),
                new Row("Պսակի վկ․ում", rest),
                new Row("Ատամնալիցք", plomb),
                new Row("Գամիկ", shift),
                new Row("Արմատալիցք", endo),
            };

            dg.ItemsSource = _rows;
        }

        public (string[] byugel, string[] protez, string[] implzr, string[] implmk,
                string[] zrkemax, string[] mk30, string[] rest, string[] plomb, string[] shift, string[] endo)
            GetAllArrays()
        {
            string[] arr(string name) =>
                _rows.First(r => r.Name == name).ToArray();

            return (arr("Բյուգել"), arr("Պրոթեզ"), arr("Ի/Ց/Կ"), arr("Ի/Մ/Կ"),
                    arr("Ց/Կ"), arr("Մ/Կ"), arr("Պսակի վկ․ում"),
                    arr("Ատամնալիցք"), arr("Գամիկ"), arr("Արմատալիցք"));
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        public class Row
        {
            public string Name { get; }
            public string Tier1 { get; set; }
            public string Tier2 { get; set; }
            public string Tier3 { get; set; }

            public Row(string name, string[] tiers)
            {
                Name = name;
                Tier1 = tiers.ElementAtOrDefault(0) ?? "0";
                Tier2 = tiers.ElementAtOrDefault(1) ?? "0";
                Tier3 = tiers.ElementAtOrDefault(2) ?? "0";
            }
            public string[] ToArray() => new[] { Tier1, Tier2, Tier3 };
        }
    }
}
