using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyOrganIzer
{
    public class Client
    {
        string connectionString = "server=developer.000.am,1433\\MERSOFT11;database=Ayzenq;user=dev;password=Developer1*";
        string sql = "SELECT * FROM Answers";
        SqlCommand sCommand;
        SqlDataAdapter sAdapter;
        SqlCommandBuilder sBuilder;
        SqlConnection conn;
        public Client()
        {
            DateJoin  = new DateTime(1900/01/01);
        }
        public int Id { get; set; }
        public static bool edit { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MidlName { get; set; }
        public decimal? Price { get; set; }
        public decimal? Debet { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateJoin { get; set; } = new DateTime(1900/01/01);

        public void ShowDatagrid(DataGridView dataGrid)
        {
            var dt = new DataTable();
            var adpt = new SqlDataAdapter(sql, connectionString);
            adpt.Fill(dt);
            dataGrid.DataSource = dt;
        }

        public void SaveOrUpdaet()
        {
            conn = new SqlConnection(connectionString);
            conn.Open();
            if (!edit)
            {
                sCommand = new SqlCommand($"insert into Answers values('{ FirstName }' , '{ LastName }' ," +
                    $"'{ MidlName }','{DateJoin.ToString("yyyy-MM-dd HH:mm:ss")}',{ Price },{Debet},{PhoneNumber})", conn); 
            }
            else
            {
                sCommand = new SqlCommand($"update Answers set Name='{FirstName}', LastName='{LastName}'," +
                    $" MidlName='{MidlName}', DateJoin='{DateJoin.ToString("yyyy-MM-dd HH:mm:ss")}',Price={Price},Debt={Debet},PhoneNumbers={PhoneNumber} where Id={Id}", conn);
            }
            try
            {
                sCommand.ExecuteNonQuery();
                M.OKCencel(MessegesTyp.OKCenc,"Տվյալները գրանցված են");
            }
            catch (Exception)
            {
                MessageBox.Show("Տեղի է ունեցել սխալ");
            }
            finally
            {
                conn.Close();
            }
        }
        public void Delete()
        {
            conn = new SqlConnection(connectionString);
            conn.Open();
            sCommand = new SqlCommand($"DELETE FROM Answers where Id={Id}", conn);
            try
            {
                sCommand.ExecuteNonQuery();
                M.OKCencel(MessegesTyp.OKCenc);
            }
            catch (Exception)
            {
                M.OKCencel(MessegesTyp.OKCenc,"Տեղի է ունեցել սխալ");
            }
            finally
            {
                conn.Close();
            }
        }
        public  void SelectBedt()
        {
            conn = new SqlConnection(connectionString);
            conn.Open();
            sCommand = new SqlCommand($"SELECT * FROM Answers where Debt>0", conn);
            try
            {
                sCommand.ExecuteNonQuery();
            }
            catch (Exception)
            {
                MessageBox.Show("Տեղի է ունեցել սխալ");
            }
            finally
            {
                conn.Close();
            }
        }
    }
}
