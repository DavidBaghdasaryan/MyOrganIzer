using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOrganizer.Wpf.Entities
{
    public class Technic
    {
        public int Id { get; set; }
        public string Type { get; set; } = "";
        public int Price { get; set; }
        public DateTime Date { get; set; }
        public string Name { get; set; } = "";
    }

}
