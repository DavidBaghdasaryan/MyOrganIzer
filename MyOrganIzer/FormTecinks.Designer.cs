namespace MyOrganIzer
{
    partial class FormTecinks
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormTecinks));
            this.cmbTecnics = new System.Windows.Forms.ComboBox();
            this.dateTecincs = new System.Windows.Forms.DateTimePicker();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtSumTecnics = new System.Windows.Forms.TextBox();
            this.txtPriceTecnics = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.btnSum = new MyOrganIzer.CriculButton();
            this.dgvTecnics = new System.Windows.Forms.DataGridView();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmAdd = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmEdit = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmRemouve = new System.Windows.Forms.ToolStripMenuItem();
            this.btnTecnicsEdit = new MyOrganIzer.CriculButton();
            this.btnClose = new MyOrganIzer.CriculButton();
            this.btnTecnicsAddSum = new MyOrganIzer.CriculButton();
            this.btnTecnicsDeleteSum = new MyOrganIzer.CriculButton();
            this.txtTechnoName = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTecnics)).BeginInit();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // cmbTecnics
            // 
            this.cmbTecnics.FormattingEnabled = true;
            this.cmbTecnics.Location = new System.Drawing.Point(30, 88);
            this.cmbTecnics.Name = "cmbTecnics";
            this.cmbTecnics.Size = new System.Drawing.Size(264, 21);
            this.cmbTecnics.TabIndex = 1;
            // 
            // dateTecincs
            // 
            this.dateTecincs.Location = new System.Drawing.Point(370, 89);
            this.dateTecincs.Name = "dateTecincs";
            this.dateTecincs.Size = new System.Drawing.Size(154, 20);
            this.dateTecincs.TabIndex = 22;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(27, 72);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(135, 13);
            this.label1.TabIndex = 25;
            this.label1.Text = "Նյութական միջոցներ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(367, 72);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(86, 13);
            this.label2.TabIndex = 24;
            this.label2.Text = "Գործարքի օր";
            // 
            // txtSumTecnics
            // 
            this.txtSumTecnics.Location = new System.Drawing.Point(984, 490);
            this.txtSumTecnics.Name = "txtSumTecnics";
            this.txtSumTecnics.Size = new System.Drawing.Size(151, 20);
            this.txtSumTecnics.TabIndex = 28;
            // 
            // txtPriceTecnics
            // 
            this.txtPriceTecnics.Location = new System.Drawing.Point(558, 92);
            this.txtPriceTecnics.Name = "txtPriceTecnics";
            this.txtPriceTecnics.Size = new System.Drawing.Size(151, 20);
            this.txtPriceTecnics.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(555, 77);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(113, 13);
            this.label3.TabIndex = 23;
            this.label3.Text = "Վճարված գումար";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.SystemColors.WindowText;
            this.label4.Location = new System.Drawing.Point(981, 474);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(154, 13);
            this.label4.TabIndex = 29;
            this.label4.Text = "Վճարված գումար /ամիս";
            // 
            // btnSum
            // 
            this.btnSum.BackColor = System.Drawing.Color.Peru;
            this.btnSum.FlatAppearance.BorderSize = 0;
            this.btnSum.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSum.Image = ((System.Drawing.Image)(resources.GetObject("btnSum.Image")));
            this.btnSum.Location = new System.Drawing.Point(1159, 451);
            this.btnSum.Name = "btnSum";
            this.btnSum.Size = new System.Drawing.Size(69, 68);
            this.btnSum.TabIndex = 30;
            this.btnSum.UseVisualStyleBackColor = false;
            this.btnSum.Click += new System.EventHandler(this.btnSum_Click_1);
            // 
            // dgvTecnics
            // 
            this.dgvTecnics.BackgroundColor = System.Drawing.Color.Bisque;
            this.dgvTecnics.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTecnics.ContextMenuStrip = this.contextMenuStrip1;
            this.dgvTecnics.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dgvTecnics.Location = new System.Drawing.Point(30, 184);
            this.dgvTecnics.Name = "dgvTecnics";
            this.dgvTecnics.Size = new System.Drawing.Size(1238, 261);
            this.dgvTecnics.TabIndex = 0;
            this.dgvTecnics.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvTecnics_CellDoubleClick);
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
            this.tsmAdd.BackColor = System.Drawing.Color.Goldenrod;
            this.tsmAdd.Image = ((System.Drawing.Image)(resources.GetObject("tsmAdd.Image")));
            this.tsmAdd.ImageTransparentColor = System.Drawing.Color.Blue;
            this.tsmAdd.Name = "tsmAdd";
            this.tsmAdd.Size = new System.Drawing.Size(166, 46);
            this.tsmAdd.Text = "Գրանցել";
            // 
            // tsmEdit
            // 
            this.tsmEdit.BackColor = System.Drawing.Color.DarkGoldenrod;
            this.tsmEdit.Image = ((System.Drawing.Image)(resources.GetObject("tsmEdit.Image")));
            this.tsmEdit.Name = "tsmEdit";
            this.tsmEdit.Size = new System.Drawing.Size(166, 46);
            this.tsmEdit.Text = "Խմբագրել";
            // 
            // tsmRemouve
            // 
            this.tsmRemouve.BackColor = System.Drawing.Color.SaddleBrown;
            this.tsmRemouve.Image = ((System.Drawing.Image)(resources.GetObject("tsmRemouve.Image")));
            this.tsmRemouve.Name = "tsmRemouve";
            this.tsmRemouve.Size = new System.Drawing.Size(166, 46);
            this.tsmRemouve.Text = "Հեռացնել";
            // 
            // btnTecnicsEdit
            // 
            this.btnTecnicsEdit.BackColor = System.Drawing.Color.DarkGoldenrod;
            this.btnTecnicsEdit.FlatAppearance.BorderSize = 0;
            this.btnTecnicsEdit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTecnicsEdit.Image = ((System.Drawing.Image)(resources.GetObject("btnTecnicsEdit.Image")));
            this.btnTecnicsEdit.Location = new System.Drawing.Point(1035, 37);
            this.btnTecnicsEdit.Name = "btnTecnicsEdit";
            this.btnTecnicsEdit.Size = new System.Drawing.Size(75, 72);
            this.btnTecnicsEdit.TabIndex = 34;
            this.btnTecnicsEdit.UseVisualStyleBackColor = false;
            this.btnTecnicsEdit.Click += new System.EventHandler(this.btnTecnicsEdit_Click);
            // 
            // btnClose
            // 
            this.btnClose.BackColor = System.Drawing.Color.BurlyWood;
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Image = ((System.Drawing.Image)(resources.GetObject("btnClose.Image")));
            this.btnClose.Location = new System.Drawing.Point(1231, 536);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(71, 61);
            this.btnClose.TabIndex = 33;
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click_1);
            // 
            // btnTecnicsAddSum
            // 
            this.btnTecnicsAddSum.BackColor = System.Drawing.Color.Goldenrod;
            this.btnTecnicsAddSum.FlatAppearance.BorderSize = 0;
            this.btnTecnicsAddSum.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTecnicsAddSum.Image = ((System.Drawing.Image)(resources.GetObject("btnTecnicsAddSum.Image")));
            this.btnTecnicsAddSum.Location = new System.Drawing.Point(911, 37);
            this.btnTecnicsAddSum.Name = "btnTecnicsAddSum";
            this.btnTecnicsAddSum.Size = new System.Drawing.Size(75, 72);
            this.btnTecnicsAddSum.TabIndex = 32;
            this.btnTecnicsAddSum.UseVisualStyleBackColor = false;
            this.btnTecnicsAddSum.Click += new System.EventHandler(this.btnTecnicsAddSum_Click);
            // 
            // btnTecnicsDeleteSum
            // 
            this.btnTecnicsDeleteSum.BackColor = System.Drawing.Color.SaddleBrown;
            this.btnTecnicsDeleteSum.FlatAppearance.BorderSize = 0;
            this.btnTecnicsDeleteSum.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTecnicsDeleteSum.Image = ((System.Drawing.Image)(resources.GetObject("btnTecnicsDeleteSum.Image")));
            this.btnTecnicsDeleteSum.Location = new System.Drawing.Point(1140, 37);
            this.btnTecnicsDeleteSum.Name = "btnTecnicsDeleteSum";
            this.btnTecnicsDeleteSum.Size = new System.Drawing.Size(75, 72);
            this.btnTecnicsDeleteSum.TabIndex = 31;
            this.btnTecnicsDeleteSum.UseVisualStyleBackColor = false;
            this.btnTecnicsDeleteSum.Click += new System.EventHandler(this.btnTecnicsDeleteSum_Click);
            // 
            // txtTechnoName
            // 
            this.txtTechnoName.Location = new System.Drawing.Point(743, 92);
            this.txtTechnoName.Name = "txtTechnoName";
            this.txtTechnoName.Size = new System.Drawing.Size(151, 20);
            this.txtTechnoName.TabIndex = 35;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(740, 77);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(45, 13);
            this.label5.TabIndex = 36;
            this.label5.Text = "Ա/Ա/Հ";
            // 
            // FormTecinks
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.BurlyWood;
            this.ClientSize = new System.Drawing.Size(1321, 607);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtTechnoName);
            this.Controls.Add(this.btnTecnicsEdit);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnTecnicsAddSum);
            this.Controls.Add(this.btnTecnicsDeleteSum);
            this.Controls.Add(this.cmbTecnics);
            this.Controls.Add(this.btnSum);
            this.Controls.Add(this.dateTecincs);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.dgvTecnics);
            this.Controls.Add(this.txtSumTecnics);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtPriceTecnics);
            this.Controls.Add(this.label3);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "FormTecinks";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FormTecinks";
            this.Load += new System.EventHandler(this.FormTecinks_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgvTecnics)).EndInit();
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cmbTecnics;
        private System.Windows.Forms.DateTimePicker dateTecincs;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtSumTecnics;
        private System.Windows.Forms.TextBox txtPriceTecnics;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private CriculButton btnSum;
        private System.Windows.Forms.DataGridView dgvTecnics;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem tsmAdd;
        private System.Windows.Forms.ToolStripMenuItem tsmEdit;
        private System.Windows.Forms.ToolStripMenuItem tsmRemouve;
        private CriculButton btnTecnicsEdit;
        private CriculButton btnClose;
        private CriculButton btnTecnicsAddSum;
        private CriculButton btnTecnicsDeleteSum;
        private System.Windows.Forms.TextBox txtTechnoName;
        private System.Windows.Forms.Label label5;
    }
}