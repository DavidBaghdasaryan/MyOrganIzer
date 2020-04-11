using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyOrganIzer
{
    public enum MessegesTyp
    {
        Question,
        OKCenc
    }
    public partial class MyMessageBox : Form
    {

        public static string labelText;
        public static bool Yes;
        public static bool No;

        public MyMessageBox(string text, MessegesTyp meseg )
        {
            InitializeComponent();
            labelText = text;
            
            if (meseg== MessegesTyp.OKCenc)
            {
                btnNo.Visible = false;
                btnYes.Visible = false;
                btnClosOk.Visible = true;
            }
            if(meseg == MessegesTyp.Question)
            {
                btnClosOk.Visible = false;
            }
        }

        private void btnYes_Click(object sender, EventArgs e)
        {
            Yes = true;
            this.Close();
        }

        private void btnNo_Click(object sender, EventArgs e)
        {
            No = true;
            this.Close();
        }

        private void MyMessageBox_Load(object sender, EventArgs e)
        {
            LabelQuestion.Text = labelText;
        }

        private void btnClosOk_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
    public class M
    {
        
        public static void Show(string text)
        {
            MyMessageBox Mesage  =new MyMessageBox(text,MessegesTyp.Question);
            Mesage.ShowDialog();
            MyMessageBox.labelText = text;
        }
        public static void OKCencel(MessegesTyp messeges,string text= "Կատարված է")
        {
            MyMessageBox Mesage = new MyMessageBox(text,MessegesTyp.OKCenc);
            Mesage.ShowDialog();

        }
    }

}
