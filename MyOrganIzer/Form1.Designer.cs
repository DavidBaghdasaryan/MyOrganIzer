namespace MyOrganIzer
{
    partial class FromWorkSpace
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FromWorkSpace));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmAdd = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmRemouve = new System.Windows.Forms.ToolStripMenuItem();
            this.dgvimpornents = new System.Windows.Forms.DataGridView();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtSumImporters = new System.Windows.Forms.TextBox();
            this.txtSum = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.dateimporters = new System.Windows.Forms.DateTimePicker();
            this.cmbImporters = new System.Windows.Forms.ComboBox();
            this.btnSum = new MyOrganIzer.CriculButton();
            this.btnEdit = new MyOrganIzer.CriculButton();
            this.btnClose = new MyOrganIzer.CriculButton();
            this.btnImportersAddSum = new MyOrganIzer.CriculButton();
            this.btnImportersDeleteSum = new MyOrganIzer.CriculButton();
            this.metroToolTip1 = new MetroFramework.Components.MetroToolTip();
            this.panel1 = new System.Windows.Forms.Panel();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.label6 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvimpornents)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
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
            this.tsmAdd.BackColor = System.Drawing.Color.Turquoise;
            this.tsmAdd.Image = ((System.Drawing.Image)(resources.GetObject("tsmAdd.Image")));
            this.tsmAdd.ImageTransparentColor = System.Drawing.Color.Blue;
            this.tsmAdd.Name = "tsmAdd";
            this.tsmAdd.Size = new System.Drawing.Size(166, 46);
            this.tsmAdd.Text = "Գրանցել";
            this.tsmAdd.Click += new System.EventHandler(this.btnImportersAddSum_Click);
            // 
            // tsmEdit
            // 
            this.tsmEdit.BackColor = System.Drawing.Color.SeaGreen;
            this.tsmEdit.Image = ((System.Drawing.Image)(resources.GetObject("tsmEdit.Image")));
            this.tsmEdit.Name = "tsmEdit";
            this.tsmEdit.Size = new System.Drawing.Size(166, 46);
            this.tsmEdit.Text = "Խմբագրել";
            this.tsmEdit.Click += new System.EventHandler(this.btnEdit_Click);
            // 
            // tsmRemouve
            // 
            this.tsmRemouve.BackColor = System.Drawing.Color.OrangeRed;
            this.tsmRemouve.Image = ((System.Drawing.Image)(resources.GetObject("tsmRemouve.Image")));
            this.tsmRemouve.Name = "tsmRemouve";
            this.tsmRemouve.Size = new System.Drawing.Size(166, 46);
            this.tsmRemouve.Text = "Հեռացնել";
            this.tsmRemouve.Click += new System.EventHandler(this.btnImportersDeleteSum_Click);
            // 
            // dgvimpornents
            // 
            this.dgvimpornents.AllowUserToAddRows = false;
            this.dgvimpornents.BackgroundColor = System.Drawing.Color.DarkTurquoise;
            this.dgvimpornents.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvimpornents.ContextMenuStrip = this.contextMenuStrip1;
            this.dgvimpornents.Location = new System.Drawing.Point(27, 211);
            this.dgvimpornents.Name = "dgvimpornents";
            this.dgvimpornents.RowHeadersVisible = false;
            this.dgvimpornents.Size = new System.Drawing.Size(1128, 261);
            this.dgvimpornents.TabIndex = 0;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.SystemColors.WindowText;
            this.label4.Location = new System.Drawing.Point(930, 510);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(154, 13);
            this.label4.TabIndex = 29;
            this.label4.Text = "Վճարված գումար /ամիս";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(214, 155);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(113, 13);
            this.label3.TabIndex = 23;
            this.label3.Text = "Վճարված գումար";
            // 
            // txtSumImporters
            // 
            this.txtSumImporters.Location = new System.Drawing.Point(217, 171);
            this.txtSumImporters.Name = "txtSumImporters";
            this.txtSumImporters.Size = new System.Drawing.Size(151, 20);
            this.txtSumImporters.TabIndex = 2;
            // 
            // txtSum
            // 
            this.txtSum.Location = new System.Drawing.Point(933, 526);
            this.txtSum.Name = "txtSum";
            this.txtSum.Size = new System.Drawing.Size(151, 20);
            this.txtSum.TabIndex = 28;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(393, 155);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 13);
            this.label1.TabIndex = 24;
            this.label1.Text = "Առաքման օր ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(24, 151);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 13);
            this.label2.TabIndex = 25;
            this.label2.Text = "Առաքիչներ";
            // 
            // dateimporters
            // 
            this.dateimporters.Location = new System.Drawing.Point(396, 171);
            this.dateimporters.Name = "dateimporters";
            this.dateimporters.Size = new System.Drawing.Size(154, 20);
            this.dateimporters.TabIndex = 22;
            // 
            // cmbImporters
            // 
            this.cmbImporters.FormattingEnabled = true;
            this.cmbImporters.Location = new System.Drawing.Point(27, 170);
            this.cmbImporters.Name = "cmbImporters";
            this.cmbImporters.Size = new System.Drawing.Size(178, 21);
            this.cmbImporters.TabIndex = 1;
            // 
            // btnSum
            // 
            this.btnSum.BackColor = System.Drawing.Color.LimeGreen;
            this.btnSum.FlatAppearance.BorderSize = 0;
            this.btnSum.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSum.Image = ((System.Drawing.Image)(resources.GetObject("btnSum.Image")));
            this.btnSum.Location = new System.Drawing.Point(1110, 482);
            this.btnSum.Name = "btnSum";
            this.btnSum.Size = new System.Drawing.Size(69, 68);
            this.btnSum.TabIndex = 30;
            this.metroToolTip1.SetToolTip(this.btnSum, "Դիտել ընդհանուր գումարը");
            this.btnSum.UseVisualStyleBackColor = false;
            this.btnSum.Click += new System.EventHandler(this.btnSum_Click);
            // 
            // btnEdit
            // 
            this.btnEdit.BackColor = System.Drawing.Color.SeaGreen;
            this.btnEdit.FlatAppearance.BorderSize = 0;
            this.btnEdit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEdit.Image = ((System.Drawing.Image)(resources.GetObject("btnEdit.Image")));
            this.btnEdit.Location = new System.Drawing.Point(1080, 121);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Size = new System.Drawing.Size(75, 72);
            this.btnEdit.TabIndex = 27;
            this.metroToolTip1.SetToolTip(this.btnEdit, "Խմբագրել");
            this.btnEdit.UseVisualStyleBackColor = false;
            this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
            // 
            // btnClose
            // 
            this.btnClose.BackColor = System.Drawing.Color.LightSeaGreen;
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Image = ((System.Drawing.Image)(resources.GetObject("btnClose.Image")));
            this.btnClose.Location = new System.Drawing.Point(1218, 554);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(71, 61);
            this.btnClose.TabIndex = 26;
            this.metroToolTip1.SetToolTip(this.btnClose, "Փակել");
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnImportersAddSum
            // 
            this.btnImportersAddSum.BackColor = System.Drawing.Color.Turquoise;
            this.btnImportersAddSum.FlatAppearance.BorderSize = 0;
            this.btnImportersAddSum.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnImportersAddSum.Image = ((System.Drawing.Image)(resources.GetObject("btnImportersAddSum.Image")));
            this.btnImportersAddSum.Location = new System.Drawing.Point(970, 121);
            this.btnImportersAddSum.Name = "btnImportersAddSum";
            this.btnImportersAddSum.Size = new System.Drawing.Size(75, 72);
            this.btnImportersAddSum.TabIndex = 21;
            this.metroToolTip1.SetToolTip(this.btnImportersAddSum, "Գրանցել");
            this.btnImportersAddSum.UseVisualStyleBackColor = false;
            this.btnImportersAddSum.Click += new System.EventHandler(this.btnImportersAddSum_Click);
            // 
            // btnImportersDeleteSum
            // 
            this.btnImportersDeleteSum.BackColor = System.Drawing.Color.OrangeRed;
            this.btnImportersDeleteSum.FlatAppearance.BorderSize = 0;
            this.btnImportersDeleteSum.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnImportersDeleteSum.Image = ((System.Drawing.Image)(resources.GetObject("btnImportersDeleteSum.Image")));
            this.btnImportersDeleteSum.Location = new System.Drawing.Point(1177, 121);
            this.btnImportersDeleteSum.Name = "btnImportersDeleteSum";
            this.btnImportersDeleteSum.Size = new System.Drawing.Size(75, 72);
            this.btnImportersDeleteSum.TabIndex = 20;
            this.metroToolTip1.SetToolTip(this.btnImportersDeleteSum, "Հեռացնել");
            this.btnImportersDeleteSum.UseVisualStyleBackColor = false;
            this.btnImportersDeleteSum.Click += new System.EventHandler(this.btnImportersDeleteSum_Click);
            // 
            // metroToolTip1
            // 
            this.metroToolTip1.Style = MetroFramework.MetroColorStyle.Blue;
            this.metroToolTip1.StyleManager = null;
            this.metroToolTip1.Theme = MetroFramework.MetroThemeStyle.Light;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.DarkGreen;
            this.panel1.Controls.Add(this.pictureBox3);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Location = new System.Drawing.Point(0, 24);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1289, 51);
            this.panel1.TabIndex = 38;
            // 
            // pictureBox3
            // 
            this.pictureBox3.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox3.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox3.Image")));
            this.pictureBox3.Location = new System.Drawing.Point(1237, 0);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(52, 51);
            this.pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox3.TabIndex = 122;
            this.pictureBox3.TabStop = false;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.BackColor = System.Drawing.Color.Transparent;
            this.label6.Font = new System.Drawing.Font("Comic Sans MS", 24F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.label6.Location = new System.Drawing.Point(13, 2);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(206, 45);
            this.label6.TabIndex = 7;
            this.label6.Text = "Առաքիչներ";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(0, 532);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(99, 97);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 121;
            this.pictureBox1.TabStop = false;
            // 
            // FromWorkSpace
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.LightSeaGreen;
            this.ClientSize = new System.Drawing.Size(1301, 627);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.btnSum);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtSum);
            this.Controls.Add(this.btnEdit);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.dateimporters);
            this.Controls.Add(this.btnImportersAddSum);
            this.Controls.Add(this.btnImportersDeleteSum);
            this.Controls.Add(this.txtSumImporters);
            this.Controls.Add(this.cmbImporters);
            this.Controls.Add(this.dgvimpornents);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "FromWorkSpace";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Առաքիչներ";
            this.Load += new System.EventHandler(this.FromWorkSpace_Load);
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvimpornents)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private CriculButton btnImportersDeleteSum;
        private CriculButton btnImportersAddSum;
        private CriculButton btnClose;
        private CriculButton btnEdit;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem tsmAdd;
        private System.Windows.Forms.ToolStripMenuItem tsmEdit;
        private System.Windows.Forms.ToolStripMenuItem tsmRemouve;
        private System.Windows.Forms.DataGridView dgvimpornents;
        private CriculButton btnSum;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtSumImporters;
        private System.Windows.Forms.TextBox txtSum;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DateTimePicker dateimporters;
        private System.Windows.Forms.ComboBox cmbImporters;
        private MetroFramework.Components.MetroToolTip metroToolTip1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PictureBox pictureBox3;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}

