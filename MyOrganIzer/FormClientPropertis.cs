using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace MyOrganIzer
{
    public partial class FormClientPropertis : Form
    {
        Thread thread;
       public Client ClientNew;
        public FormClientPropertis()
        {
            InitializeComponent();
        }
     

        private void FormClientPropertis_Load(object sender, EventArgs e)
        {
            if (ClientNew.Id!=0)
            {
                txtName.Text = ClientNew.FirstName;
                txtLastName.Text = ClientNew.LastName;
                txtMidlName.Text = ClientNew.MidlName;
                txtPrice.Text = ClientNew.Price.ToString();
                txtdebt.Text = ClientNew.Debet.ToString();
                datejoin.Value = ClientNew.DateJoin;
                txtPhoneNumber.Text = ClientNew.PhoneNumber.ToString();
                
            }
        }
        public void ClinetSave()
        {
            if (!Client.edit)
            {
                ClientNew = new Client();
            }
            ClientNew.FirstName = txtName.Text;
            ClientNew.LastName = txtLastName.Text;
            ClientNew.MidlName = txtMidlName.Text;
            ClientNew.DateJoin = datejoin.Value;
            ClientNew.Price = decimal.Parse(txtPrice.Text);
            ClientNew.Debet = decimal.Parse(txtdebt.Text);
            ClientNew.PhoneNumber = txtPhoneNumber.Text;
            ClientNew.DateDobleJoin =datejoindouble.Value;
            if(chbDouble.Checked)
            ClientNew.DateJoinString = datejoindouble.Text;
            ClientNew.SaveOrUpdaet();
            Client.edit = false;
        }
        private void btnSave1_Click(object sender, EventArgs e)
        {
            ClinetSave();
            if (ClientNew.Id == 0)
            {
                ClientNew.Id = SqlQuery.Id();
                SqlQuery.CackingToot(ClientNew.Id);

            }
            
            OpenAndClose();
        }

        private void btrDelete_Click(object sender, EventArgs e)
        {
            txtName.Clear();
            txtLastName.Clear();
            txtMidlName.Clear();
            txtPrice.Clear();
            txtdebt.Clear();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            OpenAndClose();
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

        private void btnDubladd_Click(object sender, EventArgs e)
        {
            Client.edit = true;
            ClientNew.FirstName = txtName.Text;
            ClientNew.LastName = txtLastName.Text;
            ClientNew.MidlName = txtMidlName.Text;
            ClientNew.DateJoin = datejoin.Value;
            ClientNew.Price = decimal.Parse(txtPrice.Text);
            ClientNew.Debet = decimal.Parse(txtdebt.Text);
            ClientNew.PhoneNumber = txtPhoneNumber.Text;
            ClientNew.DateDobleJoin = datejoindouble.Value;
            if (chbDouble.Checked)
                ClientNew.DateJoinString = datejoindouble.Text;
            ClientNew.SaveOrUpdaet();
        }
        public bool chacing()
        {
            if (string.IsNullOrEmpty(txtName.Text) || string.IsNullOrEmpty(txtLastName.Text) || string.IsNullOrEmpty(txtMidlName.Text))
            {
                M.OKCencel(MessegesTyp.OKCenc, "Հաճախորդի տվյալները լրացված չեն");
                return false;
            }
            else
                return true;
        }
        private void btnWork_Click(object sender, EventArgs e)
        {
            if (!chacing())
            {
                return; 
            }
            if (ClientNew.Id==0)
            {
                Client.edit = false;
                ClinetSave(); 
            }
            
            FormTooth tooth = new FormTooth(ClientNew);
            tooth.client = ClientNew;
            tooth.ShowDialog();
        }

        private void chbDouble_CheckedChanged(object sender, EventArgs e)
        {
            if (chbDouble.Checked)
            {
                datejoindouble.Visible = true;
                //datejoindouble.CustomFormat = "dd-MM-yyyy HH:mm:ss";
            }
            else
            {
                datejoindouble.Visible = false;
                datejoindouble.Text = "";
            }
            
        }

        private void datejoindouble_ValueChanged(object sender, EventArgs e)
        {
            datejoindouble.CustomFormat = "dd-MM-yyyy HH:mm:ss";
        }

        private void datejoin_ValueChanged(object sender, EventArgs e)
        {
            datejoin.CustomFormat = "dd-MM-yyyy HH:mm:ss";
        }
    }
}
