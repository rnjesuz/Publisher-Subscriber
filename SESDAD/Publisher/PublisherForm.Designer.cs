namespace SESDAD
{
    partial class PublisherForm
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
            this.TopicTextBox = new System.Windows.Forms.TextBox();
            this.TopicButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // TopicTextBox
            // 
            this.TopicTextBox.Location = new System.Drawing.Point(30, 10);
            this.TopicTextBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.TopicTextBox.Name = "TopicTextBox";
            this.TopicTextBox.Size = new System.Drawing.Size(241, 20);
            this.TopicTextBox.TabIndex = 0;
            this.TopicTextBox.Text = "Enter your Topic";
            this.TopicTextBox.TextChanged += new System.EventHandler(this.TopicTextBox_TextChanged);
            // 
            // TopicButton
            // 
            this.TopicButton.Location = new System.Drawing.Point(288, 6);
            this.TopicButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.TopicButton.Name = "TopicButton";
            this.TopicButton.Size = new System.Drawing.Size(80, 24);
            this.TopicButton.TabIndex = 1;
            this.TopicButton.Text = "Enter Toppic";
            this.TopicButton.UseVisualStyleBackColor = true;
            this.TopicButton.Click += new System.EventHandler(this.TopicButton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(376, 382);
            this.Controls.Add(this.TopicButton);
            this.Controls.Add(this.TopicTextBox);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox TopicTextBox;
        private System.Windows.Forms.Button TopicButton;
    }
}

