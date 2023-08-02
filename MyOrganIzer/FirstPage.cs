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
        Dictionary<int, object> empl = new Dictionary<int, object>();
        private bool dragg = false;
        private Point startPoint = new Point(0, 0);
        public FirstPage()
        {
            InitializeComponent();
            timer1.Start();
            var tmer = Client.SalectDatduoblejoin();
            
            foreach (var item in tmer)
            {
                empl.Add((int)((object[])item)[0], ((object[])item)[1]);
                
            }
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
        string name;
        string lastName;
        string houer;
        string minuts;
        List<string> messge = new List<string>();
        bool tik=true;
        bool signal = false;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (signal)
            {
                Signal();
            }
           

            foreach (object item in empl)
            {
                
                if (!tik)
                    return;
                if (((DateTime)((KeyValuePair<int, object>)item).Value).Day == DateTime.Now.Day)
                {
                    
                    
                    var Name= SqlQuery.SqlSelect($"select Name,LastName from Answers where id ={((KeyValuePair<int, object>)item).Key} and DateJoinString <>'' ", "Answers");
                    if (Name.Count!=0)
                    {
                        signal = true;
                        btnmesseje.Visible = true;
                        foreach (var na in Name.ToArray())
                        {
                            name = na[0].ToString();
                            lastName = na[1].ToString();
                        }
                        
                        houer = ((DateTime)((KeyValuePair<int, object>)item).Value).Hour.ToString();
                        minuts = ((DateTime)((KeyValuePair<int, object>)item).Value).Minute.ToString();
                        string mess = $"{name + "  " + lastName}" + $"-ն {houer}:{minuts}-ին  այց";

                        messge.Add(mess);
                    }
                 
                }
            }
            tik = false;   
            
        }
        public void Signal()
        {
            if (DateTime.Now.Second % 2 != 0)
                btnmesseje.BackColor = Color.OrangeRed;
            else
                btnmesseje.BackColor = Color.Wheat;
        }
        private void btnmesseje_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            var message = string.Join(Environment.NewLine, messge);
            btnmesseje.Image = System.Drawing.Image.FromFile(@"C:\Users\User\Pictures\gev\Messaging-Read-Message-icon (1).png");
            M.OKCencel(MessegesTyp.OKCenc, message);
            
            btnmesseje.Visible = false;
        }
    }
}
