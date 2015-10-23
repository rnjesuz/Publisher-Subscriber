namespace SESDAD
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
            this.SubscribeButton = new System.Windows.Forms.Button();
            this.TopicBox = new System.Windows.Forms.TextBox();
            this.PublicationBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // SubscribeButton
            // 
            this.SubscribeButton.Location = new System.Drawing.Point(197, 12);
            this.SubscribeButton.Name = "SubscribeButton";
            this.SubscribeButton.Size = new System.Drawing.Size(75, 23);
            this.SubscribeButton.TabIndex = 0;
            this.SubscribeButton.Text = "Subscribe";
            this.SubscribeButton.UseVisualStyleBackColor = true;
            this.SubscribeButton.Click += new System.EventHandler(this.SubscribeButton_Click);
            // 
            // TopicBox
            // 
            this.TopicBox.Location = new System.Drawing.Point(12, 12);
            this.TopicBox.Name = "TopicBox";
            this.TopicBox.Size = new System.Drawing.Size(179, 20);
            this.TopicBox.TabIndex = 1;
            this.TopicBox.TextChanged += new System.EventHandler(this.TopicBox_TextChanged);
            // 
            // PublicationBox
            // 
            this.PublicationBox.Location = new System.Drawing.Point(13, 39);
            this.PublicationBox.Multiline = true;
            this.PublicationBox.Name = "PublicationBox";
            this.PublicationBox.Size = new System.Drawing.Size(259, 210);
            this.PublicationBox.TabIndex = 2;
            this.PublicationBox.TextChanged += new System.EventHandler(this.PublicationBox_TextChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.PublicationBox);
            this.Controls.Add(this.TopicBox);
            this.Controls.Add(this.SubscribeButton);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button SubscribeButton;
        private System.Windows.Forms.TextBox TopicBox;
        private System.Windows.Forms.TextBox PublicationBox;
    }
}

