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
       static string connectionString = "server=(LocalDb)\\MSSQLLocalDB;database=Ayzenq";


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
        public static void SaveTecnic(string name, string columnname, int value, DateTime date)
        {
            conn = new SqlConnection(connectionString);
            conn.Open();


            sCommand = new SqlCommand($"insert into Tecno ( Name, [IMP M/K],[M/K],CR, Byuget, Time, PRT, MISAbot, Gog) " +
                $"values (null,null,null,null,null,null,null,null,null) update Tecno set  Time='{date.ToString("yyyy-MM-dd HH:mm:ss")}',Name='{name}', [{columnname}]='{value}' where id=(Select top 1 id from Tecno order by id desc)", conn);

            try
            {
                sCommand.ExecuteNonQuery();
                M.OKCencel(MessegesTyp.OKCenc, "Տվյալները գրանցված են");
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
                M.OKCencel(MessegesTyp.OKCenc, "Տվյալները գրանցված են");
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
                M.OKCencel(MessegesTyp.OKCenc, "Տվյալները գրանցված են");
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


            sCommand = new SqlCommand($" SELECT SUM([{columnname}]) FROM {table}  WHERE MONTH( {datcolumne}) ='{date.Month}'",conn);
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
                M.OKCencel(MessegesTyp.OKCenc, "Նշված տողը հեռացված է");
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
        public static int Id()
        {
            conn = new SqlConnection(connectionString);
            conn.Open();


            sCommand = new SqlCommand($" select top 1 id from Answers order by id desc", conn);
            int result = (int)sCommand.ExecuteScalar();
            return result;
        }
        
        public static void SaveTooth(int empid,string tid,int b,int prt,int imlz,int impmk,int zrk,int mk30,int rest, int plomb,int shift, int endo,bool up,bool add)
        {
            conn = new SqlConnection(connectionString);
            conn.Open();


           
            
                sCommand = new SqlCommand($"insert into Emploit(EmploeId,ToothId,Byugel,Protez,[Impl/zr],[impl/mk],[zr/k,emax],mk30,rest,plomb,shift,endo,up)  values ({empid},'{tid}',{b},{prt},{imlz},{impmk},{zrk},{mk30},{rest}," +
                        $"{plomb},{shift},{endo},'{up}')", conn); 
            
           
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
        }
        public static void CackingToot(int id)
        {
            conn = new SqlConnection(connectionString);
            conn.Open();


            sCommand = new SqlCommand($"   insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('Byugel',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})" +
                $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('Protez',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})" +
                $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('[Impl/zr]',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})" +
                $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('[Impl/mk]',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})" +
                $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('[zr/k,emax]',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})" +
                $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('mk30',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})" +
                $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('rest',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})" +
                $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('plomb',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})" +
                $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('shift',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})" +
                $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('endo',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})" +
                 $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values (null,8,7,6,5,4,3,2,1,1,2,3,4,5,6,7,8,{id})" +
                 $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('endo.',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})" +
                $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('shift.',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})" +
                $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('plomb.',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})" +
                $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('rest.',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})" +
                $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('mk30.',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})" +
                $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('[zr/k,emax].',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})" +
                $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('[Impl/mk].',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})" +
                $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('[Impl/zr].',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})" +
                $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('Protez.',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})" +
                $"insert into ABC(A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,EpmlId)  values ('Byugel.',null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,{id})"
                , conn);

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
        }

        public static void LoadToot(int id)
        {
            conn = new SqlConnection(connectionString);
            conn.Open();


            sCommand = new SqlCommand($"SELECT * FROM ABC WHERE ID={id}", conn);
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
        }

        public static void UpdateToots(string columnname, int id,   int value, string rowName)
        {
            conn = new SqlConnection(connectionString);
            conn.Open();


            sCommand = new SqlCommand($" update ABC set {columnname}='{value}'    where A='{rowName}' and  epmlid={id}", conn);

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
        }
        static string[] text;
        public static List<object[]> SqlSelect(string sql,string table)
        {
            List<object[]> dataList = new List<object[]>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            object[] tempRow = new object[reader.FieldCount];
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                tempRow[i] = reader[i];
                            }
                            dataList.Add(tempRow);
                        }
                    }
                }
            }

            return dataList;
        }
    }


}
