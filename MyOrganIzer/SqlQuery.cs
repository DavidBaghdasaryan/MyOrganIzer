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
    public class SqlQuery
    {
       static string connectionString = "server=developer.000.am,1433\\MERSOFT11;database=Ayzenq;user=dev;password=Developer1*";
        
       static SqlCommand sCommand;
        SqlDataAdapter sAdapter;
        SqlCommandBuilder sBuilder;
       static SqlConnection conn;


        public static void SaveImport(string columnname,int value,DateTime date)
        {
            conn = new SqlConnection(connectionString);
            conn.Open();


            sCommand = new SqlCommand($"insert into Imprtno ( Tig, [Lev/SOC], adnt,non, Dntx, [s/zmi], nardhakz, Napki, [inTak], invoc, Amint,Time) " +
                $"values (null,null,null,null,null,null,null,null,null,null,null,null) update Imprtno set Time='{date.ToString("yyyy-MM-dd HH:mm:ss")}', [{columnname}]='{value}' where id=(Select top 1 id from Imprtno order by id desc)", conn);
                         
            try
            {
                sCommand.ExecuteNonQuery();
                MessageBox.Show("Տվյալները գրանցված են");
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
        public static void SaveTecnic(string name, string columnname, int value, DateTime date)
        {
            conn = new SqlConnection(connectionString);
            conn.Open();


            sCommand = new SqlCommand($"insert into Tecno ( Name, [IMP M/K],[M/K],CR, Byuget, Time, PRT, MISAbot, Gog) " +
                $"values (null,null,null,null,null,null,null,null,null) update Tecno set  Time='{date.ToString("yyyy-MM-dd HH:mm:ss")}',Name='{name}', [{columnname}]='{value}' where id=(Select top 1 id from Tecno order by id desc)", conn);

            try
            {
                sCommand.ExecuteNonQuery();
                MessageBox.Show("Տվյալները գրանցված են");
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
        public static void Update(string table, string columnname, string datcolumne, int value, DateTime date,int id)
        {
            conn = new SqlConnection(connectionString);
            conn.Open();


            sCommand = new SqlCommand($" update {table} set {datcolumne}='{date.ToString("yyyy-MM-dd HH:mm:ss")}', [{columnname}]='{value}' where id={id}", conn);

            try
            {
                sCommand.ExecuteNonQuery();
                MessageBox.Show("Տվյալները գրանցված են");
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
        public static void UpdateTecnic(string table, string columnname,string name, string datcolumne, int value, DateTime date, int id)
        {
            conn = new SqlConnection(connectionString);
            conn.Open();


            sCommand = new SqlCommand($" update {table} set Name='{name}', {datcolumne}='{date.ToString("yyyy-MM-dd HH:mm:ss")}', [{columnname}]='{value}' where id={id}", conn);

            try
            {
                sCommand.ExecuteNonQuery();
                MessageBox.Show("Տվյալները գրանցված են");
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
        public static object SelectSum(string table, string columnname,string datcolumne, DateTime date)
        {
            conn = new SqlConnection(connectionString);
            conn.Open();


            sCommand = new SqlCommand($" SELECT SUM({columnname}) FROM {table}  WHERE MONTH( {datcolumne}) ='{date.Month}'",conn);
            object result = (object)sCommand.ExecuteScalar();
            try
            {
                sCommand.ExecuteNonQuery();
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                conn.Close();
            }
            return result;
        }
        public static void Delete(string table, int id)
        {
            conn = new SqlConnection(connectionString);
            conn.Open();


            sCommand = new SqlCommand($" Delete from {table}  where id={id}", conn);

            try
            {
                sCommand.ExecuteNonQuery();
                MessageBox.Show("Նշված տողը հեռացված է");
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
        public static void ShowDatagrid(DataGridView dataGrid,string sql)
        {
            var dt = new DataTable();
            var adpt = new SqlDataAdapter(sql, connectionString);
            adpt.Fill(dt);
            dataGrid.DataSource = dt;
        }



    }


}
