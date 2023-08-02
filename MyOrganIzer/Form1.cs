using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace MyOrganIzer
{
    public partial class FromWorkSpace : Form
    {
        Thread thread;
        string connectionString = "server=(LocalDb)\\MSSQLLocalDB;database=Ayzenq";
        string sql = "SELECT * FROM Imprtno";
        SqlCommand sCommand;
        SqlDataAdapter sAdapter;
        SqlCommandBuilder sBuilder;
        string tableName = "Imprtno";
        public FromWorkSpace()
        {
            InitializeComponent();


            DataSet sDs;
            DataTable sTable;


            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            sCommand = new SqlCommand(sql, connection);
            sAdapter = new SqlDataAdapter(sCommand);
            sBuilder = new SqlCommandBuilder(sAdapter);
            sDs = new DataSet();
            sAdapter.Fill(sDs, "Imprtno");
            sTable = sDs.Tables["Imprtno"];
            connection.Close();
            dgvimpornents.DataSource = sDs.Tables["Imprtno"];
            dgvimpornents.Columns["ID"].Visible = false;
            dgvimpornents.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgvimpornents.Columns[1].HeaderText = "Տիգրան Eldex";
            dgvimpornents.Columns[2].HeaderText = "Levadent";
            dgvimpornents.Columns[3].HeaderText = "Soco";
            dgvimpornents.Columns[4].HeaderText = "Armdental";
            dgvimpornents.Columns[5].HeaderText = "Dentax";
            dgvimpornents.Columns[6].HeaderText = "S/ZPharma";
            dgvimpornents.Columns[8].HeaderText = "Nardentax";
            dgvimpornents.Columns[9].HeaderText = "Հակյկազ";
            dgvimpornents.Columns[10].HeaderText = "Անձեռոցիկ";
            dgvimpornents.Columns[11].HeaderText = "Tokuyama Իննա";
            dgvimpornents.Columns[12].HeaderText = "Movses";
            dgvimpornents.Columns[13].HeaderText = "Ամսաթիվ";

            dgvimpornents.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
          
        }

        private void FromWorkSpace_Load(object sender, EventArgs e)
        {

            string[] importersName = 
            { "Տիգրան Eldex",
               "Levadent",
               "Soco",
               "Armdental",
               "Dentax",
               "S/ZPharma",
               "Nardentax",
               "Հակյկազ",
               "Անձեռոցիկ",
               "Tokuyama Իննա",
               "Movses"
            };

            List<string> imporetrs = new List<string>();
            imporetrs.AddRange(importersName);
            cmbImporters.Items.AddRange(imporetrs.ToArray());
        }

        private void btnImportersAddSum_Click(object sender, EventArgs e)
        {
            if (cmbImporters.SelectedItem == null)
            {
                M.OKCencel(MessegesTyp.OKCenc,"Առաքիչը նշված չէ");
                return;
            }


            for (int i = 0; i < dgvimpornents.ColumnCount; i++)
            {
            
                if (cmbImporters.SelectedItem.ToString()== dgvimpornents.Columns[i].HeaderText)
                {
                    

                    SqlQuery.SaveImport( dgvimpornents.Columns[i].Name, int.Parse(txtSumImporters.Text),dateimporters.Value);

                    SqlQuery.ShowDatagrid(dgvimpornents,sql);
                }
            }
            
        }

        private void OpenForm(object obj)
        {
            Application.Run(new FormClients());
        }
        public void OpenAndClose()
        {
            this.Close();
            thread = new Thread(OpenForm);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            int id= (int)dgvimpornents.SelectedRows[0].Cells["Id"].Value;
            for (int i = 0; i < dgvimpornents.ColumnCount; i++)
            {

                if (cmbImporters.SelectedItem.ToString() == dgvimpornents.Columns[i].HeaderText)
                {
                    
                    
                    SqlQuery.Update(tableName,dgvimpornents.Columns[i].Name,"Time", int.Parse(txtSumImporters.Text), dateimporters.Value,id);

                    SqlQuery.ShowDatagrid(dgvimpornents,sql);
                }
            }
        }

        private void btnImportersDeleteSum_Click(object sender, EventArgs e)
        {
            int id = (int)dgvimpornents.SelectedRows[0].Cells["Id"].Value;
                    
            
            SqlQuery.Delete(tableName, id);
            SqlQuery.ShowDatagrid(dgvimpornents,sql);
        }

        private void btnSum_Click(object sender, EventArgs e)
        {
            int id = (int)dgvimpornents.SelectedRows[0].Cells["Id"].Value;
            for (int i = 0; i < dgvimpornents.ColumnCount; i++)
            {
                if (cmbImporters.SelectedItem == null)
                {
                    M.OKCencel(MessegesTyp.OKCenc, "Նյութական միջոցի տեսակը ընտրված չէ");
                    return;
                }

                if (cmbImporters.SelectedItem.ToString() == dgvimpornents.Columns[i].HeaderText)
                {

                    
                    txtSum.Text = SqlQuery.SelectSum(tableName, dgvimpornents.Columns[i].Name,"Time",  dateimporters.Value).ToString();
                    
                    SqlQuery.ShowDatagrid( dgvimpornents,sql);
                }
            }
        }
        public void SqlSelect(string sql)
        {
            //DataSet sDs;
            //DataTable sTable;

            //SqlConnection connection = new SqlConnection(connectionString);
            //connection.Open();
            //sCommand = new SqlCommand(sql, connection);
            //sAdapter = new SqlDataAdapter(sCommand);
            //sBuilder = new SqlCommandBuilder(sAdapter);
            //sDs = new DataSet();
            //sAdapter.Fill(sDs, "Imprtno");
            //sTable = sDs.Tables["Imprtno"];
            //connection.Close();
            //dgvimpornents.DataSource = sDs.Tables["Imprtno"];
            
        }
    } 
}
