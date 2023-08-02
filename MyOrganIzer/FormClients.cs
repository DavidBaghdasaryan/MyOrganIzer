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
    public partial class FormClients : Form
    {
        Thread thread;
        Client clientedit =new Client ();
        string connectionString = "server=(LocalDb)\\MSSQLLocalDB;database=Ayzenq";
        string sql = "select  * from Answers";
        SqlCommand sCommand;
        SqlDataAdapter sAdapter;
        SqlCommandBuilder sBuilder;

        
       
        public FormClients()
        {
            InitializeComponent();
            SqlSelect(sql);
            dgvClients.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvClients.Columns[0].Visible = false;
            dgvClients.Columns[1].HeaderText = "Անուն";
            dgvClients.Columns[2].HeaderText = "Ազգաուն";
            dgvClients.Columns[3].HeaderText = "Հայրանուն";
            dgvClients.Columns[4].HeaderText = "Գրանցման օր";
            dgvClients.Columns[4].DefaultCellStyle.Format = "MM-dd-yyyy HH:mm";
            dgvClients.Columns[5].HeaderText = "Վճարում";
            dgvClients.Columns[6].HeaderText = "Մնացորդ";
            dgvClients.Columns[7].HeaderText = "Հեռախոսահամար";
            dgvClients.Columns[8].Visible=false;
            dgvClients.Columns[9].HeaderText = "Նախատեսված է կրկնակի այց";
            dgvClients.Columns[9].DefaultCellStyle.Format = "MM-dd-yyyy HH:mm";
        }


        private void FormClients_Load(object sender, EventArgs e)
        {
            clientedit.ShowDatagrid(dgvClients);

            string[] importersName = { "Անուն", "Ազգանուն", "Հայրանուն", "Գրանցման օր", "Վճարում", "Մնացորդ", "Հեռախոսահամար" };
            List<string> imporetrs = new List<string>();
            imporetrs.AddRange(importersName);
            cmbFind.Items.AddRange(imporetrs.ToArray());
        }

        private void btnSave1_Click(object sender, EventArgs e)
        {
            OpenAndClose();
        }

        private void btrDelete_Click(object sender, EventArgs e)
        {
            M.Show("Ցանկանու՞մ եք հեռացնել նշված տողը");
            
            if (MyMessageBox.No==true)
            {
                return;
            }
            clientedit.Id = (int)dgvClients.SelectedRows[0].Cells["Id"].Value;
            clientedit.Delete();
            clientedit = new Client();
            clientedit.ShowDatagrid(dgvClients);
          
        }

        private void btnEdit1_Click(object sender, EventArgs e)
        {
            clientedit.Id = (int)dgvClients.SelectedRows[0].Cells["Id"].Value;
            Client.edit = true;
            clientedit.FirstName = dgvClients.SelectedRows[0].Cells["Name"]?.Value.ToString();
            clientedit.LastName = dgvClients.SelectedRows[0].Cells["LastName"]?.Value.ToString();
            clientedit.MidlName = dgvClients.SelectedRows[0].Cells["MidlName"]?.Value.ToString();
            clientedit.DateJoin = (DateTime)dgvClients.SelectedRows[0].Cells["DateJoin"]?.Value;
            clientedit.Price = (decimal)dgvClients.SelectedRows[0].Cells["Price"]?.Value;
            clientedit.Debet = (decimal)dgvClients.SelectedRows[0].Cells["Debt"]?.Value;
            clientedit.PhoneNumber = dgvClients.SelectedRows[0].Cells["PhoneNumbers"]?.Value.ToString();

            OpenAndClose();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OpenForm(object obj)
        {
            Application.Run(new FormClientPropertis() { ClientNew= clientedit});
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
            DataSet sDs;
            DataTable sTable;
            
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            sCommand = new SqlCommand(sql, connection);
            sAdapter = new SqlDataAdapter(sCommand);
            sBuilder = new SqlCommandBuilder(sAdapter);
            sDs = new DataSet();
            sAdapter.Fill(sDs, "Answers");
            sTable = sDs.Tables["Answers"];
            connection.Close();
            if(dgvClients.Columns.Count>0)
            dgvClients.Columns[0].Visible = false;
            dgvClients.DataSource = sDs.Tables["Answers"];
     
        }

        private void criculButton1_Click(object sender, EventArgs e)
        {

            var Sum = SqlQuery.SqlSelect($"SELECT * FROM Answers where Debt > 0 and  MONTH(DateJoin) = '{datemounth.Value.Month}'", "Answers");
            foreach (var item in Sum)
            {
                txtSum.Text = item[0].ToString();
            }
        }

        private void criculButton2_Click(object sender, EventArgs e)
        {
           var Sum= SqlQuery.SqlSelect($"select SUM(PRICE) from Answers WHERE MONTH(DateJoin) = '{datemounth.Value.Month}' ", "Answers");
            foreach (var item in Sum)
            {
                txtSum.Text = item[0].ToString();
            }
            
        }

        private void btnFind_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < dgvClients.ColumnCount; i++)
            {
                if (chPrice.Checked)
                {
                    SqlSelect($"select * From Answers WHERE  PRICE<>0 And MONTH(DateJoin) = '{datemounth.Value.Month}'");
                    return;
                }
                if (chbDebt.Checked)
                {
                    SqlSelect($"select * From Answers WHERE  Debt<>0 And MONTH(DateJoin) = '{datemounth.Value.Month}'");
                    return;
                }
                if (cmbFind.SelectedItem==null)
                {
                    SqlSelect($"select * From Answers WHERE [{dgvClients.Columns[i].Name}] Like (N'%{txtFind.Text}%') And MONTH(DateJoin) = '{datemounth.Value.Month}'");
                }
                else
                if (cmbFind.SelectedItem.ToString() == dgvClients.Columns[i].HeaderText)
                {
                    SqlSelect($"select * From Answers WHERE [{dgvClients.Columns[i].Name}] Like (N'%{txtFind.Text}%') And MONTH(DateJoin) = '{datemounth.Value.Month}'");
                }
            }

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            clientedit.ShowDatagrid(dgvClients);
            dgvClients.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvClients.Columns[0].Visible = false;
            dgvClients.Columns[1].HeaderText = "Անուն";
            dgvClients.Columns[2].HeaderText = "Ազգաուն";
            dgvClients.Columns[3].HeaderText = "Հայրանուն";
            dgvClients.Columns[4].HeaderText = "Գրանցման օր";
            dgvClients.Columns[5].HeaderText = "Վճարում";
            dgvClients.Columns[6].HeaderText = "Մնացորդ";
            dgvClients.Columns[7].HeaderText = "Հեռախոսահամար";
            dgvClients.Columns[8].Visible=false;
            dgvClients.Columns[9].HeaderText = "Նախատեսված է կրկնակի այց";
        }

        private void chPrice_CheckedChanged(object sender, EventArgs e)
        {
            chbDebt.Checked = false;
        }

        private void chbDebt_CheckedChanged(object sender, EventArgs e)
        {
            chPrice.Checked = false;
        }
    }
}
