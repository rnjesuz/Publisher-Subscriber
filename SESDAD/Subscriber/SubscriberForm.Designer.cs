namespace SESDAD
{
    partial class SubscriberForm
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
            this.UnsubBox = new System.Windows.Forms.TextBox();
            this.UnsubButton = new System.Windows.Forms.Button();
            this.TopicListBox = new System.Windows.Forms.TextBox();
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
            this.PublicationBox.Location = new System.Drawing.Point(94, 71);
            this.PublicationBox.Multiline = true;
            this.PublicationBox.Name = "PublicationBox";
            this.PublicationBox.Size = new System.Drawing.Size(178, 178);
            this.PublicationBox.TabIndex = 2;
            this.PublicationBox.TextChanged += new System.EventHandler(this.PublicationBox_TextChanged);
            // 
            // UnsubBox
            // 
            this.UnsubBox.Location = new System.Drawing.Point(12, 38);
            this.UnsubBox.Name = "UnsubBox";
            this.UnsubBox.Size = new System.Drawing.Size(179, 20);
            this.UnsubBox.TabIndex = 3;
            // 
            // UnsubButton
            // 
            this.UnsubButton.Location = new System.Drawing.Point(197, 41);
            this.UnsubButton.Name = "UnsubButton";
            this.UnsubButton.Size = new System.Drawing.Size(75, 23);
            this.UnsubButton.TabIndex = 4;
            this.UnsubButton.Text = "Unsubscribe";
            this.UnsubButton.UseVisualStyleBackColor = true;
            this.UnsubButton.Click += new System.EventHandler(this.UnsubButton_Click);
            // 
            // TopicListBox
            // 
            this.TopicListBox.Location = new System.Drawing.Point(12, 71);
            this.TopicListBox.Multiline = true;
            this.TopicListBox.Name = "TopicListBox";
            this.TopicListBox.Size = new System.Drawing.Size(76, 178);
            this.TopicListBox.TabIndex = 5;
            this.TopicListBox.TextChanged += new System.EventHandler(this.TopicListBox_TextChanged);
            // 
            // SubscriberForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.TopicListBox);
            this.Controls.Add(this.UnsubButton);
            this.Controls.Add(this.UnsubBox);
            this.Controls.Add(this.PublicationBox);
            this.Controls.Add(this.TopicBox);
            this.Controls.Add(this.SubscribeButton);
            this.Name = "SubscriberForm";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button SubscribeButton;
        private System.Windows.Forms.TextBox TopicBox;
        private System.Windows.Forms.TextBox PublicationBox;
        private System.Windows.Forms.TextBox UnsubBox;
        private System.Windows.Forms.Button UnsubButton;
        private System.Windows.Forms.TextBox TopicListBox;
    }
}

