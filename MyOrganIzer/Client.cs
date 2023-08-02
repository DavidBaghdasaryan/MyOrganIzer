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
       static string connectionString = "server=(LocalDb)\\MSSQLLocalDB;database=Ayzenq";
        string sql = "SELECT * FROM Answers";
        SqlCommand sCommand;
        SqlDataAdapter sAdapter;
        SqlCommandBuilder sBuilder;
       static SqlConnection conn;
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
        public DateTime DateJoin { get; set; } = new DateTime(1900-01-01);
        public DateTime DateDobleJoin { get; set; } = new DateTime(1900-01-01);
        public string DateJoinString { get; set; }
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
                sCommand = new SqlCommand($"insert into Answers values(N'{ FirstName }' , N'{ LastName }' ," +
                    $"N'{ MidlName }','{DateJoin.ToString("yyyy-MM-dd HH:mm:ss")}',{ Price },{Debet},{PhoneNumber},'{DateDobleJoin.ToString("yyyy-MM-dd HH:mm:ss")}','{DateJoinString}')", conn); 
            }
            else
            {
                sCommand = new SqlCommand($"update Answers set Name=N'{FirstName}', LastName=N'{LastName}'," +
                    $" MidlName=N'{MidlName}', DateDoubleJoin='{DateDobleJoin.ToString("yyyy-MM-dd HH:mm:ss")}', DateJoin='{DateJoin.ToString("yyyy-MM-dd HH:mm:ss")}',Price={Price},Debt={Debet},PhoneNumbers={PhoneNumber},DateJoinString='{DateJoinString}' where Id={Id}", conn);
            }
            try
            {
                sCommand.ExecuteNonQuery();
                M.OKCencel(MessegesTyp.OKCenc,"Տվյալները գրանցված են");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }
        public static object[] SalectDatduoblejoin()
        {
            List<object> str = new List<object>();
            conn = new SqlConnection(connectionString);
            try
            {
                SqlCommand comm = new SqlCommand("SELECT  id,DateDoubleJoin FROM Answers", conn);
               
                conn.Open();

                SqlDataReader reader = comm.ExecuteReader();
                
          
                while (reader.Read())
                {
                    object[] values = new object[reader.FieldCount];
                    reader.GetValues(values);
                    str.Add(values);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                M.OKCencel(MessegesTyp.OKCenc, ex.Message);
            }
            finally
            {
                conn.Close();
            }
            return str.ToArray();
        }
        public void Delete()
        {
            conn = new SqlConnection(connectionString);
            conn.Open();
            sCommand = new SqlCommand($"DELETE FROM Answers where Id={Id} DELETE FROM ABC where EPMLID={Id} DELETE FROM Emploit where EmploeId={Id}", conn);
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
