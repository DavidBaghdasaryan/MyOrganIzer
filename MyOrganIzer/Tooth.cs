using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyOrganIzer
{
   public class Tooth
    {
        public int EmployeId { get; set; }
        public string TootNumber { get; set; }
        public int byugel { get; set; }
        public int protez { get; set; }
        public int implantzr { get; set; }
        public int implmk { get; set; }
        public int zrkemax { get; set; }
        public int mk30 { get; set; }
        public int rest { get; set; }
        public int plomb { get; set; }
        public int shift { get; set; }
        public int endo { get; set; }
        public bool Up { get; set; }
        public void Save(bool Add)
        {
            SqlQuery.SaveTooth(EmployeId, TootNumber, byugel, protez, implantzr, implmk, zrkemax, mk30, rest, plomb, shift, endo, Up,Add);
        }
    }
}
