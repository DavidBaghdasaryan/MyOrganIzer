namespace MyOrganIzer
{
    partial class FormClients
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormClients));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmAdd = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmRemouve = new System.Windows.Forms.ToolStripMenuItem();
            this.dgvClients = new System.Windows.Forms.DataGridView();
            this.datemounth = new System.Windows.Forms.DateTimePicker();
            this.cmbFind = new System.Windows.Forms.ComboBox();
            this.txtFind = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.criculButton2 = new MyOrganIzer.CriculButton();
            this.panel4 = new System.Windows.Forms.Panel();
            this.btnFind = new MyOrganIzer.CriculButton();
            this.txtSum = new System.Windows.Forms.TextBox();
            this.panel3 = new System.Windows.Forms.Panel();
            this.criculButton1 = new MyOrganIzer.CriculButton();
            this.btnExit = new MyOrganIzer.CriculButton();
            this.panel5 = new System.Windows.Forms.Panel();
            this.panel6 = new System.Windows.Forms.Panel();
            this.metroToolTip1 = new MetroFramework.Components.MetroToolTip();
            this.btnEdit1 = new MyOrganIzer.CriculButton();
            this.btrDelete = new MyOrganIzer.CriculButton();
            this.btnSave1 = new MyOrganIzer.CriculButton();
            this.chPrice = new System.Windows.Forms.CheckBox();
            this.chbDebt = new System.Windows.Forms.CheckBox();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvClients)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.panel2.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(50, 40);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmAdd,
            this.tsmEdit,
            this.tsmRemouve});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(167, 142);
            // 
            // tsmAdd
            // 
            this.tsmAdd.BackColor = System.Drawing.Color.MediumTurquoise;
            this.tsmAdd.Image = ((System.Drawing.Image)(resources.GetObject("tsmAdd.Image")));
            this.tsmAdd.ImageTransparentColor = System.Drawing.Color.Blue;
            this.tsmAdd.Name = "tsmAdd";
            this.tsmAdd.Size = new System.Drawing.Size(166, 46);
            this.tsmAdd.Text = "Գրանցել";
            this.tsmAdd.Click += new System.EventHandler(this.btnSave1_Click);
            // 
            // tsmEdit
            // 
            this.tsmEdit.BackColor = System.Drawing.Color.LightSeaGreen;
            this.tsmEdit.Image = ((System.Drawing.Image)(resources.GetObject("tsmEdit.Image")));
            this.tsmEdit.Name = "tsmEdit";
            this.tsmEdit.Size = new System.Drawing.Size(166, 46);
            this.tsmEdit.Text = "Խմբագրել";
            this.tsmEdit.Click += new System.EventHandler(this.btnEdit1_Click);
            // 
            // tsmRemouve
            // 
            this.tsmRemouve.BackColor = System.Drawing.Color.IndianRed;
            this.tsmRemouve.Image = ((System.Drawing.Image)(resources.GetObject("tsmRemouve.Image")));
            this.tsmRemouve.Name = "tsmRemouve";
            this.tsmRemouve.Size = new System.Drawing.Size(166, 46);
            this.tsmRemouve.Text = "Հեռացնել";
            this.tsmRemouve.Click += new System.EventHandler(this.btrDelete_Click);
            // 
            // dgvClients
            // 
            this.dgvClients.AllowUserToAddRows = false;
            this.dgvClients.AllowUserToResizeColumns = false;
            this.dgvClients.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvClients.BackgroundColor = System.Drawing.Color.PaleTurquoise;
            this.dgvClients.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Sunken;
            this.dgvClients.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvClients.ContextMenuStrip = this.contextMenuStrip1;
            this.dgvClients.Location = new System.Drawing.Point(32, 151);
            this.dgvClients.Name = "dgvClients";
            this.dgvClients.Size = new System.Drawing.Size(902, 317);
            this.dgvClients.TabIndex = 0;
            // 
            // datemounth
            // 
            this.datemounth.CalendarMonthBackground = System.Drawing.Color.LightSeaGreen;
            this.datemounth.Location = new System.Drawing.Point(3, 3);
            this.datemounth.Name = "datemounth";
            this.datemounth.Size = new System.Drawing.Size(163, 20);
            this.datemounth.TabIndex = 24;
            // 
            // cmbFind
            // 
            this.cmbFind.BackColor = System.Drawing.Color.MediumTurquoise;
            this.cmbFind.FormattingEnabled = true;
            this.cmbFind.Location = new System.Drawing.Point(0, 3);
            this.cmbFind.Name = "cmbFind";
            this.cmbFind.Size = new System.Drawing.Size(121, 21);
            this.cmbFind.TabIndex = 25;
            // 
            // txtFind
            // 
            this.txtFind.BackColor = System.Drawing.Color.MediumTurquoise;
            this.txtFind.Location = new System.Drawing.Point(3, 58);
            this.txtFind.Name = "txtFind";
            this.txtFind.Size = new System.Drawing.Size(102, 20);
            this.txtFind.TabIndex = 26;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.DarkCyan;
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.pictureBox2);
            this.panel1.Location = new System.Drawing.Point(0, 13);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1311, 51);
            this.panel1.TabIndex = 28;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Font = new System.Drawing.Font("Comic Sans MS", 24F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.label2.Location = new System.Drawing.Point(46, 2);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(263, 45);
            this.label2.TabIndex = 7;
            this.label2.Text = "Հաճախորդներ";
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox2.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox2.Image")));
            this.pictureBox2.Location = new System.Drawing.Point(1257, 3);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(46, 45);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox2.TabIndex = 120;
            this.pictureBox2.TabStop = false;
            this.pictureBox2.Click += new System.EventHandler(this.pictureBox2_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(1, 546);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(100, 120);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 29;
            this.pictureBox1.TabStop = false;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.DarkTurquoise;
            this.panel2.Controls.Add(this.label1);
            this.panel2.Controls.Add(this.criculButton2);
            this.panel2.Controls.Add(this.panel4);
            this.panel2.Controls.Add(this.txtSum);
            this.panel2.Controls.Add(this.panel3);
            this.panel2.Controls.Add(this.criculButton1);
            this.panel2.Controls.Add(this.btnExit);
            this.panel2.Location = new System.Drawing.Point(1123, 64);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(186, 590);
            this.panel2.TabIndex = 121;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Comic Sans MS", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.label1.Location = new System.Drawing.Point(37, 325);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(119, 15);
            this.label1.TabIndex = 121;
            this.label1.Text = "Ընդհանուր գումար";
            // 
            // criculButton2
            // 
            this.criculButton2.BackColor = System.Drawing.Color.Transparent;
            this.criculButton2.FlatAppearance.BorderSize = 0;
            this.criculButton2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.criculButton2.Image = ((System.Drawing.Image)(resources.GetObject("criculButton2.Image")));
            this.criculButton2.Location = new System.Drawing.Point(8, 253);
            this.criculButton2.Name = "criculButton2";
            this.criculButton2.Size = new System.Drawing.Size(75, 72);
            this.criculButton2.TabIndex = 23;
            this.metroToolTip1.SetToolTip(this.criculButton2, "Տվյալ ամսվա եկամուտը");
            this.criculButton2.UseVisualStyleBackColor = false;
            this.criculButton2.Click += new System.EventHandler(this.criculButton2_Click);
            // 
            // panel4
            // 
            this.panel4.BackColor = System.Drawing.Color.Transparent;
            this.panel4.Controls.Add(this.chbDebt);
            this.panel4.Controls.Add(this.chPrice);
            this.panel4.Controls.Add(this.datemounth);
            this.panel4.Controls.Add(this.btnFind);
            this.panel4.Location = new System.Drawing.Point(5, 126);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(175, 104);
            this.panel4.TabIndex = 123;
            // 
            // btnFind
            // 
            this.btnFind.BackColor = System.Drawing.Color.Transparent;
            this.btnFind.FlatAppearance.BorderSize = 0;
            this.btnFind.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFind.Image = ((System.Drawing.Image)(resources.GetObject("btnFind.Image")));
            this.btnFind.Location = new System.Drawing.Point(3, 38);
            this.btnFind.Name = "btnFind";
            this.btnFind.Size = new System.Drawing.Size(61, 61);
            this.btnFind.TabIndex = 27;
            this.metroToolTip1.SetToolTip(this.btnFind, "Փնտրել");
            this.btnFind.UseVisualStyleBackColor = false;
            this.btnFind.Click += new System.EventHandler(this.btnFind_Click);
            // 
            // txtSum
            // 
            this.txtSum.Location = new System.Drawing.Point(40, 343);
            this.txtSum.Name = "txtSum";
            this.txtSum.Size = new System.Drawing.Size(118, 20);
            this.txtSum.TabIndex = 123;
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.MediumTurquoise;
            this.panel3.Controls.Add(this.cmbFind);
            this.panel3.Controls.Add(this.txtFind);
            this.panel3.Location = new System.Drawing.Point(5, 7);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(175, 104);
            this.panel3.TabIndex = 122;
            // 
            // criculButton1
            // 
            this.criculButton1.BackColor = System.Drawing.Color.Transparent;
            this.criculButton1.FlatAppearance.BorderSize = 0;
            this.criculButton1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.criculButton1.Image = ((System.Drawing.Image)(resources.GetObject("criculButton1.Image")));
            this.criculButton1.Location = new System.Drawing.Point(108, 253);
            this.criculButton1.Name = "criculButton1";
            this.criculButton1.Size = new System.Drawing.Size(75, 72);
            this.criculButton1.TabIndex = 22;
            this.metroToolTip1.SetToolTip(this.criculButton1, "Տվյալ ամսվա պարտքը");
            this.criculButton1.UseVisualStyleBackColor = false;
            this.criculButton1.Click += new System.EventHandler(this.criculButton1_Click);
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.Color.Transparent;
            this.btnExit.FlatAppearance.BorderSize = 0;
            this.btnExit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExit.Image = ((System.Drawing.Image)(resources.GetObject("btnExit.Image")));
            this.btnExit.Location = new System.Drawing.Point(109, 514);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(71, 61);
            this.btnExit.TabIndex = 21;
            this.metroToolTip1.SetToolTip(this.btnExit, "ՓԱԿԵԼ");
            this.btnExit.UseVisualStyleBackColor = false;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // panel5
            // 
            this.panel5.BackColor = System.Drawing.Color.Teal;
            this.panel5.Location = new System.Drawing.Point(0, 64);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(26, 482);
            this.panel5.TabIndex = 121;
            // 
            // panel6
            // 
            this.panel6.BackColor = System.Drawing.Color.Teal;
            this.panel6.Location = new System.Drawing.Point(102, 645);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(1209, 21);
            this.panel6.TabIndex = 122;
            // 
            // metroToolTip1
            // 
            this.metroToolTip1.Style = MetroFramework.MetroColorStyle.Blue;
            this.metroToolTip1.StyleManager = null;
            this.metroToolTip1.Theme = MetroFramework.MetroThemeStyle.Light;
            // 
            // btnEdit1
            // 
            this.btnEdit1.BackColor = System.Drawing.Color.DarkTurquoise;
            this.btnEdit1.FlatAppearance.BorderSize = 0;
            this.btnEdit1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEdit1.Image = ((System.Drawing.Image)(resources.GetObject("btnEdit1.Image")));
            this.btnEdit1.Location = new System.Drawing.Point(944, 252);
            this.btnEdit1.Name = "btnEdit1";
            this.btnEdit1.Size = new System.Drawing.Size(75, 72);
            this.btnEdit1.TabIndex = 20;
            this.metroToolTip1.SetToolTip(this.btnEdit1, "Խմբագրել");
            this.btnEdit1.UseVisualStyleBackColor = false;
            this.btnEdit1.Click += new System.EventHandler(this.btnEdit1_Click);
            // 
            // btrDelete
            // 
            this.btrDelete.BackColor = System.Drawing.Color.IndianRed;
            this.btrDelete.FlatAppearance.BorderSize = 0;
            this.btrDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btrDelete.Image = ((System.Drawing.Image)(resources.GetObject("btrDelete.Image")));
            this.btrDelete.Location = new System.Drawing.Point(865, 474);
            this.btrDelete.Name = "btrDelete";
            this.btrDelete.Size = new System.Drawing.Size(69, 63);
            this.btrDelete.TabIndex = 19;
            this.metroToolTip1.SetToolTip(this.btrDelete, "Հեռացնել");
            this.btrDelete.UseVisualStyleBackColor = false;
            this.btrDelete.Click += new System.EventHandler(this.btrDelete_Click);
            // 
            // btnSave1
            // 
            this.btnSave1.BackColor = System.Drawing.Color.MediumAquamarine;
            this.btnSave1.FlatAppearance.BorderSize = 0;
            this.btnSave1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave1.Image = ((System.Drawing.Image)(resources.GetObject("btnSave1.Image")));
            this.btnSave1.Location = new System.Drawing.Point(939, 151);
            this.btnSave1.Name = "btnSave1";
            this.btnSave1.Size = new System.Drawing.Size(75, 72);
            this.btnSave1.TabIndex = 18;
            this.metroToolTip1.SetToolTip(this.btnSave1, "Գրանցել");
            this.btnSave1.UseVisualStyleBackColor = false;
            this.btnSave1.Click += new System.EventHandler(this.btnSave1_Click);
            // 
            // chPrice
            // 
            this.chPrice.Image = ((System.Drawing.Image)(resources.GetObject("chPrice.Image")));
            this.chPrice.Location = new System.Drawing.Point(81, 34);
            this.chPrice.Name = "chPrice";
            this.chPrice.Size = new System.Drawing.Size(52, 28);
            this.chPrice.TabIndex = 28;
            this.chPrice.UseVisualStyleBackColor = true;
            this.chPrice.CheckedChanged += new System.EventHandler(this.chPrice_CheckedChanged);
            // 
            // chbDebt
            // 
            this.chbDebt.Image = ((System.Drawing.Image)(resources.GetObject("chbDebt.Image")));
            this.chbDebt.Location = new System.Drawing.Point(81, 67);
            this.chbDebt.Name = "chbDebt";
            this.chbDebt.Size = new System.Drawing.Size(52, 35);
            this.chbDebt.TabIndex = 29;
            this.chbDebt.UseVisualStyleBackColor = true;
            this.chbDebt.CheckedChanged += new System.EventHandler(this.chbDebt_CheckedChanged);
            // 
            // FormClients
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.LightSeaGreen;
            this.ClientSize = new System.Drawing.Size(1309, 666);
            this.Controls.Add(this.panel6);
            this.Controls.Add(this.panel5);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.btnEdit1);
            this.Controls.Add(this.btrDelete);
            this.Controls.Add(this.btnSave1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.dgvClients);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.ImeMode = System.Windows.Forms.ImeMode.Hiragana;
            this.Name = "FormClients";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Պացինետների ցանկ";
            this.Load += new System.EventHandler(this.FormClients_Load);
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvClients)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel4.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem tsmAdd;
        private System.Windows.Forms.ToolStripMenuItem tsmEdit;
        private System.Windows.Forms.ToolStripMenuItem tsmRemouve;
        private System.Windows.Forms.DataGridView dgvClients;
        private CriculButton btnSave1;
        private CriculButton btrDelete;
        private CriculButton btnEdit1;
        private CriculButton btnExit;
        private CriculButton criculButton1;
        private CriculButton criculButton2;
        private System.Windows.Forms.DateTimePicker datemounth;
        private System.Windows.Forms.ComboBox cmbFind;
        private System.Windows.Forms.TextBox txtFind;
        private CriculButton btnFind;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Panel panel6;
        private MetroFramework.Components.MetroToolTip metroToolTip1;
        private System.Windows.Forms.TextBox txtSum;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox chbDebt;
        private System.Windows.Forms.CheckBox chPrice;
    }
}