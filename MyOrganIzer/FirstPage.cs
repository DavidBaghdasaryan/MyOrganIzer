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
    public partial class FirstPage : Form
    {

        private bool dragg = false;
        private Point startPoint = new Point(0, 0);
        public FirstPage()
        {
            InitializeComponent();
        }

        

        private void btnPcainet_Click_1(object sender, EventArgs e)
        {
            FormClients formClient = new FormClients();
            formClient.ShowDialog();
        }

        

        private void btnSenders_Click(object sender, EventArgs e)
        {
            FromWorkSpace workSpace = new FromWorkSpace();
          
            workSpace.ShowDialog();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            this.WindowState =FormWindowState.Minimized;
        }

        private void btnTexniqs_Click(object sender, EventArgs e)
        {
            FormTecinks formTecinks = new FormTecinks();
            formTecinks.ShowDialog();
        }

        private void FirstPage_MouseDown(object sender, MouseEventArgs e)
        {
            dragg = true;
            startPoint = new Point(e.X, e.Y);
        }

        private void FirstPage_MouseUp(object sender, MouseEventArgs e)
        {
            dragg = false;
        }

        private void FirstPage_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragg)
            {
                Point p = PointToScreen(e.Location);
                Location = new Point(p.X - this.startPoint.X, p.Y - this.startPoint.Y);
            }
        }

        private void FirstPage_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
        }
    }
}
