namespace MyOrganIzer
{
    partial class MyMessageBox
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.LabelQuestion = new System.Windows.Forms.Label();
            this.btnYes = new System.Windows.Forms.Button();
            this.btnNo = new System.Windows.Forms.Button();
            this.btnClosOk = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel5 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.LabelQuestion);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(26, 23);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(434, 61);
            this.panel1.TabIndex = 0;
            // 
            // LabelQuestion
            // 
            this.LabelQuestion.AutoSize = true;
            this.LabelQuestion.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LabelQuestion.Font = new System.Drawing.Font("Centaur", 11.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LabelQuestion.ForeColor = System.Drawing.Color.Black;
            this.LabelQuestion.Location = new System.Drawing.Point(0, 0);
            this.LabelQuestion.Name = "LabelQuestion";
            this.LabelQuestion.Size = new System.Drawing.Size(158, 17);
            this.LabelQuestion.TabIndex = 7;
            this.LabelQuestion.Text = "Ցանկանու՞մ եք հեռացնել";
            // 
            // btnYes
            // 
            this.btnYes.AutoSize = true;
            this.btnYes.BackColor = System.Drawing.Color.Turquoise;
            this.btnYes.CausesValidation = false;
            this.btnYes.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnYes.ForeColor = System.Drawing.Color.White;
            this.btnYes.Location = new System.Drawing.Point(128, 146);
            this.btnYes.Name = "btnYes";
            this.btnYes.Size = new System.Drawing.Size(102, 63);
            this.btnYes.TabIndex = 2;
            this.btnYes.Text = "Այո";
            this.btnYes.UseVisualStyleBackColor = false;
            this.btnYes.Click += new System.EventHandler(this.btnYes_Click);
            // 
            // btnNo
            // 
            this.btnNo.AutoSize = true;
            this.btnNo.BackColor = System.Drawing.Color.Red;
            this.btnNo.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnNo.ForeColor = System.Drawing.Color.White;
            this.btnNo.Location = new System.Drawing.Point(261, 146);
            this.btnNo.Name = "btnNo";
            this.btnNo.Size = new System.Drawing.Size(91, 63);
            this.btnNo.TabIndex = 3;
            this.btnNo.Text = "Ոչ";
            this.btnNo.UseVisualStyleBackColor = false;
            this.btnNo.Click += new System.EventHandler(this.btnNo_Click);
            // 
            // btnClosOk
            // 
            this.btnClosOk.AutoSize = true;
            this.btnClosOk.BackColor = System.Drawing.Color.Orange;
            this.btnClosOk.CausesValidation = false;
            this.btnClosOk.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnClosOk.ForeColor = System.Drawing.Color.White;
            this.btnClosOk.Location = new System.Drawing.Point(359, 186);
            this.btnClosOk.Name = "btnClosOk";
            this.btnClosOk.Size = new System.Drawing.Size(102, 63);
            this.btnClosOk.TabIndex = 4;
            this.btnClosOk.Text = "Փակել";
            this.btnClosOk.UseVisualStyleBackColor = false;
            this.btnClosOk.Visible = false;
            this.btnClosOk.Click += new System.EventHandler(this.btnClosOk_Click);
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.SandyBrown;
            this.panel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel2.Location = new System.Drawing.Point(26, 249);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(434, 23);
            this.panel2.TabIndex = 8;
            // 
            // panel5
            // 
            this.panel5.BackColor = System.Drawing.Color.SandyBrown;
            this.panel5.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel5.Location = new System.Drawing.Point(0, 0);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(26, 272);
            this.panel5.TabIndex = 123;
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.SandyBrown;
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Location = new System.Drawing.Point(26, 0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(462, 23);
            this.panel3.TabIndex = 9;
            // 
            // panel4
            // 
            this.panel4.BackColor = System.Drawing.Color.SandyBrown;
            this.panel4.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel4.Location = new System.Drawing.Point(460, 23);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(28, 249);
            this.panel4.TabIndex = 124;
            // 
            // MyMessageBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.Moccasin;
            this.ClientSize = new System.Drawing.Size(488, 272);
            this.Controls.Add(this.btnClosOk);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel5);
            this.Controls.Add(this.btnNo);
            this.Controls.Add(this.btnYes);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "MyMessageBox";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MyMessageBox";
            this.Load += new System.EventHandler(this.MyMessageBox_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnYes;
        private System.Windows.Forms.Button btnNo;
        private System.Windows.Forms.Label LabelQuestion;
        private System.Windows.Forms.Button btnClosOk;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel4;
    }
}