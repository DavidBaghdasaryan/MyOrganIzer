using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyOrganIzer.Entites
{
    public class Client
    {
        public int Id { get; set; }
        public static bool edit { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MidlName { get; set; }
        public decimal? Price { get; set; }
        public decimal? Debet { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateJoin { get; set; } = new DateTime(1900-01-01);
        public DateTime DateDobleJoin { get; set; } = new DateTime(1900-01-01);
        public string DateJoinString { get; set; }
        
    }
}
