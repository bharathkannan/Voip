namespace SocketCoder
{
    partial class Form1
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
            this.DisConncet = new System.Windows.Forms.Button();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.Conncet = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.text_Port = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // DisConncet
            // 
            this.DisConncet.Enabled = false;
            this.DisConncet.Location = new System.Drawing.Point(264, 10);
            this.DisConncet.Name = "DisConncet";
            this.DisConncet.Size = new System.Drawing.Size(76, 23);
            this.DisConncet.TabIndex = 11;
            this.DisConncet.Text = "Disconnect";
            this.DisConncet.UseVisualStyleBackColor = true;
            this.DisConncet.Click += new System.EventHandler(this.DisConncet_Click);
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(4, 38);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(370, 160);
            this.listBox1.TabIndex = 10;
            // 
            // Conncet
            // 
            this.Conncet.Location = new System.Drawing.Point(149, 9);
            this.Conncet.Name = "Conncet";
            this.Conncet.Size = new System.Drawing.Size(109, 23);
            this.Conncet.TabIndex = 7;
            this.Conncet.Text = "Start The Server";
            this.Conncet.UseVisualStyleBackColor = true;
            this.Conncet.Click += new System.EventHandler(this.Conncet_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 15);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(27, 13);
            this.label2.TabIndex = 15;
            this.label2.Text = "Port";
            // 
            // text_Port
            // 
            this.text_Port.Location = new System.Drawing.Point(39, 12);
            this.text_Port.Name = "text_Port";
            this.text_Port.Size = new System.Drawing.Size(104, 20);
            this.text_Port.TabIndex = 16;
            this.text_Port.Text = "5000";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(376, 213);
            this.Controls.Add(this.text_Port);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.DisConncet);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.Conncet);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SocketCoderBinaryServer - (C) SocketCoder.Com";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button DisConncet;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button Conncet;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox text_Port;
    }
}

