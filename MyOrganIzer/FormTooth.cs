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
    public partial class FormTooth : Form
    {
        Thread thread;
    
        string connectionString = "server=(LocalDb)\\MSSQLLocalDB;database=Ayzenq";
        string sql = $"select  * from ABC ";
        string sql2 = $"";
        SqlCommand sCommand;
        SqlCommand sCommand2;
        SqlDataAdapter sAdapter;
        SqlDataAdapter sAdapter2;
        SqlCommandBuilder sBuilder;
        SqlCommandBuilder sBuilder2;
       public Client client;

        string[] byugel=new string[] {"250000","230000","200000" };
        string[] protez=new string[] {"70000","65000","60000" };
        string[] implzr=new string[] {"90000","85000","80000" };
        string[] implmk= new string[] { "70000", "65000", "60000" };
        string[] zrkemax= new string[] { "80000", "78000", "75000" };
        string[] mk30=new string[] {"35000","30000","25000" };
        string[] rest=  new string[] {"20000","18000","15000" };
        string[] plomb= new string[] {"15000","13000","10000" };
        string[] shift= new string[] {"5000","4000","3000" };
        string[] endo = new string[] {"7000","6000","5000" };
       
        Tooth tooth = new Tooth();
        public FormTooth(Client _client)
        {
            client = _client;
            InitializeComponent();
            DataSet sDs;
            DataSet sDs2;
            DataTable sTable;
            DataTable sTable2;
             if (client!=null)
            {
                if (client.Id == 0)
                {
                    client.Id = SqlQuery.Id();
                    SqlQuery.CackingToot(client.Id);
                    
                }
                sql = $"select  * from ABC where epmlid={client.Id}";
                sql2 = $"select sum(Byugel),sum(Protez),sum([impl/mk]),sum([Impl/zr]),sum([zr/k,emax]),sum(mk30),sum(rest),sum(plomb),sum(shift),sum(endo) from Emploit where EmploeId={client.Id}";
            }
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            sCommand = new SqlCommand(sql, connection);
            sCommand2 = new SqlCommand(sql2, connection);
            sAdapter = new SqlDataAdapter(sCommand);
            sAdapter2 = new SqlDataAdapter(sCommand2);
            sBuilder = new SqlCommandBuilder(sAdapter);
            sBuilder2 = new SqlCommandBuilder(sAdapter2);
            sDs = new DataSet();
            sAdapter.Fill(sDs, "ABC");
            sTable = sDs.Tables["ABC"];
            sDs2 = new DataSet();
            sAdapter2.Fill(sDs2, "ABC");
            sTable2 = sDs2.Tables["ABC"];
            connection.Close();
            SumAll();
            //SqlQuery.EditToot();

            dgvToots.DataSource = sDs.Tables["ABC"];
            if (dgvToots.Columns.Count > 0)
            {
                dgvToots.Columns[0].Visible = false;
                dgvToots.Columns[1].Visible = false;
            }
            dgvTootsSum.DataSource = sDs2.Tables["ABC"];
            
            dgvTootsSum.AutoResizeColumns();
            dgvTootsSum.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvTootsSum.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgvTootsSum.Columns[0].HeaderText = "Բյուգել";
            dgvTootsSum.Columns[1].HeaderText = "Պրոթեզ";
            dgvTootsSum.Columns[2].HeaderText = "Ի/Մ/Կ";
            dgvTootsSum.Columns[3].HeaderText = "Ի/Ց/Կ";
            dgvTootsSum.Columns[4].HeaderText = "Պ/կ";
            dgvTootsSum.Columns[5].HeaderText = "Մ/Կ";
            dgvTootsSum.Columns[6].HeaderText = "Պսակի վկգ․ում";
            dgvTootsSum.Columns[7].HeaderText = "Ատամնալիցք";
            dgvTootsSum.Columns[8].HeaderText = "Գամիկ";
            dgvTootsSum.Columns[9].HeaderText = "Արմատալիցք";
            //this.dgvToots.EditMode = DataGridViewEditMode.EditOnEnter;

            dgvToots.AutoResizeColumns();
            dgvToots.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgvToots.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            dgvToots.Columns[1].HeaderText = "";
            dgvToots.Columns[2].HeaderText = "";
            dgvToots.Columns[3].HeaderText = "";
            dgvToots.Columns[4].HeaderText = "";
            dgvToots.Columns[5].HeaderText = "";
            dgvToots.Columns[6].HeaderText = "";
            dgvToots.Columns[7].HeaderText = "";
            dgvToots.Columns[8].HeaderText = "";
            dgvToots.Columns[9].HeaderText = "";
            dgvToots.Columns[10].HeaderText = "";
            dgvToots.Columns[11].HeaderText = "";
            dgvToots.Columns[12].HeaderText = "";
            dgvToots.Columns[13].HeaderText = "";
            dgvToots.Columns[14].HeaderText = "";
            dgvToots.Columns[15].HeaderText = "";
            dgvToots.Columns[16].HeaderText = "";
            dgvToots.Columns[17].HeaderText = "";
            dgvToots.Columns[18].Visible = false;
        }



        public bool Add { get; set; } = true;
        private void tsmAdd_Click(object sender, EventArgs e)
        {
            if (toolcmbAddByugel.SelectedItem != null)
            {
                tooth.byugel = int.Parse(toolcmbAddByugel.SelectedItem?.ToString());
                if(Up)
                    SqlQuery.UpdateToots(tooth.TootNumber, client.Id, tooth.byugel, "Byugel.");
                else
                SqlQuery.UpdateToots(tooth.TootNumber, client.Id, tooth.byugel, "Byugel");
               
            }
            if (toolcmbAddProtez.SelectedItem != null) {
                tooth.protez = int.Parse(toolcmbAddProtez.SelectedItem?.ToString());
                if(Up)
                    SqlQuery.UpdateToots(tooth.TootNumber, client.Id, tooth.protez, "protez.");
                else
                SqlQuery.UpdateToots(tooth.TootNumber, client.Id, tooth.protez, "protez");
                
            }
            if (toolcmbAddImplzr.SelectedItem != null)
            {
                tooth.implantzr = int.Parse(toolcmbAddImplzr.SelectedItem?.ToString());
                if(Up)
                    SqlQuery.UpdateToots(tooth.TootNumber, client.Id, tooth.implantzr, "[Impl/zr].");
                else
                SqlQuery.UpdateToots(tooth.TootNumber, client.Id, tooth.implantzr, "[Impl/zr]");
               
            }
            if (toolcmbAddImplMk.SelectedItem != null)
            {
                tooth.implmk = int.Parse(toolcmbAddImplMk.SelectedItem?.ToString());
                if(Up)
                    SqlQuery.UpdateToots(tooth.TootNumber, client.Id, tooth.implmk, "[impl/mk].");
                else
                SqlQuery.UpdateToots(tooth.TootNumber, client.Id, tooth.implmk, "[impl/mk]");
                
            }
            if (toolcmbAddMk30.SelectedItem != null)
            {
                tooth.mk30 = int.Parse(toolcmbAddMk30.SelectedItem?.ToString());
                if(Up)
                    SqlQuery.UpdateToots(tooth.TootNumber, client.Id, tooth.mk30, "mk30.");
                else
                SqlQuery.UpdateToots(tooth.TootNumber, client.Id, tooth.mk30, "mk30");
              
            }

            if (toolcmbAddRest.SelectedItem != null)
            {
                tooth.rest = int.Parse(toolcmbAddRest.SelectedItem?.ToString());
                if(Up)
                    SqlQuery.UpdateToots(tooth.TootNumber, client.Id, tooth.rest, "rest.");
                else
                SqlQuery.UpdateToots(tooth.TootNumber, client.Id, tooth.rest, "rest");
                
            }
            if (toolcmbAddPlomb.SelectedItem != null)
            {
                tooth.plomb = int.Parse(toolcmbAddPlomb.SelectedItem?.ToString());
                if(Up)
                    SqlQuery.UpdateToots(tooth.TootNumber, client.Id, tooth.plomb, "plomb.");
                else
                SqlQuery.UpdateToots(tooth.TootNumber, client.Id, tooth.plomb, "plomb");
            
            }
            if (toolcmbAddShift.SelectedItem != null)
            {
                tooth.shift = int.Parse(toolcmbAddShift.SelectedItem?.ToString());
                if(Up)
                    SqlQuery.UpdateToots(tooth.TootNumber, client.Id, tooth.shift, "shift.");
                else
                SqlQuery.UpdateToots(tooth.TootNumber, client.Id, tooth.shift, "shift");
                
            }
            if (toolcmbAddEdno.SelectedItem != null)
            {
                tooth.endo = int.Parse(toolcmbAddEdno.SelectedItem?.ToString());
                if(Up)
                    SqlQuery.UpdateToots(tooth.TootNumber, client.Id, tooth.endo, "endo.");
                else
                SqlQuery.UpdateToots(tooth.TootNumber, client.Id, tooth.endo, "endo");
             
            }
            tooth.EmployeId = client.Id;
            tooth.Up = Up;
           
            tooth.Save(Add);
            Add= Client.edit;
            SqlQuery.ShowDatagrid(dgvToots, sql);
            SqlQuery.ShowDatagrid(dgvTootsSum, sql2);
            Updatechck();
            tooth.byugel = 0;
            tooth.protez = 0;
            tooth.implantzr = 0;
            tooth.implmk = 0;
            tooth.mk30 = 0;
            tooth.rest = 0;
            tooth.plomb = 0;
            tooth.shift = 0;
            tooth.endo = 0;
            SumAll();
        }

        
       public void SumAll()
        {
            var t = SqlQuery.SqlSelect($"select sum(Byugel)+sum(Protez)+sum([impl/mk])+sum([Impl/zr])" +
                $"+sum([zr/k,emax])+sum(mk30)+sum(rest)+sum(plomb)+sum(shift)" +
                $"+sum(endo) from Emploit where EmploeId={client.Id}", "Emploit");
            foreach (var item in t)
            {
                txtAllSum.Text = item[0].ToString();

            }
        }
        
        public void Updatechck()
        {
            toolcmbAddByugel.Items.Clear();
            toolcmbAddByugel.Text = "Բյուգել";
            toolcmbAddEdno.Items.Clear();
            toolcmbAddEdno.Text = "Ատամնալիցք";
            toolcmbAddImplMk.Items.Clear();
            toolcmbAddImplMk.Text = "Ի/Մ/Կ";
            toolcmbAddImplzr.Items.Clear();
            toolcmbAddImplzr.Text = "Ի/Ց/Կ";
            toolcmbAddMk30.Items.Clear();
            toolcmbAddMk30.Text = "Մ/Կ";
            toolcmbAddPlomb.Items.Clear();
            toolcmbAddPlomb.Text = "Ատամնալիցք";
            toolcmbAddProtez.Items.Clear();
            toolcmbAddProtez.Text = "Պրոթեզ";
            toolcmbAddRest.Items.Clear();
            toolcmbAddRest.Text = "Պսակի վկգ․ում";
            toolcmbAddShift.Items.Clear();
            toolcmbAddShift.Text = "Գամիկ";
            toolcmbAddZrkEmax.Items.Clear();
            toolcmbAddZrkEmax.Text = "Պ/Կ";
           
            toolcmbAddByugel.Items.AddRange(byugel?.ToArray());
            toolcmbAddEdno.Items.AddRange(endo?.ToArray());
            toolcmbAddImplMk.Items.AddRange(implmk?.ToArray());
            toolcmbAddImplzr.Items.AddRange(implzr?.ToArray());
            toolcmbAddMk30.Items.AddRange(mk30?.ToArray());
            toolcmbAddPlomb.Items.AddRange(plomb?.ToArray());
            toolcmbAddProtez.Items.AddRange(protez?.ToArray());
            toolcmbAddRest.Items.AddRange(rest?.ToArray());
            toolcmbAddShift.Items.AddRange(shift?.ToArray());
            toolcmbAddZrkEmax.Items.AddRange(zrkemax?.ToArray());
        }
        private void FormTooth_Load(object sender, EventArgs e)
        {
            //if (client.Id==0)
            //{
            //    client.Id = SqlQuery.Id();
            //    SqlQuery.CackingToot(client.Id);
            //}
           
            SqlQuery.LoadToot(client.Id);

            SqlQuery.ShowDatagrid(dgvToots, sql);
            Updatechck();
        }

        private void BtnPrice_Click(object sender, EventArgs e)
        {
            byugel=new string[]{ txtbyugel.Text, txtbyugel2.Text, txtbyugel3.Text};
            protez = new string[] {txtprotez1.Text,txtprotez2.Text,txtprotez3.Text };
            implzr = new string[] { txtimplzk1.Text, txtimplzk2.Text, txtimplzk3.Text };
            implmk = new string[] { txtimplmk1.Text, txtimplmk2.Text, txtimplmk3.Text };
            zrkemax = new string[] {txtzrk1.Text,txtzrk2.Text,txtzrk3.Text };
            mk30 = new string[] {txtmk301.Text,txtmk302.Text,txtmk303.Text };
            rest = new string[] {txtrest1.Text,txtrest2.Text,txtrest3.Text };
            plomb = new string[] {txtPlomb1.Text,txtPlomb2.Text,txtPlomb3.Text };
            shift = new string[] {txtshift1.Text,txtshift2.Text,txtshift3.Text };
            endo = new string[] {txtendo1.Text,txtendo2.Text,txtendo3.Text };
            Updatechck();
        }


        public bool Up { get; set; }
        private void btnTu1_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "B";
            Up = false;
        }
        private void btnTu2_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "C";
            Up = false;
        }
        

        private void btnTu3_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "D";
            Up = false;
        }

        private void btnTu4_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "E";
            Up = false;
        }

        private void btnTu5_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "F";
            Up = false;
        }

        private void btnTu6_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "G";
            Up = false;
        }

        private void btnTu7_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "H";
            Up = false;
        }

        private void btnTu8_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "I";
            Up = false;
        }

        private void btnTu9_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "J";
            Up = false;
        }

        private void btnTu10_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "K";
            Up = false;
        }

        private void btnTu11_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "L";
            Up = false;
        }

        private void btnTu12_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "M";
            Up = false;
        }

        private void btnTu13_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "N";
            Up = false;
        }

        private void btnTu14_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "O";
            Up = false;
        }

        private void btnTu15_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "P";
            Up = false;
        }

        private void btnTu16_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "Q";
            Up = false;
        }

        private void btn1d_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "B";
            Up = true;
        }

        private void btn2d_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "C";
            Up = true;
        }

        private void btn3d_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "D";
            Up = true;
        }

        private void brn4d_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "E";
            Up = true;
        }

        private void btn5d_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "F";
            Up = true;
        }

        private void btn6d_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "G";
            Up = true;
        }

        private void btn7d_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "H";
            Up = true;
        }

        private void btn8d_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "I";
            Up = true;
        }

        private void btn9d_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "J";
            Up = true;
        }

        private void btn10d_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "K";
            Up = true;
        }

        private void btn11d_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "L";
            Up = true;
        }

        private void btn12d_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "M";
            Up = true;
        }

        private void btn13d_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "N";
            Up = true;
        }

        private void btn14d_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "O";
            Up = true;
        }

        private void btn15d_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "P";
            Up = true;
        }

        private void btn16d_MouseDown(object sender, MouseEventArgs e)
        {
            tooth.TootNumber = "Q";
            Up = true;
        }
        int p;
        private void label1_Click(object sender, EventArgs e)
        {
            p++;
            txtbyugel.Visible = true;
            txtbyugel2.Visible = true;
            txtbyugel3.Visible = true;
            if (p == 2)
            {
                txtbyugel.Visible = false;
                txtbyugel2.Visible = false;
                txtbyugel3.Visible = false;
                p = 0;
            }
        }
     
        private void label2_Click(object sender, EventArgs e)
        {
            p++;
            txtprotez1.Visible = true;
            txtprotez2.Visible = true;
            txtprotez3.Visible = true;
            if (p == 2)
            {
                txtprotez1.Visible = false;
                txtprotez2.Visible = false;
                txtprotez3.Visible = false;
                p = 0;
            }

        }

        private void label3_Click(object sender, EventArgs e)
        {
            p++;
            txtimplzk1.Visible = true;
            txtimplzk2.Visible = true;
            txtimplzk3.Visible = true;
            if (p == 2)
            {
                txtimplzk1.Visible = false;
                txtimplzk2.Visible = false;
                txtimplzk3.Visible = false;
                p = 0;
            }
        }

        private void label4_Click(object sender, EventArgs e)
        {
            p++;
            txtimplmk1.Visible = true;
            txtimplmk2.Visible = true;
            txtimplmk3.Visible = true;
            if (p == 2)
            {
                txtimplmk1.Visible = false;
                txtimplmk2.Visible = false;
                txtimplmk3.Visible = false;
                p = 0;
            }
        }

        private void label5_Click(object sender, EventArgs e)
        {
            p++;
            txtzrk1.Visible = true;
            txtzrk2.Visible = true;
            txtzrk3.Visible = true;
            if (p == 2)
            {
                txtzrk1.Visible = false;
                txtzrk2.Visible = false;
                txtzrk3.Visible = false;
                p = 0;
            }
        }

        private void label6_Click(object sender, EventArgs e)
        {
            p++;
            txtmk301.Visible = true;
            txtmk302.Visible = true;
            txtmk303.Visible = true;
            if (p == 2)
            {
                txtmk301.Visible = false;
                txtmk302.Visible = false;
                txtmk303.Visible = false;
                p = 0;
            }
        }

        private void label7_Click(object sender, EventArgs e)
        {
            p++;
            txtrest1.Visible = true;
            txtrest2.Visible = true;
            txtrest3.Visible = true;
            if (p == 2)
            {
                txtrest1.Visible = false;
                txtrest2.Visible = false;
                txtrest3.Visible = false;
                p = 0;
            }
        }

        private void label8_Click(object sender, EventArgs e)
        {
            p++;
            txtPlomb1.Visible = true;
            txtPlomb2.Visible = true;
            txtPlomb3.Visible = true;
            if (p == 2)
            {
                txtPlomb1.Visible = false;
                txtPlomb2.Visible = false;
                txtPlomb3.Visible = false;
                p = 0;
            }
        }

        private void label9_Click(object sender, EventArgs e)
        {
            p++;
            txtshift1.Visible = true;
            txtshift2.Visible = true;
            txtshift3.Visible = true;
            if (p == 2)
            {
                txtshift1.Visible = false;
                txtshift2.Visible = false;
                txtshift3.Visible = false;
                p = 0;
            }
        }

        private void label10_Click(object sender, EventArgs e)
        {
            p++;
            txtendo1.Visible = true;
            txtendo2.Visible = true;
            txtendo3.Visible = true;
            if (p == 2)
            {
                txtendo1.Visible = false;
                txtendo2.Visible = false;
                txtendo3.Visible = false;
                p = 0;
            }
        }

        //private void OpenForm(object obj)
        //{
        //    Application.Run(new FormClientPropertis() { ClientNew = client });
        //}
        //public void OpenAndClose()
        //{
        //    this.Close();
        //    thread = new Thread(OpenForm);
        //    thread.SetApartmentState(ApartmentState.STA);
        //    thread.Start();
        //}
        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void dgvToots_EditModeChanged(object sender, EventArgs e)
        {

        }

        private void dgvToots_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            //dgvToots.CurrentCell.Value = new Value.ToString();
        }
    }
}
