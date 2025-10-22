using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOrganizer.Wpf.MVVM.DTOs
{
    public class PriceRowDto
    {
        public int ProcedureId { get; set; }
        public string Name { get; set; } = "";
        public decimal Tier1 { get; set; }
        public decimal Tier2 { get; set; }
        public decimal Tier3 { get; set; }
        public string Currency { get; set; } = "AMD";
    }
}
