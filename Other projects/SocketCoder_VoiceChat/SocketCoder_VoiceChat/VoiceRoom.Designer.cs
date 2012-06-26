namespace SocketCoder_VoiceChat
{
    partial class VoiceRoom
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
            this.JoinBTN = new System.Windows.Forms.Button();
            this.StartTalkingBTN = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.ServerIPTXT = new System.Windows.Forms.TextBox();
            this.StopTalkingBTN = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // JoinBTN
            // 
            this.JoinBTN.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.JoinBTN.Location = new System.Drawing.Point(6, 46);
            this.JoinBTN.Name = "JoinBTN";
            this.JoinBTN.Size = new System.Drawing.Size(316, 56);
            this.JoinBTN.TabIndex = 0;
            this.JoinBTN.Text = "Join";
            this.JoinBTN.UseVisualStyleBackColor = true;
            this.JoinBTN.Click += new System.EventHandler(this.JoinBTN_Click);
            // 
            // StartTalkingBTN
            // 
            this.StartTalkingBTN.Enabled = false;
            this.StartTalkingBTN.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.StartTalkingBTN.Location = new System.Drawing.Point(10, 128);
            this.StartTalkingBTN.Name = "StartTalkingBTN";
            this.StartTalkingBTN.Size = new System.Drawing.Size(312, 53);
            this.StartTalkingBTN.TabIndex = 1;
            this.StartTalkingBTN.Text = "Talk";
            this.StartTalkingBTN.UseVisualStyleBackColor = true;
            this.StartTalkingBTN.Click += new System.EventHandler(this.StartTalkingBTN_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(52, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Server IP";
            // 
            // ServerIPTXT
            // 
            this.ServerIPTXT.Location = new System.Drawing.Point(70, 12);
            this.ServerIPTXT.Name = "ServerIPTXT";
            this.ServerIPTXT.Size = new System.Drawing.Size(252, 20);
            this.ServerIPTXT.TabIndex = 3;
            // 
            // StopTalkingBTN
            // 
            this.StopTalkingBTN.Enabled = false;
            this.StopTalkingBTN.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.StopTalkingBTN.Location = new System.Drawing.Point(10, 186);
            this.StopTalkingBTN.Name = "StopTalkingBTN";
            this.StopTalkingBTN.Size = new System.Drawing.Size(312, 53);
            this.StopTalkingBTN.TabIndex = 4;
            this.StopTalkingBTN.Text = "Stop Talking";
            this.StopTalkingBTN.UseVisualStyleBackColor = true;
            this.StopTalkingBTN.Click += new System.EventHandler(this.StopTalkingBTN_Click);
            // 
            // VoiceRoom
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(330, 254);
            this.Controls.Add(this.StopTalkingBTN);
            this.Controls.Add(this.ServerIPTXT);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.StartTalkingBTN);
            this.Controls.Add(this.JoinBTN);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "VoiceRoom";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "(SocketCoder) Voice Chat Room";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.VoiceRoom_FormClosing);
            this.Load += new System.EventHandler(this.VoiceRoom_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button JoinBTN;
        private System.Windows.Forms.Button StartTalkingBTN;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox ServerIPTXT;
        private System.Windows.Forms.Button StopTalkingBTN;
    }
}

