using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOrganizer.Wpf.Entities.Procedures
{
    public class Procedure
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; } = true;

        public List<ProcedurePrice>? Prices { get; set; } = new();
    }

    // Domain/Entities/ProcedurePrice.cs
    public class ProcedurePrice
    {
        public int Id { get; set; }
        public int ProcedureId { get; set; }
        public decimal Tier1 { get; set; }
        public decimal Tier2 { get; set; }
        public decimal Tier3 { get; set; }
        public string Currency { get; set; } = "AMD";
        public Procedure Procedure { get; set; } = null!;
    }
}
