using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyOrganIzer
{
    public partial class FormTecinks : Form
    {
        Thread thread;
        string connectionString = "server=developer.000.am,1433\\MERSOFT11;database=Ayzenq;user=dev;password=Developer1*";
        string sql = "SELECT * FROM Tecno";
        SqlCommand sCommand;
        SqlDataAdapter sAdapter;
        SqlCommandBuilder sBuilder;
        string tableName = "Tecno";
        public FormTecinks()
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
            sAdapter.Fill(sDs, "Tecno");
            sTable = sDs.Tables["Tecno"];
            connection.Close();
            dgvTecnics.DataSource = sDs.Tables["Tecno"];
            dgvTecnics.Columns["ID"].Visible = false;
            dgvTecnics.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgvTecnics.Columns[1].HeaderText = "Ա/Ա/Հ";
            dgvTecnics.Columns[2].HeaderText = "implant m/k";
            dgvTecnics.Columns[3].HeaderText = "m/k";
            dgvTecnics.Columns[4].HeaderText = "cr";
            dgvTecnics.Columns[5].HeaderText = "byuget";
            dgvTecnics.Columns[6].HeaderText = "protez";
            dgvTecnics.Columns[7].HeaderText = "M & S abatments";
            dgvTecnics.Columns[8].HeaderText = "Gog";
            dgvTecnics.Columns[9].HeaderText = "Առաքման օր";
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

        private void btnTecnicsAddSum_Click(object sender, EventArgs e)
        {
            if (cmbTecnics.SelectedItem == null)
            {
                MessageBox.Show("Տեխնիկը նշված չէ");
                return;
            }

            for (int i = 0; i < dgvTecnics.ColumnCount; i++)
            {

                if (cmbTecnics.SelectedItem.ToString() == dgvTecnics.Columns[i].HeaderText)
                {
                    SqlQuery.SaveTecnic(txtTechnoName.Text, dgvTecnics.Columns[i].Name, int.Parse(txtPriceTecnics.Text), dateTecincs.Value);

                    SqlQuery.ShowDatagrid(dgvTecnics,sql);
                }
            }
        }

        private void btnTecnicsEdit_Click(object sender, EventArgs e)
        {
            int id = (int)dgvTecnics.SelectedRows[0].Cells["Id"].Value;
            for (int i = 0; i < dgvTecnics.ColumnCount; i++)
            {

                if (cmbTecnics.SelectedItem.ToString() == dgvTecnics.Columns[i].HeaderText)
                {
                    SqlQuery.Update(tableName, dgvTecnics.Columns[i].Name,"Time",int.Parse(txtPriceTecnics.Text), dateTecincs.Value, id);

                   SqlQuery.ShowDatagrid(dgvTecnics,sql);
                }
            }
        }

        private void btnTecnicsDeleteSum_Click(object sender, EventArgs e)
        {
            int id = (int)dgvTecnics.SelectedRows[0].Cells["Id"].Value;

            
            SqlQuery.Delete(tableName, id);
            SqlQuery.ShowDatagrid(dgvTecnics,sql);
        }

        private void btnSum_Click_1(object sender, EventArgs e)
        {
            int id = (int)dgvTecnics.SelectedRows[0].Cells["Id"].Value;
            for (int i = 0; i < dgvTecnics.ColumnCount; i++)
            {

                if (cmbTecnics.SelectedItem.ToString() == dgvTecnics.Columns[i].HeaderText)
                {
                    txtSumTecnics.Text = SqlQuery.SelectSum(tableName, dgvTecnics.Columns[i].Name,"Time", dateTecincs.Value).ToString();

                    SqlQuery.ShowDatagrid(dgvTecnics,sql);
                }
            }
        }

        private void btnClose_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }

        private void FormTecinks_Load(object sender, EventArgs e)
        {
            string[] importersName = { "implant m/k", "m/k", "cr", "byuget", "jmk", "protez", "M & S abatments", "Gog" };

            List<string> imporetrs = new List<string>();
            imporetrs.AddRange(importersName);
            cmbTecnics.Items.AddRange(imporetrs.ToArray());
        }

        private void dgvTecnics_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            
        }
    }
}

