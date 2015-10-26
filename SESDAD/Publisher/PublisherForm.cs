using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SESDAD
{
    public partial class PublisherForm : Form
    {
        private RemotePublisher RP =  new RemotePublisher();
        public PublisherForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void TopicButton_Click(object sender, EventArgs e)
        {
            string topic =TopicTextBox.Text;
            RP.ChangeTopic(topic);
        }

        private void TopicTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void SendButton_Click(object sender, EventArgs e)
        {
            RP.SendPublication(PublicationTextBox.Text);
        }
    }
}
