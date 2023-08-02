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
    public partial class FormLogin :Form
    {
        Thread thread;
        public FormLogin()
        {
            InitializeComponent();
            txtLogin.PasswordChar = '*';

        }
        private void OpenForm(object obj)
        {
            Application.Run(new FirstPage());
        }
        public void OpenAndClose()
        {
            this.Close();
            thread = new Thread(OpenForm);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        
        private void btnEnter_Click_1(object sender, EventArgs e)
        {
            if (txtLogin.Text == "1234")
            {
                OpenAndClose();
            }
            else
            {
                M.OKCencel(MessegesTyp.OKCenc, "Սխալ գաղտանաբառ");
                txtLogin.Text = string.Empty;
                return;
            }
        }

        private void txtLogin_Click(object sender, EventArgs e)
        {
            txtLogin.Text = string.Empty;
           
        }

        

        private void txtLogin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnEnter_Click_1(sender, e);
            }
        }
    }
}
