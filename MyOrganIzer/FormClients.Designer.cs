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
            this.btnEdit1 = new MyOrganIzer.CriculButton();
            this.btrDelete = new MyOrganIzer.CriculButton();
            this.btnSave1 = new MyOrganIzer.CriculButton();
            this.btnFind = new MyOrganIzer.CriculButton();
            this.criculButton2 = new MyOrganIzer.CriculButton();
            this.criculButton1 = new MyOrganIzer.CriculButton();
            this.btnExit = new MyOrganIzer.CriculButton();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvClients)).BeginInit();
            this.panel1.SuspendLayout();
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
            this.tsmRemouve.BackColor = System.Drawing.Color.Snow;
            this.tsmRemouve.Image = ((System.Drawing.Image)(resources.GetObject("tsmRemouve.Image")));
            this.tsmRemouve.Name = "tsmRemouve";
            this.tsmRemouve.Size = new System.Drawing.Size(166, 46);
            this.tsmRemouve.Text = "Հեռացնել";
            this.tsmRemouve.Click += new System.EventHandler(this.btrDelete_Click);
            // 
            // dgvClients
            // 
            this.dgvClients.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvClients.BackgroundColor = System.Drawing.Color.Azure;
            this.dgvClients.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Sunken;
            this.dgvClients.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvClients.ContextMenuStrip = this.contextMenuStrip1;
            this.dgvClients.Location = new System.Drawing.Point(260, 161);
            this.dgvClients.Name = "dgvClients";
            this.dgvClients.Size = new System.Drawing.Size(902, 317);
            this.dgvClients.TabIndex = 0;
            // 
            // datemounth
            // 
            this.datemounth.CalendarMonthBackground = System.Drawing.SystemColors.Info;
            this.datemounth.Location = new System.Drawing.Point(12, 174);
            this.datemounth.Name = "datemounth";
            this.datemounth.Size = new System.Drawing.Size(163, 20);
            this.datemounth.TabIndex = 24;
            // 
            // cmbFind
            // 
            this.cmbFind.BackColor = System.Drawing.Color.Azure;
            this.cmbFind.FormattingEnabled = true;
            this.cmbFind.Location = new System.Drawing.Point(1168, 161);
            this.cmbFind.Name = "cmbFind";
            this.cmbFind.Size = new System.Drawing.Size(121, 21);
            this.cmbFind.TabIndex = 25;
            // 
            // txtFind
            // 
            this.txtFind.BackColor = System.Drawing.Color.Azure;
            this.txtFind.Location = new System.Drawing.Point(1168, 202);
            this.txtFind.Name = "txtFind";
            this.txtFind.Size = new System.Drawing.Size(102, 20);
            this.txtFind.TabIndex = 26;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Azure;
            this.panel1.Controls.Add(this.label2);
            this.panel1.Location = new System.Drawing.Point(0, 13);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1163, 51);
            this.panel1.TabIndex = 28;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Font = new System.Drawing.Font("Comic Sans MS", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.Location = new System.Drawing.Point(24, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(247, 45);
            this.label2.TabIndex = 7;
            this.label2.Text = "Հաճախորդներ";
            // 
            // btnEdit1
            // 
            this.btnEdit1.BackColor = System.Drawing.Color.LightSeaGreen;
            this.btnEdit1.FlatAppearance.BorderSize = 0;
            this.btnEdit1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEdit1.Image = ((System.Drawing.Image)(resources.GetObject("btnEdit1.Image")));
            this.btnEdit1.Location = new System.Drawing.Point(388, 492);
            this.btnEdit1.Name = "btnEdit1";
            this.btnEdit1.Size = new System.Drawing.Size(75, 72);
            this.btnEdit1.TabIndex = 20;
            this.btnEdit1.UseVisualStyleBackColor = false;
            this.btnEdit1.Click += new System.EventHandler(this.btnEdit1_Click);
            // 
            // btrDelete
            // 
            this.btrDelete.BackColor = System.Drawing.Color.White;
            this.btrDelete.FlatAppearance.BorderSize = 0;
            this.btrDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btrDelete.Image = ((System.Drawing.Image)(resources.GetObject("btrDelete.Image")));
            this.btrDelete.Location = new System.Drawing.Point(1092, 501);
            this.btrDelete.Name = "btrDelete";
            this.btrDelete.Size = new System.Drawing.Size(60, 55);
            this.btrDelete.TabIndex = 19;
            this.btrDelete.UseVisualStyleBackColor = false;
            this.btrDelete.Click += new System.EventHandler(this.btrDelete_Click);
            // 
            // btnSave1
            // 
            this.btnSave1.BackColor = System.Drawing.Color.MediumTurquoise;
            this.btnSave1.FlatAppearance.BorderSize = 0;
            this.btnSave1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave1.Image = ((System.Drawing.Image)(resources.GetObject("btnSave1.Image")));
            this.btnSave1.Location = new System.Drawing.Point(269, 492);
            this.btnSave1.Name = "btnSave1";
            this.btnSave1.Size = new System.Drawing.Size(75, 72);
            this.btnSave1.TabIndex = 18;
            this.btnSave1.UseVisualStyleBackColor = false;
            this.btnSave1.Click += new System.EventHandler(this.btnSave1_Click);
            // 
            // btnFind
            // 
            this.btnFind.BackColor = System.Drawing.Color.Transparent;
            this.btnFind.FlatAppearance.BorderSize = 0;
            this.btnFind.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFind.Image = ((System.Drawing.Image)(resources.GetObject("btnFind.Image")));
            this.btnFind.Location = new System.Drawing.Point(1183, 238);
            this.btnFind.Name = "btnFind";
            this.btnFind.Size = new System.Drawing.Size(75, 72);
            this.btnFind.TabIndex = 27;
            this.btnFind.UseVisualStyleBackColor = false;
            this.btnFind.Click += new System.EventHandler(this.btnFind_Click);
            // 
            // criculButton2
            // 
            this.criculButton2.BackColor = System.Drawing.Color.Transparent;
            this.criculButton2.FlatAppearance.BorderSize = 0;
            this.criculButton2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.criculButton2.Image = ((System.Drawing.Image)(resources.GetObject("criculButton2.Image")));
            this.criculButton2.Location = new System.Drawing.Point(182, 150);
            this.criculButton2.Name = "criculButton2";
            this.criculButton2.Size = new System.Drawing.Size(75, 72);
            this.criculButton2.TabIndex = 23;
            this.criculButton2.UseVisualStyleBackColor = false;
            this.criculButton2.Click += new System.EventHandler(this.criculButton2_Click);
            // 
            // criculButton1
            // 
            this.criculButton1.BackColor = System.Drawing.Color.Transparent;
            this.criculButton1.FlatAppearance.BorderSize = 0;
            this.criculButton1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.criculButton1.Image = ((System.Drawing.Image)(resources.GetObject("criculButton1.Image")));
            this.criculButton1.Location = new System.Drawing.Point(179, 238);
            this.criculButton1.Name = "criculButton1";
            this.criculButton1.Size = new System.Drawing.Size(75, 72);
            this.criculButton1.TabIndex = 22;
            this.criculButton1.UseVisualStyleBackColor = false;
            this.criculButton1.Click += new System.EventHandler(this.criculButton1_Click);
            // 
            // btnExit
            // 
            this.btnExit.BackColor = System.Drawing.Color.Transparent;
            this.btnExit.FlatAppearance.BorderSize = 0;
            this.btnExit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExit.Image = ((System.Drawing.Image)(resources.GetObject("btnExit.Image")));
            this.btnExit.Location = new System.Drawing.Point(1192, 604);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(71, 61);
            this.btnExit.TabIndex = 21;
            this.btnExit.UseVisualStyleBackColor = false;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // FormClients
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.PaleTurquoise;
            this.ClientSize = new System.Drawing.Size(1309, 666);
            this.Controls.Add(this.btnEdit1);
            this.Controls.Add(this.btrDelete);
            this.Controls.Add(this.txtFind);
            this.Controls.Add(this.cmbFind);
            this.Controls.Add(this.btnSave1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.btnFind);
            this.Controls.Add(this.datemounth);
            this.Controls.Add(this.criculButton2);
            this.Controls.Add(this.criculButton1);
            this.Controls.Add(this.btnExit);
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
            this.ResumeLayout(false);
            this.PerformLayout();

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
    }
}